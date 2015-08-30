using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace GusNet.GusTor
{
    public class GusTorController : IDisposable
    {

        Process torProcess;

        GusTorStream stream;
        Dictionary<string, string> config = new Dictionary<string, string>();

        public bool Start(int TorPort, int ControlPort, string[] HiddenServices = null)
        {

            if (!StarTor(TorPort, ControlPort, HiddenServices) && !TakeOwnership(TorPort, ControlPort, HiddenServices))
                return false;

            try
            {
                stream = new GusTorStream(ControlPort);

                return true;
            }
            catch { return false; }

        }

        bool StarTor(int TorPort, int ControlPort, string[] HiddenServices)
        {
            try
            {

                //TOR is included in the resources file.
                //You can find the license at TORLICENSE.txt on the project's root
                if (!File.Exists("tor.exe"))
                    File.WriteAllBytes("tor.exe", Resources.tor);

                string services = "";

                if (HiddenServices != null)
                {

                    foreach (string s in HiddenServices)
                    {

                        string[] data = s.Split('@');

                        services += string.Format(" --HiddenServiceDir \"{1}\" --HiddenServicePort \"{0}\"", data);
                    
                    }
                
                }

                ProcessStartInfo info = new ProcessStartInfo { Arguments = "--SOCKSPort " + TorPort.ToString() + " --ControlPort " + ControlPort.ToString() + services, CreateNoWindow = true, FileName = "tor.exe", RedirectStandardOutput = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden };

                torProcess = Process.Start(info);
                using (StreamReader reader = torProcess.StandardOutput)
                {
                    string line = null;

                    while ((line = reader.ReadLine()) != null)
                    {
                        Debug.Print(line);

                        if (line.Contains("Bootstrapped 100%"))
                            return true;
                    
                    }
                }

                return false;

            }
            catch { return false; }
        }

        private bool TakeOwnership(int TorPort, int ControlPort, string[] HiddenServices)
        {
            try
            {
                Process[] instances = Process.GetProcessesByName("tor");

                foreach (var proc in instances)
                    proc.Kill();

                return StarTor(TorPort, ControlPort, HiddenServices);
            }
            catch { return false; }
        
        }

        public string ProtocolInfo()
        {

            SendCommand("PROTOCOLINFO");

            return ReadResponse();

        }

        public bool Autheticate()
        {
            SendCommand("AUTHENTICATE");

            string response = ReadResponse();

            if (OkResponse(response))
                return true;

            return false;

        }

        public bool NewIdentity()
        {
            SendCommand("signal NEWNYM\r\n");

            string response = ReadResponse();

            if (OkResponse(response))
                return true;

            return false;

        }

        public string RegisterHiddenService(string FilePath, int ServicePort, IPAddress ServerAddress, int ServerPort)
        {

            string command = string.Format("setconf hiddenservicedir={0} hiddenserviceport=\"{1} {2}\"", FilePath, ServicePort.ToString(), ServerAddress.ToString() + ":" + ServerPort.ToString());
        
            string response = ExecuteCommand(command);

            if(!OkResponse(response))
                return null;

            string host = File.ReadAllText(Path.Combine(FilePath, "hostname"));

            return host;

        }

        private bool OkResponse(string Response)
        {

            if (Response == null)
                return false;

            return Response.Contains("250 OK");
        
        }

        private void SendCommand(string Command)
        {
            string cmd = Command;

            if (Command.EndsWith("\r\n"))
                cmd = cmd.Substring(0, cmd.Length - 2);

            if (cmd.EndsWith("\n"))
                cmd = cmd.Substring(0, cmd.Length - 1);

            stream.WriteLine(cmd);
        }

        private string ReadResponse()
        {

            bool doRead = true;
            string response = "";

            string line = "";

            int nline = 0;

            while (doRead && (line = stream.ReadLine(nline++ == 0)) != null)
                response += line + "\r\n";
            

            return response;

        }

        public string ExecuteCommand(string Command)
        {

            SendCommand(Command);

            return ReadResponse();
        
        }

        private bool IsMultiLine(string Response)
        {

            string[] parts = Response.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1 && parts[1].StartsWith("+"))
                return true;

            return false;

        }
        
        public class GusTorStream
        {

            Socket masterSocket;

            StringBuilder buffer = new StringBuilder(1024);

            byte[] bBuffer = new byte[1024];

            public bool Open { get; private set; }

            AutoResetEvent gotData = new AutoResetEvent(false);

            public GusTorStream(int Port)
            {

                IPEndPoint ip = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);
                masterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                masterSocket.Connect(ip);

                masterSocket.BeginReceive(bBuffer, 0, 1024, SocketFlags.None, GotData, null);

                Open = true;

            }

            void GotData(IAsyncResult Result)
            {

                try 
                {

                    int leidos = masterSocket.EndReceive(Result);

                    if (leidos == 0)
                    {
                        Open = false;
                        gotData.Set(); 
                        gotData.Dispose();
                    }
                    else
                    {
                        lock (buffer)
                            buffer.Append(Encoding.ASCII.GetString(bBuffer, 0, leidos));

                        masterSocket.BeginReceive(bBuffer, 0, 1024, SocketFlags.None, GotData, null);

                        gotData.Set();
                    }
                
                }
                catch { Open = false; gotData.Set(); gotData.Dispose(); }
            
            }

            public void WriteLine(string Text)
            {
                if (!Open)
                    throw new ThreadStateException();

                masterSocket.Send(Encoding.ASCII.GetBytes(Text + "\r\n"));
                
            }

            public string ReadLine(bool BlockUntilResponse)
            {
                if (!Open)
                    throw new ThreadStateException();

                string cmd = null;

                bool loop = true;

                while (loop == true && cmd == null)
                {
                    if (BlockUntilResponse)
                        gotData.WaitOne();
                    else
                        loop = false;

                    lock (buffer)
                    {
                        string data = buffer.ToString();
                        int pos = data.IndexOf("\r\n");

                        if (pos != -1)
                        {

                            cmd = data.Substring(0, pos);
                            buffer.Remove(0, pos + 2);

                        }
                    }
                }

                return cmd;
            }

            public void Close()
            {

                Open = false;


                try
                {
                    masterSocket.Close();
                    masterSocket.Dispose();
                    masterSocket = null;
                }
                catch { }

                try
                {
                    gotData.Set();
                    gotData.Dispose();
                    gotData = null;
                }
                catch { }
            }

        }

        public void Dispose()
        {
            if (torProcess != null)
            {
                try
                {
                    torProcess.Kill();
                }
                catch { }
                torProcess = null;
            
            }
        }

        ~GusTorController()
        {

            if (torProcess != null)
            {
                try
                {
                    torProcess.Kill();
                }
                catch { }
                torProcess = null;

            }

        }

    }
}
