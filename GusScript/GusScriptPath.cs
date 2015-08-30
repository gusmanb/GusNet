/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using GusNet.GusServer;

namespace GusNet.GusScripting
{
    public class GusScriptPath : GusServerPath
    {
        string path;
        string physicalPath;

        public string PhysicalPath
        {
            get { return physicalPath; }
            set { physicalPath = value; }
        }

        string defaultFile;

        public string DefaultFile
        {
            get { return defaultFile; }
            set { defaultFile = value; }
        }

        bool debug = false;

        public bool Debug
        {
            get { return debug; }
            set { debug = value; }
        }

        Dictionary<string, CompiledAssemblyInfo> onMemoryScripts = new Dictionary<string, CompiledAssemblyInfo>();

        public GusScriptPath() { }

        public GusScriptPath(string WebPath, string PhysicalPath, string DefaultFile, bool Debug = true)
        {
            path = WebPath;
            physicalPath = PhysicalPath;
            defaultFile = DefaultFile;
            debug = Debug;
        }

        public override string Path
        {
            get { return path; }

        }

        public string WebPath { get { return path; } set { path = value; } }

        public override void ProcessRequest(GusServerRequest Request)
        {
            
            string file = defaultFile;

            if (Request.GetVariables.ContainsKey("file"))
                file = System.IO.Path.GetFileNameWithoutExtension(Request.GetVariables["file"]) + ".gsc";

            string path = System.IO.Path.Combine(physicalPath, file);

            if (!File.Exists(path))
            {

                Request.WriteNotFoundResponse();
                return;

            }

            dynamic script = null;
            Assembly asm = null;
            string error = "";
            string compiledcode = "";
            string sharedcode = "";
            try
            {
                FileInfo fi = new FileInfo(path);

                if (!onMemoryScripts.ContainsKey(path) || onMemoryScripts[path].CodeDate != fi.LastWriteTime)
                {
                    string code = File.ReadAllText(path);
                    GusScripting.GusScriptParser parser = new GusScripting.GusScriptParser();
                    asm = parser.CreateScriptObject(code, System.IO.Path.GetDirectoryName(path), debug);
                    error = parser.ErrorMsg;
                    compiledcode = parser.CompiledCode.ExecutionCode;
                    sharedcode = parser.CompiledCode.SharedCode;

                    if (asm != null)
                        onMemoryScripts[path] =  new CompiledAssemblyInfo { Assmebly = asm, CodeDate = fi.LastWriteTime };
                }
                else
                    asm = onMemoryScripts[path].Assmebly;
                
            }
            catch { }

            if (asm == null)
            {

                if (debug)
                {

                    Request.Processor.WriteResponseCode("500");

                    if (!string.IsNullOrEmpty(error) && debug)
                        Request.ResponseStream.WriteText(@"<html><body><h1>HTTP Error 500 - Internal server error</h1><div><p>GusScript compilation error</p></div><h2>Errors</h2><div><p>" + error.Replace("\r\n", "<br />") + "</p></div><h2>Compiled code</h2><div><p>" + compiledcode.Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;").Replace("\r\n", "<br />") + "</p><p>" + sharedcode.Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;").Replace("\r\n", "<br />") + "</p></div></body></html>");


                }
                else
                    Request.Processor.WriteError();

                return;

            }

            try
            {
                script = asm.CreateInstance("GusNet.GusScripting.CompiledGusScript");

                script.Execute(Request);
            }
            catch (Exception Ex)
            {
                try
                {
                    Request.ResponseStream.WriteText("<div style=\"background-color:#FFCC00; padding:10px\"><b>Execution error</b><br /><br />" + Ex.Message + " &lt;-&gt; " + Ex.StackTrace + "</div>");
                }
                catch { }
            }
        }

        public string Info { get { return path + " @ " + physicalPath; } }


    }

    class CompiledAssemblyInfo
    {
        public Assembly Assmebly { get; set; }
        public DateTime CodeDate { get; set; }
    }

}
