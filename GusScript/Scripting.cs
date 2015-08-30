/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.Reflection;
using System.Runtime.Remoting;
using System.CodeDom.Compiler;
using System.IO;
using GusNet.GusServer;

namespace GusNet.GusScripting
{

    public delegate void DelegateCompleted(object sender, EventArgs e);


    public class GusScriptCompiler
    {

        protected ICodeCompiler Compiler = null;

        protected CompilerParameters Parameters = null;

        protected CompilerResults Compiled = null;

        protected string Namespaces = "";

        const string AssemblyNamespace = "GusNet.GusScripting";

        public string ErrorMsg = "";

        public GusScriptCompiler()
        {
            this.Compiler = new CSharpCodeProvider().CreateCompiler();
            this.Parameters = new CompilerParameters();
            AddDefaultAssemblies();
        }


        public void AddAssembly(string lcAssemblyDll, string lcNamespace)
        {
            if (lcAssemblyDll == null && lcNamespace == null)
            {

                this.Parameters.ReferencedAssemblies.Clear();
                this.Namespaces = "";
                return;
            }

            if (lcAssemblyDll != null)
                this.Parameters.ReferencedAssemblies.Add(lcAssemblyDll);

            if (lcNamespace != null)
                this.Namespaces = this.Namespaces + "using " + lcNamespace + ";\r\n";
                
        }

        public void AddAssembly(string lcAssemblyDll)
        {
            this.AddAssembly(lcAssemblyDll, null);
        }

        public void AddNamespace(string lcNamespace)
        {
            this.AddAssembly(null, lcNamespace);
        }

        void AddDefaultAssemblies()
        {
            this.AddAssembly("System.dll", "System");
            this.AddNamespace("System.Reflection");
            this.AddNamespace("System.IO");

            this.AddAssembly("GusScript.dll", "GusNet.GusScripting");
            this.AddAssembly("GusServer.dll", "GusNet.GusServer");
        }

        public Assembly CreateScript(ParsingResult Code, bool Debug, string AssembliesPath)
        {

            string code = this.Namespaces + @"
namespace " + AssemblyNamespace + @"
{

    public class CompiledGusScript : GusScript
    {

        " + Code.SharedCode + @"

        public override void Execute(GusServerRequest Request)
        {
            " + Code.ExecutionCode + @"
        }

    }
}";

            return CompileAssembly(code, Debug, AssembliesPath);

        }

        private Assembly CompileAssembly(string lcSource, bool Debug, string AssembliesPath)
        {

            if (Debug)
            {
                this.Parameters.GenerateInMemory = false;
                this.Parameters.TempFiles = new TempFileCollection(Environment.GetEnvironmentVariable("TEMP"), true);
                this.Parameters.IncludeDebugInformation = true;
            }
            else
                this.Parameters.GenerateInMemory = true;

            this.Parameters.CompilerOptions = "/lib:" + AssembliesPath;

            this.Compiled = this.Compiler.CompileAssemblyFromSource(this.Parameters, lcSource);

            if (Compiled.Errors.HasErrors)
            {
                this.ErrorMsg = Compiled.Errors.Count.ToString() + " Errors:";

                for (int x = 0; x < Compiled.Errors.Count; x++)
                    this.ErrorMsg = this.ErrorMsg + "\r\nLine: " + Compiled.Errors[x].Line.ToString() + " - " +
                                                       Compiled.Errors[x].ErrorText;
                return null;
            }

            return Compiled.CompiledAssembly;
        }

    }

    public abstract class GusScript
    {

        public abstract void Execute(GusServerRequest Request);
    
    }

}
