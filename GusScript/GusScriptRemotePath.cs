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
    public class GusScriptRemotePath : GusServerPath
    {
        string path;
        Dictionary<string, Assembly> onMemoryScripts = new Dictionary<string, Assembly>();

        public GusScriptRemotePath(string WebPath)
        {
            path = WebPath;
        }

        public override string Path
        {
            get { return path; }
        }

        public override void ProcessRequest(GusServerRequest Request)
        {
            if(Request.Method.ToLower() != "post")
                return;

            if(Request.PostVariables == null)
                return;

            if (Request.PostVariables.ContainsKey("compile") && Request.PostVariables.ContainsKey("code"))
            {

                string file = Request.PostVariables["compile"];
                string code = Request.PostVariables["code"];

                GusScripting.GusScriptParser parser = new GusScripting.GusScriptParser();
                Assembly asm = parser.CreateScriptObject(code, "", true);
                string error = parser.ErrorMsg;
                string compiledcode = parser.CompiledCode.ExecutionCode;
                string sharedcode = parser.CompiledCode.SharedCode;

                if (asm != null)
                    onMemoryScripts.Add(file, asm);

                if (!string.IsNullOrEmpty(error))
                {
                    Request.Processor.WriteResponseCode("500");
                    Request.ResponseStream.WriteText(@"<html><body><h1>HTTP Error 500 - Internal server error</h1><div><p>GusScript compilation error</p></div><h2>Errors</h2><div><p>" + error.Replace("\r\n", "<br />") + "</p></div><h2>Compiled code</h2><div><p>" + compiledcode.Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;").Replace("\r\n", "<br />") + "</p><p>" + sharedcode.Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;").Replace("\r\n", "<br />") + "</p></div></body></html>");
                    return;
                }
                else
                {

                    Request.WriteOkResponse();
                    Request.ResponseStream.WriteText(@"<html><body><h1>Code compiled for file " + file + "</h1><div><p>" + compiledcode.Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;").Replace("\r\n", "<br />") + "</p><p>" + sharedcode.Replace("<", "&lt;").Replace(">", "&gt;").Replace(" ", "&nbsp;").Replace("\r\n", "<br />") + "</p></div></body></html>");
                    return;
                }
            }

            Assembly asmm;

            try
            {
                if (!onMemoryScripts.ContainsKey(path))
                {
                    Request.WriteNotFoundResponse();
                    return;
                    
                }
                else
                    asmm = onMemoryScripts[path];
                
            }
            catch
            {
                Request.WriteNotFoundResponse();
                return;
            }

            try
            {
                dynamic script = asmm.CreateInstance("GusScripting.CompiledGusScript");

                script.Execute(Request);
            }
            catch(Exception Ex)
            {
                try
                {
                    Request.ResponseStream.WriteText("<div style=\"background-color:#FFCC00; padding:10px\"><b>Execution error</b><br /><br />" + Ex.Message + "</div>");
                }
                catch { }
            }
        }
    }

}
