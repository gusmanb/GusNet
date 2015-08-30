/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Web;

namespace GusNet.GusServer
{
    public class GusMainServer : GusHttpServer
    {

        Dictionary<string, GusServerPath> paths = new Dictionary<string, GusServerPath>(StringComparer.OrdinalIgnoreCase);

        Regex pathParser = new Regex("([^\\?]*)(\\?*)?(.*)?", RegexOptions.Singleline);

        public GusMainServer(int Port, bool UseSsl, string CertificateFile = null) : base(Port, UseSsl, CertificateFile) { }

        ~GusMainServer()
        {

            Stop();
        
        }

        public void AddPath(GusServerPath Path)
        {

            lock(paths)
                paths.Add(Path.Path, Path);
        
        }

        public void RemovePath(GusServerPath Path)
        {

            lock (paths)
                paths.Remove(Path.Path);
        
        }

        public override void HandleRequest(GusHttpProcessor Processor)
        {

            GusServerRequest req = CreateRequest(Processor);
            GusServerPath path = null;

            lock(paths)
            {
                if (req == null || !paths.ContainsKey(req.Path))
                {

                    if (paths.ContainsKey("*"))
                        path = paths["*"];
                    else
                    {
                        Processor.WriteNotFound();
                        return;
                    }
                }
                else
                    path = paths[req.Path];
            }

            if (Processor.Method.ToLower() == "post")
            {
                Stream str = File.OpenRead(Processor.PostDataFile);

                if (req.RequestHeaders.ContainsKey("Content-Type") && req.RequestHeaders["Content-Type"].ToLower().StartsWith("multipart/"))
                {

                    GusPostProcessor post = new GusPostProcessor(str, req.RequestHeaders["Content-Type"]);

                    if (post.Success)
                    {

                        req.PostFiles = post.Files;
                        req.PostVariables = post.Variables;

                    }
                    else
                    {
                        req.PostFiles = new Dictionary<string, GusPostFile>(StringComparer.OrdinalIgnoreCase);
                        req.PostVariables = new Dictionary<string, string>();

                        StreamReader reader = new StreamReader(str);
                        string data = reader.ReadToEnd();

                        var vars = ParseQueryString(data);

                        foreach (string v in vars.Keys)
                            req.PostVariables.Add(v, vars[v]);


                    }

                    
                }
                else
                {

                    req.PostVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    StreamReader reader = new StreamReader(str);
                    string data = reader.ReadToEnd();

                    var vars = ParseQueryString(data);

                    foreach (string v in vars.Keys)
                        req.PostVariables.Add(v, vars[v]);


                }

                str.Close();
            
            }

            try
            {

                path.ProcessRequest(req);

            }
            catch(Exception Ex) 
            {
                try
                {
                    req.ResponseStream.WriteText("<div style=\"background-color:#FFCC00; padding:10px\"><b>Execution error</b><br /><br />" + Ex.Message + "</div>");
                }
                catch { }
            }

            if (req.Method.ToLower() == "post" && req.PostFiles != null)
            {
                foreach (var v in req.PostFiles)
                {
                    if (v.Value.TempFile != null)
                        File.Delete(v.Value.TempFile);
                }
            }
        }

        public static Dictionary<string, string> ParseQueryString(string Query)
        {

            Dictionary<string, string> vars = new Dictionary<string, string>();

            string[] groups = Query.Split('&');

            foreach (var group in groups)
            {

                int pos = group.IndexOf('=');

                if (pos != -1)
                    vars.Add(group.Substring(0, pos), group.Substring(pos + 1));
                else
                    vars.Add(group, "");
            
            }

            return vars;

        }

        private GusServerRequest CreateRequest(GusHttpProcessor Processor)
        {

            Match m = pathParser.Match(Processor.SourceUrl);

            if (m == null || !m.Success)
                return null;
            else
            {
                GusServerRequest req = new GusServerRequest();
                req.Processor = Processor;
                req.Server = Processor.Server;
                req.RequestHeaders = Processor.RequestHeaders;
                req.Method = Processor.Method;
                req.SourceUrl = Processor.SourceUrl;
                req.Path = m.Groups[1].Value;
                req.RemoteIP = Processor.RemoteIP;
                req.RequestStream = Processor.InputStream;
                req.ResponseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                req.Cookies = new Dictionary<string, string>();
                req.PostVariables = new Dictionary<string, string>();
                req.PostFiles = new Dictionary<string, GusPostFile>();
                req.IsSsl = Processor.IsSecure;

                if (req.RequestHeaders.ContainsKey("Cookie"))
                    req.ParseCookie(req.RequestHeaders["Cookie"]);

                //req.Path = req.Path.Substring(0, req.Path.Length - 1);
                
                req.QueryString = m.Groups[3].Value;
                req.ProtocolVersion = Processor.HttpProtocolVersion;
                req.ResponseStream = Processor.OutputStream;

                Dictionary<string, string> getVars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var vars = ParseQueryString(req.QueryString);// HttpUtility.ParseQueryString(req.QueryString);

                foreach (string v in vars.Keys)
                    getVars.Add(v, vars[v]);

                req.GetVariables = getVars;

                return req;
            }

        
        }

    }

