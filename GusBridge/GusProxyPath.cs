/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

//Uncomment to enable debug output
//#define ENABLEDEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using GusNet.GusServer;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace GusNet.GusBridge
{

    public class GusProxyPath : GusServerPath
    {

        public override string Path
        {
            get { return "*"; }
        }

        public override void ProcessRequest(GusServer.GusServerRequest Request)
        {
            string currentHost = Request.Method.ToLower() == "connect" ? Request.Path : Request.RequestHeaders["Host"];

            string[] address = currentHost.Split(':');

            Socket socket;

            ManualResetEvent evt;
            ProxyData bdata = null;

            try
            {


                string sport = "80";

                if (address.Length == 1)
                {

                    if (Request.Method.ToLower() == "connect")
                        sport = "443";


                }
                else
                    sport = address[1];

                socket = CreateSocket(address[0], int.Parse(sport));

                evt = new ManualResetEvent(false);

                bdata = new ProxyData { ClientSocket = Request.Processor.Socket, Event = evt, Host = currentHost, SourceRequest = Request, KeepAlive = Request.Method.ToLower() != "connect" };
                
                SocketData sckData = new SocketData { Data = bdata, ServerSocket = socket };
                bdata.CurrentServer = sckData;

                if (Request.RequestHeaders.ContainsKey("Proxy-Connection") && Request.RequestHeaders["Proxy-Connection"].ToLower() != "keep-alive")
                    bdata.KeepAlive = false;

                if (Request.RequestHeaders.ContainsKey("Connection") && Request.RequestHeaders["Connection"].ToLower() != "keep-alive")
                    bdata.KeepAlive = false;
                

#if ENABLEDEBUG
                bdata.ProxyKeepAlive = bdata.KeepAlive;
#endif

                if (Request.Method.ToLower() == "connect")
                {

                    bdata.SSL = true;
                    bdata.RequestLeft = -1;
                    sckData.ResponseLeft = -1;
                    bdata.KeepAlive = false;
                }

                if (bdata.SSL)
                {
                    byte[] okdata = Encoding.ASCII.GetBytes(Request.ProtocolVersion + " 200 Connection established\r\nProxy-Agent: GusNet.GusBridge Server\r\n\r\n");
                    bdata.ClientSocket.Send(okdata, 0, okdata.Length, SocketFlags.None);
                }
                else
                {

                    byte[] header = ReconstructRequest(Request, bdata);
#if ENABLEDEBUG
                    bdata.requestheaders.Add("Original\r\n" + Encoding.ASCII.GetString(Request.Processor.OriginalRequest) + "Enviada\r\n" + Encoding.ASCII.GetString(header));
#endif
                    socket.Send(header, SocketFlags.None);

                    if (!string.IsNullOrEmpty(Request.Processor.PostDataFile))
                    {
                        Stream str = File.OpenRead(Request.Processor.PostDataFile);
                        int leidos = str.Read(bdata.ClientBuffer, 0, bdata.ClientBuffer.Length);

                        while (leidos > 0)
                        {

                            socket.Send(bdata.ClientBuffer, leidos, SocketFlags.None);
                            leidos = str.Read(bdata.ClientBuffer, 0, bdata.ClientBuffer.Length);
                        }

                        str.Close();

                    }

                }

                if (bdata.SSL)
                    BeginRelayClient(bdata);
                else
                    BeginReadClientHeader(bdata);

                if (bdata.SSL)
                    BeginRelayServer(sckData);
                else
                    BeginReadServerHeader(sckData);

#if ENABLEDEBUG

                //Para poder poner Breakpoints en peticiones que se están ejecutando
                while (!evt.WaitOne(10))
                {
                }
                DebugData(bdata);
#else
                evt.WaitOne();
#endif

            }
            catch
            { }
        }
#if ENABLEDEBUG
        private void DebugData(ProxyData bdata)
        {

            string data = "@@@@@@@@@@@@@@@ Socket, modo SSL= " + bdata.SSL + ", KeepAlive original=" + bdata.ProxyKeepAlive + ", KeepAlive actual=" + bdata.KeepAlive + " --------\r\r";

            data += "##### Secuencia de funciones\r\n";

            foreach (string s in bdata.debug)
                data += s + "\r\n";

            data += "##### Requests recibidas\r\n";

            foreach (string s in bdata.requestheaders)
                data += s + "\r\n";

            data += "##### Responses recibidas\r\n";

            foreach (string s in bdata.responseheaders)
                data += s + "\r\n";

            Guid id = Guid.NewGuid();



            Stream str = File.Create("debug\\Debug " + id.ToString());
            byte[] filedata = Encoding.ASCII.GetBytes(data);
            str.Write(filedata, 0, filedata.Length);
            str.Close();

            str = File.Create("debug\\Cliente " + id.ToString());
            filedata = bdata.clientReceived.ToArray();
            str.Write(filedata, 0, filedata.Length);
            str.Close();

            str = File.Create("debug\\Servidor " + id.ToString());
            filedata = bdata.serverReceived.ToArray();
            str.Write(filedata, 0, filedata.Length);
            str.Close();

            str = File.Create("debug\\SocketCliente " + id.ToString());
            filedata = bdata.clientSent.ToArray();
            str.Write(filedata, 0, filedata.Length);
            str.Close();

            data += ">>>>>>>>>>>>>>>>>>>>>>>>";

            Debug.Print(data);
        }
#endif

        private byte[] ReconstructRequest(GusServerRequest Request, ProxyData Data)
        {
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("ReconstructRequest");
#endif
            Regex postParser = new Regex("^[a-zA-Z]+:/+([^\\n/]*)(/.*)?", RegexOptions.Singleline);

            Match m = postParser.Match(Request.SourceUrl);

            string host = Request.RequestHeaders["Host"];
            string path = Request.SourceUrl;

            if (m.Success)
            {

                host = m.Groups[1].Value;
                path = m.Groups[2].Value;

                if (string.IsNullOrEmpty(path))
                    path = "/";

            }

            string query = Request.Method + " " + path + " " + Request.ProtocolVersion + "\r\n";

            bool chunked = false;

            foreach (string sc in Request.RequestHeaders.Keys)
            {
                string key = sc.ToLower();

                if (key.StartsWith("content-length") && chunked)
                    continue;

                if (key.StartsWith("transfer-encoding") && Request.RequestHeaders[sc].ToLower().StartsWith("chunked"))
                    chunked = true;


                if (sc.Length < 6 || !key.StartsWith("proxy-"))
                {
                    query += System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sc) + ": " + Request.RequestHeaders[sc] + "\r\n";

                }

            }

            if (chunked && !string.IsNullOrEmpty(Request.Processor.PostDataFile))
            {

                FileInfo fi = new FileInfo(Request.Processor.PostDataFile);
                query += "Content-Length: " + fi.Length.ToString() + "\r\n";

            }

            query += "\r\n";

            return Encoding.ASCII.GetBytes(query);
        }


        static byte[] trail = Encoding.ASCII.GetBytes("\r\n\r\n");


        private void SwapSocket(string OldHost, string Host, ProxyData data, bool Https)
        {
            try
            {
#if ENABLEDEBUG
                lock (data.debug)
                    data.debug.Add("SwapSocket " + OldHost + "->" + Host);
#endif
                string[] parts = Host.Split(':');

                string port = Https ? "443" : "80";
                string host = Host;

                if (parts.Length == 2)
                {

                    host = parts[0];
                    port = parts[1];

                }

                Socket socket = CreateSocket(host, int.Parse(port));
                
                data.CurrentServer = new SocketData { ServerSocket = socket, Data = data };

                if (data.SSL)
                    BeginRelayServer(data.CurrentServer);
                else
                    BeginReadServerHeader(data.CurrentServer);

            }
            catch { }
        }


        void BeginReadClientHeader(ProxyData Data)
        {
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("BeginReadClientHeader");
#endif
            Data.ClientSocket.BeginReceive(Data.ClientBuffer, 0, 16, SocketFlags.None, EndReadClientHeader, Data);

        }

        void EndReadClientHeader(IAsyncResult Result)
        {
            
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("EndReadClientHeader");
#endif
            ProxyData Data = (ProxyData)Result.AsyncState;

            try
            {

                int leidos = Data.ClientSocket.EndReceive(Result);

                if (leidos == 0)
                {

                    CloseClient(Data);
                    CloseServer(Data);
                    return;

                }

                byte[] endBuffer = new Byte[4096];

                bool gotHeader = false;
                int pos = 0;

                while (leidos > 0 && !gotHeader)
                {
#if ENABLEDEBUG
                    Data.clientReceived.AddRange(Data.ClientBuffer.Take(leidos));
#endif
                    Buffer.BlockCopy(Data.ClientBuffer, 0, endBuffer, pos, leidos);
                    pos += leidos;

                    //HasTrail
                    gotHeader = endBuffer[pos - 4] == trail[0] &&
                                endBuffer[pos - 3] == trail[1] &&
                                endBuffer[pos - 2] == trail[2] &&
                                endBuffer[pos - 1] == trail[3];

                    if (!gotHeader)
                        leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 0, 1, SocketFlags.None);
                    else
                        ProcessClientHeader(Data, endBuffer, pos);

                }



            }
            catch 
            {
#if ENABLEDEBUG

                lock (Data.debug)
                        Data.debug.Add("Exception en EndReadClientHeader");
#endif
                CloseClient(Data); CloseServer(Data); 
            }

        }

        private void ProcessClientHeader(ProxyData Data, byte[] Header, int Len)
        {
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("ProcessClientHeader");
#endif

            Data.Chunked = false;

            string header = Encoding.ASCII.GetString(Header, 0, Len);

            string[] lines = header.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            string[] tokens = lines[0].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != 3)
                throw new Exception("invalid http request line");

            Data.SourceRequest.Method = tokens[0].ToLower();

            Regex urlParser = new Regex("^[a-zA-Z]+:/+([^\\n/]*)(/.*)?", RegexOptions.Singleline);

            Match m = urlParser.Match(tokens[1]);

            if (m == null || !m.Success)
            {

                CloseClient(Data);
                CloseServer(Data);
                return;

            }

            string newhost = m.Groups[1].Value.Trim();
            string oldhost = Data.Host;

            bool switchSocket = oldhost.ToLower() != newhost.ToLower();

            Data.Host = newhost;
            Data.SourceRequest.Method = tokens[0];
            Data.SourceRequest.SourceUrl = tokens[1];
            Data.SourceRequest.ProtocolVersion = tokens[2];

            Data.SSL = Data.SourceRequest.Method.ToLower() == "connect";

            if (Data.SSL)
                Data.KeepAlive = false;

            int contentLength = Data.SSL ? -1 : 0;

            foreach (string linea in lines)
            {
                string llinea = linea.ToLower();

                if (llinea.StartsWith("content-length:"))
                {

                    string[] partes = linea.Split(':');

                    if (partes.Length != 2 || !int.TryParse(partes[1], out contentLength))
                    {

                        CloseClient(Data);
                        CloseServer(Data);
                        return;

                    }

                }
                else if (llinea.StartsWith("proxy-connection:") || llinea.StartsWith("connection"))
                {

                    string[] partes = linea.Split(':');

                    if (partes.Length != 2)
                    {

                        CloseClient(Data);
                        CloseServer(Data);
                        return;

                    }

                    if (partes[1].Trim() != "keep-alive")
                        Data.KeepAlive = false;

                }
                else if (llinea.StartsWith("transfer-encoding:"))
                {

                    string[] partes = linea.Split(':');

                    if (partes.Length != 2)
                    {

                        CloseClient(Data);
                        CloseServer(Data);
                        return;

                    }

                    if (partes[1].ToLower().Trim() == "chunked")
                    {
                        Data.Chunked = true;
                        contentLength = 0;
                    }
                }

            }

            Data.RequestLeft = contentLength;

            if (switchSocket)
            {
                try
                {
                    Data.ClosedBySwap = true;
                    SwapSocket(oldhost, Data.Host, Data, Data.SSL);
                }
                catch
                {
                    CloseClient(Data);
                    CloseServer(Data);
                    return;
                }
            }

            if (Data.SourceRequest.Method.ToLower() != "connect")
            {

                string reconstructed = Data.SourceRequest.Method + " " + (m.Groups[2].Value.Trim() == "" ? "/" : m.Groups[2].Value.Trim()) + " " + Data.SourceRequest.ProtocolVersion + "\r\n";

                foreach (string s in lines.Skip(1))
                {
                    if (s.StartsWith("proxy"))
                        continue;

                    reconstructed += s + "\r\n";
                    
                }

                reconstructed += "\r\n";
                Data.CurrentServer.ServerSocket.Send(Encoding.ASCII.GetBytes(reconstructed));

#if ENABLEDEBUG
                Data.requestheaders.Add("Original\r\n" + header + "Enviada\r\n" + reconstructed);
#endif
            }
            else
            {
                byte[] okdata = Encoding.ASCII.GetBytes("HTTP/1.1 200 Connection established\r\nProxy-Agent: GusNet.GusBridge Server\r\n\r\n");
                Data.ClientSocket.Send(okdata, 0, okdata.Length, SocketFlags.None);
            }

            if (Data.Chunked)
            {

                string tam = "";
                int leidos = 0;

                while (!tam.EndsWith("\r\n"))
                {

                    leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, SocketFlags.None);

                    if (leidos == 0)
                        throw new Exception();

                    tam += Encoding.ASCII.GetString(Data.ClientBuffer, 0, 1);
                }

                int tamChunk = int.Parse(tam.Substring(0, tam.Length - 2), System.Globalization.NumberStyles.HexNumber);

                Data.CurrentServer.ServerSocket.Send(Encoding.ASCII.GetBytes(tam));

                Data.RequestLeft = contentLength = tamChunk;

                if (tamChunk == 0)
                {

                    leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, SocketFlags.None);

                    if (leidos == 0)
                        throw new Exception();

                    leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, 1, SocketFlags.None);

                    if (leidos == 0)
                        throw new Exception();

                    Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, 2, SocketFlags.None);

                }

            }

            if (contentLength != 0)
                BeginRelayClient(Data);
            else
                BeginReadClientHeader(Data);
        }

        private void BeginRelayClient(ProxyData Data)
        {
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("BeginRelayClient");
#endif
            Data.ClientSocket.BeginReceive(Data.ClientBuffer, 0, Data.ClientBuffer.Length, SocketFlags.None, EndRelayClient, Data);
        }

        private void EndRelayClient(IAsyncResult Result)
        {

            ProxyData Data = (ProxyData)Result.AsyncState;
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("EndRelayClient");
#endif
            try
            {

                int leidos = Data.ClientSocket.EndReceive(Result);

                if (leidos == 0)
                {

                    while (Data.ClientSocket.Connected)
                        Thread.Sleep(1);

                    CloseClient(Data);
                    CloseServer(Data);
                    return;
                }
#if ENABLEDEBUG
                Data.clientReceived.AddRange(Data.ClientBuffer.Take(leidos));
#endif
                if (Data.RequestLeft == -1)
                    Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, 0, leidos, SocketFlags.None);
                else
                    Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, 0, Math.Min(leidos, Data.RequestLeft), SocketFlags.None);

                int inicio = Data.RequestLeft;
                int restantes = leidos - Data.RequestLeft;

                if (Data.RequestLeft > 0)
                    Data.RequestLeft -= Math.Min(leidos, Data.RequestLeft);

                if (Data.RequestLeft < 1 && Data.SourceRequest.Method.ToLower() != "connect")
                {
                    if (!Data.Chunked)
                        BeginReadClientHeader(Data);
                    else
                    {

                        string tam = "";
                        int pos = inicio;

                        int tamChunk = -1;

                    newChunk:

                        //aqui se atrapa el \r\n anterior
                        while (restantes > 0 && (!tam.EndsWith("\r\n") || tam.Length < 3))
                        {

                            restantes--;
                            tam += Encoding.ASCII.GetString(Data.ClientBuffer, pos, 1);
                            pos++;

                        }

                        while (!tam.EndsWith("\r\n") || tam.Length < 3)
                        {

                            leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, SocketFlags.None);

                            if (leidos == 0)
                                throw new Exception();

                            tam += Encoding.ASCII.GetString(Data.ClientBuffer, 0, 1);
                        }

                        tamChunk = int.Parse(tam.Substring(2, tam.Length - 4), System.Globalization.NumberStyles.HexNumber);

                        bool lastChunk = tamChunk == 0;

                        Data.CurrentServer.ServerSocket.Send(Encoding.ASCII.GetBytes(tam));

                        //TODO: Añadir soporte para el trail
                        if (restantes > 0)
                        {

                            if (restantes >= tamChunk)
                            {

                                Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, pos, tamChunk, SocketFlags.None);
                                restantes -= tamChunk;
                                pos += tamChunk;
                                tam = "";

                                if (lastChunk)
                                {
                                    if (restantes >= 2)
                                        Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, pos, 2, SocketFlags.None);
                                    else if (restantes > 0)
                                    {
                                        Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, pos, 1, SocketFlags.None);

                                        leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, SocketFlags.None);

                                        if (leidos == 0)
                                            throw new Exception();

                                        Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, 0, 1, SocketFlags.None);
                                    }
                                    else
                                    {

                                        leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, SocketFlags.None);

                                        if (leidos == 0)
                                            throw new Exception();

                                        Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, 0, 1, SocketFlags.None);

                                        leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, SocketFlags.None);

                                        if (leidos == 0)
                                            throw new Exception();

                                        Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, 0, 1, SocketFlags.None);

                                    }

                                    if (restantes > 2)
                                        throw new Exception();

                                    lastChunk = false;

                                    BeginReadClientHeader(Data);

                                    return;
                                }
                                else
                                    goto newChunk;
                            }

                            tamChunk -= Math.Min(restantes, tamChunk);

                            Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, pos, restantes, SocketFlags.None);
                        }

                        Data.RequestLeft = tamChunk;

                        if (lastChunk)
                        {

                            leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, SocketFlags.None);

                            if (leidos == 0)
                                throw new Exception();

                            leidos = Data.ClientSocket.Receive(Data.ClientBuffer, 1, 1, SocketFlags.None);

                            if (leidos == 0)
                                throw new Exception();

                            Data.CurrentServer.ServerSocket.Send(Data.ClientBuffer, 2, SocketFlags.None);

                            BeginReadClientHeader(Data);
                        }
                        else
                            BeginRelayClient(Data);


                    }
                }
                else
                    BeginRelayClient(Data);


            }
            catch
            {
                CloseClient(Data);
                CloseServer(Data);
            }

        }

        void CloseClient(ProxyData Data)
        {
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("CloseClient");
#endif

            try
            {

                Data.ClientSocket.Shutdown(SocketShutdown.Both);
                Data.ClientSocket.Disconnect(false);
                Data.ClientSocket.Close();
                Data.ClientSocket.Dispose();
            }
            catch { }

            try
            {
                Data.Event.Set();
            }
            catch { }
        }




        protected virtual Socket CreateSocket(string Host, int Port)
        {

            Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, 5));

            sck.ExclusiveAddressUse = true;

            sck.Connect(Host, Port);

            return sck;
        }




        void BeginReadServerHeader(SocketData Data)
        {
            try
            {
#if ENABLEDEBUG
                lock (Data.Data.debug)
                    Data.Data.debug.Add("BeginReadServerHeader");
#endif
                Data.ServerSocket.BeginReceive(Data.ServerBuffer, 0, 8, SocketFlags.None, EndReadServerHeader, Data);
            }
            catch { }
        }

        //Si estamos recibiendo un Header del servidor, por mucho que sea KeepAlive se cierra para que se
        //entere el servidor a no ser que se cierre por un swapeo
        //
        //->//MAL!!
        //->//Desde el relay se vuelve a lanzar el ReadServer, puede que haga falta encapsular el socket y la query en
        //->//otro objeto para poder cambiar referencias
        void EndReadServerHeader(IAsyncResult Result)
        {
            SocketData sckData = (SocketData)Result.AsyncState;

            if (sckData != sckData.Data.CurrentServer)
            {

                try
                {

                    
                    sckData.ServerSocket.EndReceive(Result);

                }
                catch { }
                try
                {
                    CloseServer(sckData);
                }
                catch { }

                return;

            }

            ProxyData Data = sckData.Data;
#if ENABLEDEBUG

            lock (Data.debug)
            Data.debug.Add("EndReadServerHeader");
            
#endif
            try
            {

                int leidos = sckData.ServerSocket.EndReceive(Result);

                if (leidos == 0)
                {

                    CloseServer(sckData);

                    if (!Data.ClosedBySwap && Data.RequestLeft != 0)
                        CloseClient(Data);
                    else
                        Data.ClosedBySwap = false;

                    return;

                }


                byte[] endBuffer = new Byte[4096];

                bool gotHeader = false;
                int pos = 0;

                while (leidos > 0 && !gotHeader)
                {
#if ENABLEDEBUG
                    Data.serverReceived.AddRange(sckData.ServerBuffer.Take(leidos));
#endif
                    Buffer.BlockCopy(sckData.ServerBuffer, 0, endBuffer, pos, leidos);
                    pos += leidos;

                    if (pos > 4)
                    {
                        //HasTrail
                        gotHeader = endBuffer[pos - 4] == trail[0] &&
                                    endBuffer[pos - 3] == trail[1] &&
                                    endBuffer[pos - 2] == trail[2] &&
                                    endBuffer[pos - 1] == trail[3];

                        if (!gotHeader)
                            leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 0, 1, SocketFlags.None);
                        else
                            ProcessServerHeader(sckData, endBuffer, pos);
                    }
                    else
                        leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 0, 1, SocketFlags.None);
                }



            }
            catch
            {

                CloseServer(sckData);

                if (!Data.ClosedBySwap && Data.RequestLeft != 0)
                    CloseClient(Data);
                else
                    Data.ClosedBySwap = false;
            }

        }

        private void ProcessServerHeader(SocketData Data, byte[] Header, int Len)
        {

            Data.Chunked = false;
            Data.KeepAlive = Data.Data.KeepAlive;

#if ENABLEDEBUG
            lock (Data.Data.debug)
                Data.Data.debug.Add("ProcessServerHeader");
#endif
            string header = Encoding.ASCII.GetString(Header, 0, Len);
#if ENABLEDEBUG
            Data.Data.responseheaders.Add(header);
#endif
            string[] lines = header.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int contentLength = -1;

            foreach (string linea in lines)
            {
                string llinea = linea.ToLower();

                if (llinea.StartsWith("content-length:"))
                {

                    string[] partes = linea.Split(':');

                    if (partes.Length != 2 || !int.TryParse(partes[1], out contentLength))
                        continue;

                }
                else if (llinea.StartsWith("connection:"))
                {

                    string[] partes = linea.Split(':');

                    if (partes.Length != 2)
                        continue;

                    if (partes[1].Trim().ToLower() == "keep-alive")
                        Data.KeepAlive = Data.Data.KeepAlive;
                    else
                        Data.KeepAlive = false;

                }
                else if (llinea.StartsWith("transfer-encoding:"))
                {

                    string[] partes = linea.Split(':');

                    if (partes.Length != 2)
                        continue;

                    if (partes[1].Trim().ToLower() == "chunked")
                    {
                        contentLength = 0;
                        Data.Chunked = true;
                    }
                }

            }

            ///Si se cierra una conexión de servidor, es una conexión SSL y es KeepAlive
            ///se debería comenzar otro request??

            Data.ResponseLeft = contentLength;

            byte[] data;

            foreach (string s in lines)
            {
                if (s.StartsWith("proxy"))
                    continue;

                data = Encoding.ASCII.GetBytes(s + "\r\n");
                Data.Data.ClientSocket.Send(data);
#if ENABLEDEBUG 
                ClientSent(data, Data.Data);
#endif
            }

            data = Encoding.ASCII.GetBytes("\r\n");
            Data.Data.ClientSocket.Send(data);
#if ENABLEDEBUG 
                ClientSent(data, Data.Data);
#endif

            if (Data.Chunked)
            {

                string tam = "";

                int leidos = 0;

                while (!tam.EndsWith("\r\n"))
                {

                    leidos = Data.ServerSocket.Receive(Data.ServerBuffer, 1, SocketFlags.None);

                    if (leidos == 0)
                        throw new Exception();

                    tam += Encoding.ASCII.GetString(Data.ServerBuffer, 0, 1);
                }

                int tamChunk = int.Parse(tam.Substring(0, tam.Length - 2), System.Globalization.NumberStyles.HexNumber);

                Data.Data.ClientSocket.Send(Encoding.ASCII.GetBytes(tam));

#if ENABLEDEBUG
                ClientSent(Encoding.ASCII.GetBytes(tam), Data.Data); 
#endif

                Data.ResponseLeft = contentLength = tamChunk;

                if (tamChunk == 0)
                {

                    leidos = Data.ServerSocket.Receive(Data.ServerBuffer, 1, SocketFlags.None);

                    if (leidos == 0)
                        throw new Exception();

                    leidos = Data.ServerSocket.Receive(Data.ServerBuffer, 1, 1, SocketFlags.None);

                    if (leidos == 0)
                        throw new Exception();

                    Data.Data.ClientSocket.Send(Data.ServerBuffer, 2, SocketFlags.None);
#if ENABLEDEBUG
                    ClientSent(Data.ServerBuffer, 2, Data.Data); 
#endif
                }

            }

            string[] response = lines[0].Split(' ');

            int responseCode = int.Parse(response[1]);

            if ((responseCode > 99 && responseCode < 200) || responseCode == 204 || responseCode == 304)
                contentLength = 0;
            else if (contentLength == -1 && Data.Data.KeepAlive)
                Data.Data.KeepAlive = Data.KeepAlive = false;

            if (contentLength != 0)
                BeginRelayServer(Data);
            else
                BeginReadServerHeader(Data);


        }

        private void BeginRelayServer(SocketData Data)
        {
            try
            {
#if ENABLEDEBUG
                lock (Data.Data.debug)
                    Data.Data.debug.Add("BeginRelayServer");
#endif
                Data.ServerSocket.BeginReceive(Data.ServerBuffer, 0, Data.ServerBuffer.Length, SocketFlags.None, EndRelayServer, Data);
            }
            catch { }
        }

        private void EndRelayServer(IAsyncResult Result)
        {
            SocketData sckData = (SocketData)Result.AsyncState;

            if (sckData != sckData.Data.CurrentServer)
            {

                try
                {


                    sckData.ServerSocket.EndReceive(Result);

                }
                catch { }

                try
                {

                    sckData.ServerSocket.Shutdown(SocketShutdown.Both);
                    sckData.ServerSocket.Disconnect(false);
                    sckData.ServerSocket.Close();
                    sckData.ServerSocket.Dispose();
                }
                catch { }

                return;

            }

            ProxyData Data = sckData.Data;
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("EndRelayServer");
#endif
            try
            {

                int leidos = sckData.ServerSocket.EndReceive(Result);

                if (leidos == 0)
                {

                    CloseServer(Data);

                    if (!Data.KeepAlive || sckData.ResponseLeft != 0)
                        CloseClient(Data);

                    return;
                }
#if ENABLEDEBUG
                Data.serverReceived.AddRange(sckData.ServerBuffer.Take(leidos));
#endif

                int enviados = 0;
                //OJ
                if (sckData.ResponseLeft == -1)
                    Data.ClientSocket.Send(sckData.ServerBuffer, 0, leidos, SocketFlags.None);
                else
                {
                    enviados = Data.ClientSocket.Send(sckData.ServerBuffer, 0, Math.Min(leidos, sckData.ResponseLeft), SocketFlags.None);
#if ENABLEDEBUG
                    ClientSent(sckData.ServerBuffer, 0, Math.Min(leidos, sckData.ResponseLeft), Data); 
#endif
                }

                int inicio = sckData.ResponseLeft;
                int restantes = leidos - sckData.ResponseLeft;

                if (sckData.ResponseLeft > 0)
                    sckData.ResponseLeft -= Math.Min(leidos, sckData.ResponseLeft);

                

                if (sckData.ResponseLeft < 1 && Data.SourceRequest.Method.ToLower() != "connect" && (sckData.KeepAlive || Data.KeepAlive || sckData.Chunked))
                {
                    if (!sckData.Chunked)
                        BeginReadServerHeader(sckData);
                    else
                    {

                        string tam = "";
                        int pos = inicio;

                        int tamChunk = -1;

                    newChunk:
                        //aqui se atrapa el \r\n anterior
                        while (restantes > 0 && (!tam.EndsWith("\r\n") || tam.Length < 3))
                        {

                            restantes--;
                            tam += Encoding.ASCII.GetString(sckData.ServerBuffer, pos, 1);
                            pos++;

                        }

                        while (!tam.EndsWith("\r\n") || tam.Length < 3)
                        {
   
                            leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 1, SocketFlags.None);

                            if (leidos == 0)
                                throw new Exception();

                            tam += Encoding.ASCII.GetString(sckData.ServerBuffer, 0, 1);
                        }

                        tamChunk = int.Parse(tam.Substring(2, tam.Length - 4), System.Globalization.NumberStyles.HexNumber);

                        bool lastChunk = tamChunk == 0;

                        Data.ClientSocket.Send(Encoding.ASCII.GetBytes(tam));
#if ENABLEDEBUG
                        ClientSent(Encoding.ASCII.GetBytes(tam), Data); 
#endif
                        if (restantes > 0)
                        {

                            if (restantes >= tamChunk)
                            {

                                Data.ClientSocket.Send(sckData.ServerBuffer, pos, tamChunk, SocketFlags.None);
#if ENABLEDEBUG
                                ClientSent(sckData.ServerBuffer, pos, tamChunk, Data); 
#endif
                                restantes -= tamChunk;
                                pos += tamChunk;
                                tam = "";

                                if (lastChunk)
                                {
                                    if (restantes >= 2)
                                    {
                                        Data.ClientSocket.Send(sckData.ServerBuffer, pos, 2, SocketFlags.None);
#if ENABLEDEBUG
                                        ClientSent(sckData.ServerBuffer, pos, 2, Data); 
#endif
                                    }
                                    else if (restantes > 0)
                                    {
                                        Data.ClientSocket.Send(sckData.ServerBuffer, pos, 1, SocketFlags.None);
#if ENABLEDEBUG
                                        ClientSent(sckData.ServerBuffer, pos, 1, Data); 
#endif

                                        leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 1, SocketFlags.None);

                                        if (leidos == 0)
                                            throw new Exception();

                                        Data.ClientSocket.Send(sckData.ServerBuffer, 0, 1, SocketFlags.None);
#if ENABLEDEBUG
                                        ClientSent(sckData.ServerBuffer, 0, 1, Data); 
#endif
                                    }
                                    else
                                    {

                                        leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 1, SocketFlags.None);

                                        if (leidos == 0)
                                            throw new Exception();

                                        Data.ClientSocket.Send(sckData.ServerBuffer, 0, 1, SocketFlags.None);
#if ENABLEDEBUG
                                        ClientSent(sckData.ServerBuffer, 0, 1, Data); 
#endif

                                        leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 1, SocketFlags.None);

                                        if (leidos == 0)
                                            throw new Exception();

                                        Data.ClientSocket.Send(sckData.ServerBuffer, 0, 1, SocketFlags.None);
#if ENABLEDEBUG
                                        ClientSent(sckData.ServerBuffer, 0, 1, Data); 
#endif

                                    }

                                    if (restantes > 2)
                                        throw new Exception();

                                    lastChunk = false;

                                    BeginReadServerHeader(sckData);

                                    return;
                                }
                                else
                                    goto newChunk;
                            }

                            tamChunk -= Math.Min(restantes, tamChunk);

                            Data.ClientSocket.Send(sckData.ServerBuffer, pos, restantes, SocketFlags.None);
#if ENABLEDEBUG
                            ClientSent(sckData.ServerBuffer, pos, restantes, Data); 
#endif
                        }

                        sckData.ResponseLeft = tamChunk;

                        if (lastChunk)
                        {

                            leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 1, SocketFlags.None);

                            if (leidos == 0)
                                throw new Exception();

                            leidos = sckData.ServerSocket.Receive(sckData.ServerBuffer, 1, 1, SocketFlags.None);

                            if (leidos == 0)
                                throw new Exception();

                            Data.ClientSocket.Send(sckData.ServerBuffer, 2, SocketFlags.None);
