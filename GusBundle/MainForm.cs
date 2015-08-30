/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GusNet.GusTor;
using GusNet.GusServer;
using GusNet.GusScripting;
using GusNet.GusBridge;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;

namespace GusBundle
{
    public partial class MainForm : Form
    {
        GusTorController torController;
        GusMainServer server;
        List<GusScriptPath> serverPaths;
        GusMainServer bridge;
        GusBridgePath bridgePath;
        Config cfg;

        string direccionServicio;

        bool save = false;

        System.Windows.Forms.Timer tmrDead;

        public MainForm()
        {

            InitializeComponent();

            if (!Directory.Exists(Application.StartupPath + "\\servicio"))
                Directory.CreateDirectory(Application.StartupPath + "\\servicio");

            var process = Process.GetProcessesByName("tor");

            foreach (var v in process)
                v.Kill();

            Thread.Sleep(1000);

            server = new GusMainServer(8190, false);

            if (!server.Start())
            {

                MessageBox.Show(Traducir("No se pudo arrancar el servidor, verifique que no haya otra instancia ejecutandose."));
                tmrDead = new System.Windows.Forms.Timer();
                tmrDead.Interval = 2000;
                tmrDead.Tick += new EventHandler(tmrDead_Tick);
                tmrDead.Enabled = true;
                return;
            
            }

            bridgePath = new GusBridgePath(IPAddress.Parse("127.0.0.1"), 8192, "", "");
            
            serverPaths = Deserializar<List<GusScriptPath>>("paths");

            RefrescarPaths();

            foreach (var path in serverPaths)
                server.AddPath(path);
            
            cfg = Deserializar<Config>("config");

            if (cfg.AutoBoot)
            {

                arrancarAuto.Checked = true;
                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                rkApp.SetValue("GusNet Bundle", Application.ExecutablePath.ToString());
                rkApp.Dispose();
            }
            else
            {

                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                rkApp.DeleteValue("GusNet Bundle", false);
                rkApp.Dispose();
            
            }

            if (cfg.AutoTor)
            {
                iniciarTor.Checked = true;
                IniciarTor();
            }

            save = true;

            
        }

        void tmrDead_Tick(object sender, EventArgs e)
        {
            this.Close();
        }

        private string Traducir(string Frase)
        {
            string resourceName = "GusBundle.Texts";
            ResourceManager rm = new ResourceManager(resourceName, Assembly.GetExecutingAssembly());
            return rm.GetString(Frase.Replace(" ", "_").Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", ""));
            
        }

        void IniciarTor()
        {

            if (torController != null)
                torController.Dispose();

            torController = new GusTorController();

            if (torController.Start(8192, 8193))
            {
                if (torController.Autheticate())
                {

                    direccionServicio = torController.RegisterHiddenService("servicio", 80, IPAddress.Parse("127.0.0.1"), 8190);

                    if (direccionServicio == null)
                        MessageBox.Show(Traducir("No se pudo registrar el servicio de TOR, no se podrá acceder a tu servidor."));
                    else
                    {
                        bridge = new GusMainServer(8191, false);
                        if (!bridge.Start())
                        {
                            MessageBox.Show(Traducir("Error creando puente HTTP"));
                            torController.Dispose();
                            torController = null;
                            bridge = null;
                            return;
                        }

                        bridge.AddPath(bridgePath);
                        servicioTor.Text = direccionServicio;
                        
                    }

                    button1.Enabled = false;
                    button2.Enabled = true;
                    button6.Enabled = true;
                }
                else
                {
                    MessageBox.Show(Traducir("Fallo de autenticación de TOR!"));
                    torController.Dispose();
                    torController = null;
                }
            }
            else
            {

                MessageBox.Show(Traducir("No se pudo iniciar TOR, verifica tu conexión a Internet."));
                torController.Dispose();
                torController = null;
            
            }
        }

        static T Deserializar<T>(string Archivo) where T : class, new()
        {
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(T));

                Stream str = File.OpenRead(Archivo);

                T item = ser.Deserialize(str) as T;

                str.Close();

                return item;
            }
            catch { return new T();  }
        
        }

        static void Serializar<T>(T Objeto, string Archivo) where T : class, new()
        {
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(T));