    public abstract class GusServerPath
    {
        public abstract string Path { get; }
        public abstract void ProcessRequest(GusServerRequest Request);
    }

    public class GusServerRequest
    {

        public GusHttpProcessor Processor { get; internal set; }
        public GusHttpServer Server { get; internal set; }
        public Stream RequestStream { get; internal set; }
        public GusOutputStream ResponseStream { get; internal set; }
        public Dictionary<string, string> RequestHeaders { get; internal set; }
        public Dictionary<string, string> ResponseHeaders { get; internal set; }
        public Dictionary<string, string> GetVariables { get; internal set; }
        public Dictionary<string, string> PostVariables { get; internal set; }
        public Dictionary<string, GusPostFile> PostFiles { get; internal set; }
        public string SourceUrl { get; set; }
        public string ProtocolVersion { get; set; }
        public bool IsSsl { get; set; }
        public string Method { get; set; }
        public string Path { get; internal set; }
        public string QueryString { get; internal set; }
        public string RemoteIP { get; internal set; }
        public Dictionary<string, string> Cookies { get; internal set; }

        /// <summary>
        /// Respuesta 200
        /// Se debe de llamar ANTES de empezar a escribir en el stream de respuesta
        /// Se enviarán todos los headers incluidos en ResponseHeaders
        /// </summary>
        public void WriteOkResponse()
        {

            string type = "text/html";

            if (ResponseHeaders.ContainsKey("Content-Type"))
            {

                type = ResponseHeaders["Content-Type"];
                ResponseHeaders.Remove("Content-Type");
            
            }

            if (ResponseHeaders.ContainsKey("Connection"))
                ResponseHeaders.Remove("Connection");

            var headersList = ResponseHeaders.ToList();

            if (RequestHeaders.ContainsKey("Host"))
            {

                foreach (var cookie in Cookies)
                    headersList.Add(new KeyValuePair<string, string>("Set-Cookie", cookie.Key + "=" + cookie.Value + "; domain=." + RequestHeaders["Host"]));
            }

            Processor.WriteSuccess(type, headersList);
        
        }

        /// <summary>
        /// Respuesta 404
        /// Se debe de llamar ANTES de empezar a escribir en el stream de respuesta
        /// </summary>
        /// <param name="ContentType">Tipo de contenido, por defecto Html</param>
        /// <param name="Headers">Headers adicionales</param>
        public void WriteNotFoundResponse()
        {
            if (ResponseHeaders.ContainsKey("Connection"))
                ResponseHeaders.Remove("Connection");

            Processor.WriteNotFound(ResponseHeaders);

        }

        /// <summary>
        /// Respuesta manual
        /// Se debe de llamar ANTES de empezar a escribir en el stream de respuesta
        /// </summary>
        /// <param name="Code">Código de la respuesta</param>
        /// <param name="Headers">Headers adicionales</param>
        public void WriteCustomResponse(string Code)
        {
            if (!ResponseHeaders.ContainsKey("Connection"))
                ResponseHeaders.Add("Connection", "close");

            Processor.WriteResponseCode(Code, ResponseHeaders);

        }


        internal void ParseCookie(string Cookie)
        {
            try
            {
                // Advance through cookies, parsing name=value pairs
                int length = Cookie.Length;
                int index = 0;
                while (index < length)
                    index = parseNameValue(Cookie, index);
            }
            catch { }
        }

        private int parseValue(string cookies, int beginIndex, out string value)
        {
            int length = cookies.Length;
            int index = beginIndex;

            value = null;

            while (index < length)
            {
                switch (cookies[index])
                {
                    case ';':
                    case ',':
                        // Found end of value token
                        value = cookies.Substring(beginIndex, index - beginIndex).Trim();
                        return index + 1;

                    case '"':
                        // Skip past quoted span
                        do index++; 
                        while (cookies[index] != '"');
                        break;
                }

                index++;
            }

            // Found end of value token
            value = cookies.Substring(beginIndex, index - beginIndex).Trim();

            return index;
        }

        private int parseNameValue(string cookies, int beginIndex)
        {
            int length = cookies.Length;
            int index = beginIndex;

            while (index < length)
            {
                switch (cookies[index])
                {
                    case ';':
                    case ',':
                        // Found end of name token without value
                        string cookie = cookies.Substring(beginIndex, index - beginIndex).Trim();
                        if (cookie.Length > 0)
                            Cookies.Add(cookie, "");

                        return index + 1;

                    case '=':
                        // Found end of name token with value
                        string cookieName = cookies.Substring(beginIndex, index - beginIndex).Trim();
                        string cookieValue;

                        index = parseValue(cookies, index + 1, out cookieValue);

                        Cookies.Add(cookieName, cookieValue);

                        return index;

                    case '"':
                        // Skip past quoted span
                        do index++; 
                        while (cookies[index] != '"');
                        break;
                }

                index++;
            }

            if (index > beginIndex)
            {
                // Found end of name token without value
                string cookie = cookies.Substring(beginIndex, index - beginIndex).Trim();
                if (cookie.Length > 0)
                    Cookies.Add(cookie, "");
            }

            return index;
        }
    }
    
}