#if ENABLEDEBUG
                            ClientSent(sckData.ServerBuffer, 2, Data); 
#endif
                            BeginReadServerHeader(sckData);
                        }
                        else
                            BeginRelayServer(sckData);

                    }
                }
                else
                    BeginRelayServer(sckData);
                

            }
            catch
            {
                CloseServer(sckData);

                if (!Data.KeepAlive || sckData.ResponseLeft != 0)
                    CloseClient(Data);
            }

        }
#if ENABLEDEBUG
        public void ClientSent(byte[] data, ProxyData Data)
        {

            Data.clientSent.AddRange(data);
        
        }

        public void ClientSent(byte[] sdata, int len, ProxyData Data)
        {

            byte[] data = new byte[len];
            Buffer.BlockCopy(sdata, 0, data, 0, len);
            Data.clientSent.AddRange(data);

        }
        public void ClientSent(byte[] sdata,int offset, int len, ProxyData Data)
        {

            byte[] data = new byte[len];
            Buffer.BlockCopy(sdata, offset, data, 0, len);
            Data.clientSent.AddRange(data);

        }

        public void WriteDebug(string FileName, ProxyData Data)
        {

            File.WriteAllBytes(FileName, Data.clientSent.ToArray());

        }
#endif

        private void CloseServer(SocketData Data)
        {
#if ENABLEDEBUG
            lock (Data.Data.debug)
                Data.Data.debug.Add("CloseServer SocketData");
#endif


            try
            {
                if (Data.Data.CurrentServer == Data)
                    Data.Data.Host = "";

                Data.ServerSocket.Shutdown(SocketShutdown.Both);
                Data.ServerSocket.Disconnect(false);
                Data.ServerSocket.Close();
                Data.ServerSocket.Dispose();
            }
            catch 
            {
            
            }
        }

        private void CloseServer(ProxyData Data)
        {
#if ENABLEDEBUG
            lock (Data.debug)
                Data.debug.Add("CloseServer ProxyData");
#endif

            try
            {
               Data.Host = "";
                Data.CurrentServer.ServerSocket.Shutdown(SocketShutdown.Both);
                Data.CurrentServer.ServerSocket.Disconnect(false);
                Data.CurrentServer.ServerSocket.Close();
                Data.CurrentServer.ServerSocket.Dispose();
            }
            catch { }
        }

    }

    public class ProxyData
    {
        public string Host;

        public int RequestLeft = 0;
        public byte[] ClientBuffer = new byte[128 * 1024];
        public Socket ClientSocket;
        public SocketData CurrentServer;
        public ManualResetEvent Event;
        public GusServerRequest SourceRequest;
        public bool KeepAlive;
        public bool SSL;
        public bool Chunked;
        public bool ClosedBySwap;
#if ENABLEDEBUG
        public List<string> debug = new List<string>();
        public List<string> requestheaders = new List<string>();
        public List<string> responseheaders = new List<string>();
        public List<byte> clientReceived = new List<byte>();
        public List<byte> serverReceived = new List<byte>();
        public List<byte> clientSent = new List<byte>();
        public bool ProxyKeepAlive;
#endif
    }

    public class SocketData
    {
        public bool Chunked = false;
        public int ResponseLeft = 0;
        public byte[] ServerBuffer = new byte[128 * 1024];
        public Socket ServerSocket;
        public ProxyData Data;
        public bool KeepAlive;
    }

}

