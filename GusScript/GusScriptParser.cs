/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

//This code is based on a Internet found code, but can't find the 
//original source to place credits and Copyright.
//If you recognize this code, send a message to the project forum and
//the credits and copyright will be added.
//Thanks!

namespace GusNet.GusScripting
{

    public class GusScriptParser
    {
        public GusScriptCompiler Compiler = null;
        public string ErrorMsg = "";
        public ParsingResult CompiledCode;

        static string[,] CustomKeywords = 
        {
        
            { "([^\n]echo|$echo) ?\\((.*)?\\);", "Request.ResponseStream.WriteText($2);", },
            { "(binecho) ?\\((.*?)\\);", "Request.ResponseStream.Write($2, 0, $2.Length);" },
            { "(head) *?\\( *?(\"[^\"]*?\"|[^\",\\)]*?) *?,(.*?)\\)", "Request.RequestHeaders[$2] = $3" },
            { "(head) *?\\( *?(\"[^\"]*?\"|[^,\"\\)]*?) *?\\)", "(Request.ResponseHeaders.ContainsKey($2) ? Request.ResponseHeaders[$2] : null)" },
            { "(cookie) *?\\( *?(\"[^\"]*?\"|[^\",\\)]*?) *?, *?(\"[^\"]*?\"|[^\",\\)]*?) *?\\)", "Request.Cookies[$2] = $3" },
            { "(cookie) *?\\( *?(\"[^\"]*?\"|[^,\"\\)]*?) *?\\)", "(Request.Cookies.ContainsKey($2) ? Request.Cookies[$2] : null)" },
            { "(forvar) *?\\( *?([^,\"\\)]*?) *?, *?([^,\"\\)]*?) *?\\)", "foreach(var $2 in $3)" },
            { "(forbuc) *?\\( *?([^,\"\\)]*?) *?, *?([^,\"\\)]*?) *?\\)", "for(int buc=$2; buc < $3; buc++)" },
            { "(flush) *?\\( *?\\);", "Request.ResponseStream.Flush();" },
            { "(bp) *?\\( *?\\);", "System.Diagnostics.Debugger.Launch(); System.Diagnostics.Debugger.Break();" },
            { "(ok) *?\\( *?\\);", "Request.WriteOkResponse();" },
            { "(postfile) ?\\((.*?)\\)", "(Request.PostFiles.ContainsKey($2) ? Request.PostFiles[$2] : null)" },
            { "(postvar) ?\\((.*?)\\)", "(Request.PostVariables.ContainsKey($2) ? Request.PostVariables[$2] : null)" },
            { "(getvar) ?\\((.*?)\\)", "(Request.GetVariables.ContainsKey($2) ? Request.GetVariables[$2] : null)" },
        
        };

        public GusScriptParser()
        {
            this.Compiler = new GusScriptCompiler();
        }

        public ParsingResult ParseScript(string ScriptCode)
        {
            if (ScriptCode == null)
                return null;

            StringBuilder Builder = new StringBuilder();
            StringBuilder sharedBuilder = new StringBuilder();

            int Last = 0;
            int NextLocation = 0;

            int Location = ScriptCode.IndexOf("<&", 0);

            if (Location == -1)
                return new ParsingResult{ ExecutionCode = "Request.ResponseStream.WriteText(@\"" + ScriptCode.Replace("\"", "\"\"") + "\" );\r\n\r\n", SharedCode = ""};

            while (Location > -1)
            {
                if (Location > -1)
                {
                    string value = ScriptCode.Substring(Last, Location - Last).Replace("\"", "\"\"");

                    if(!string.IsNullOrEmpty(value) && value != "\r\n")
                        Builder.Append("Request.ResponseStream.WriteText(@\"" + ScriptCode.Substring(Last, Location - Last).Replace("\"", "\"\"") + "\" );\r\n\r\n");
                }

                NextLocation = ScriptCode.IndexOf("&>", Location);
                
                if (NextLocation < 0)
                    break;

                string Snippet = ScriptCode.Substring(Location, NextLocation - Location + 2);

                if (Snippet.Substring(2, 1) == "@")
                {
                    string Attribute = "";

                    Attribute = Utils.StrExtract(Snippet, "assembly", "=");

                    if (Attribute.Length > 0)
                    {
                        Attribute = Utils.StrExtract(Snippet, "\"", "\"");

                        if (Attribute.Length > 0)
                            this.Compiler.AddAssembly(Attribute);
                    }
                    else
                    {
                        Attribute = Utils.StrExtract(Snippet, "import", "=");

                        if (Attribute.Length > 0)
                        {
                            Attribute = Utils.StrExtract(Snippet, "\"", "\"");

                            if (Attribute.Length > 0)
                                this.Compiler.AddNamespace(Attribute);
                        }
                    }
                }
                else if (Snippet.Substring(2, 1) == "&" && Snippet.Substring(Snippet.Length - 3, 1) == "&")
                {

                    sharedBuilder.Append(Snippet.Substring(3, Snippet.Length - 6) + "\r\n");
                
                }
                else
                {

                    for (int buc = 0; buc < CustomKeywords.GetLength(0); buc++)
                        Snippet = Regex.Replace(Snippet, CustomKeywords[buc, 0], CustomKeywords[buc, 1]);

                    Builder.Append(Snippet.Substring(2, Snippet.Length - 4) + "\r\n");
                }

                Last = NextLocation + 2;
                Location = ScriptCode.IndexOf("<&", Last);

                if (Location < 0)
                    Builder.Append("Request.ResponseStream.WriteText(@\"" + ScriptCode.Substring(Last, ScriptCode.Length - Last).Replace("\"", "\"\"") + "\" );\r\n\r\n");
            }

            ParsingResult result = new ParsingResult { ExecutionCode = Builder.ToString(), SharedCode = sharedBuilder.ToString() };

            return result;
        }

        public Assembly CreateScriptObject(string Code, string AssembliesPath, bool Debug = false)
        {

            CompiledCode = ParseScript(Code);

            var asm = Compiler.CreateScript(CompiledCode, Debug, AssembliesPath);
            ErrorMsg = Compiler.ErrorMsg;

            return asm;
        }

    }

    public class ParsingResult
    {

        public string ExecutionCode { get; set; }
        public string SharedCode { get; set; }
    }
}