                Stream str = File.Create(Archivo);

                ser.Serialize(str, Objeto);

                str.Close();

            }
            catch {  }

        }

        private void label8_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
            else
                this.WindowState = FormWindowState.Normal;

        }

        private void label9_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void label6_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        Point prev;
        bool drag = false;

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            Capture = true;
            prev = e.Location;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (Capture)
            {
                if (drag)
                    return;

                drag = true;
                this.Location = new Point(this.Location.X + (e.Location.X - prev.X), this.Location.Y + (e.Location.Y - prev.Y));
                drag = false;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
            Capture = false;
        }

        private void Button_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawRectangle(new Pen((sender as Control).BackColor, 5), (sender as Control).ClientRectangle);
        }

        private void iniciarTor_CheckedChanged(object sender, EventArgs e)
        {
            SalvarConfig();
        }

        private void SalvarConfig()
        {
            if (!save)
                return;

            cfg.AutoTor = iniciarTor.Checked;
            cfg.AutoBoot = arrancarAuto.Checked;

            Serializar(cfg, "config");

        }

        private void DetenerTor()
        {
            if (torController != null)
            {

                torController.Dispose();
                torController = null;

                bridge.RemovePath(bridgePath);
                bridge.Stop();
                bridge = null;
            
            }

            button1.Enabled = true;
            button2.Enabled = false;
            button6.Enabled = false;
        }

        private void arrancarAuto_CheckedChanged(object sender, EventArgs e)
        {
            if (!save)
                return;

            if (arrancarAuto.Checked )
            {

                cfg.AutoBoot = true;
                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                rkApp.SetValue("GusNet Bundle", Application.ExecutablePath.ToString());
                rkApp.Dispose();
            }
            else
            {
                cfg.AutoBoot = false;
                RegistryKey rkApp = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                rkApp.DeleteValue("GusNet Bundle", false);
                rkApp.Dispose();

            }

            SalvarConfig();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = Traducir("Seleccione el directorio.");
            dlg.SelectedPath = Application.StartupPath;
            dlg.ShowNewFolderButton = true;

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            directorio.Text = dlg.SelectedPath;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (directorio.Text == "" || !Directory.Exists(directorio.Text))
            {

                MessageBox.Show(Traducir("No se encuentra el directorio."));
                return;
            
            }

            if (ruta.Text == "")
            {

                MessageBox.Show(Traducir("Debe indicar una ruta virtual."));
                return;
            
            }

            if (serverPaths.Where(sp => sp.Path == ruta.Text).Count() > 0)
            {

                MessageBox.Show(Traducir("Ya existe esa ruta."));
                return;
            
            }

            GusScriptPath path = new GusScriptPath(ruta.Text, directorio.Text, "index.gsc", true);
            serverPaths.Add(path);
            server.AddPath(path);
            SalvarPaths();
            RefrescarPaths();
        }

        private void SalvarPaths()
        {
            Serializar(serverPaths, "paths");
        }

        private void RefrescarPaths()
        {
            listaDirectorios.DataSource = null;
            listaDirectorios.DataSource = serverPaths;
            listaDirectorios.DisplayMember = "Info";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listaDirectorios.SelectedIndex == -1)
            {

                MessageBox.Show(Traducir("Seleccione un directorio."));
                return;
            
            }

            GusScriptPath path = listaDirectorios.SelectedItem as GusScriptPath;
            server.RemovePath(path);
            serverPaths.Remove(path);
            RefrescarPaths();
            SalvarPaths();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            IniciarTor();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DetenerTor();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (torController != null)
            {

                if (!torController.NewIdentity())
                    MessageBox.Show(Traducir("No se pudo obtener una nueva identidad, reitnentelo en unos minutos."));
            
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DetenerTor();
            server.Stop();
        }

        private void nico_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.Show();
            this.BringToFront();
        }

        System.Windows.Forms.Timer timer;

        private void MainForm_Load(object sender, EventArgs e)
        {
            nico.Visible = true;
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
            this.Hide();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            this.Hide();
        }

    }

    public class Config
    {

        public bool AutoBoot { get; set; }
        public bool AutoTor { get; set; }
    }
}
