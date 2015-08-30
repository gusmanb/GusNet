using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections;
using System.Net.Sockets;
using System.Web;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

namespace GusNet.GusServer
{
    

    public abstract class GusHttpServer
    {

        protected int Port;

        TcpListener listener;
        bool isActive = true;

        List<GusHttpProcessor> runningInstances = new List<GusHttpProcessor>();

        //Thread th;

        protected bool IsSsl = false; // <- Lol!
        protected string CertFile;

        private int maxPostSize = 2 * 1024 * 1024;

        public int MaxPostSize { get { return maxPostSize; } set { maxPostSize = value; } } 

        public GusHttpServer(int Port, bool UseSsl, string CertificateFile)
        {
            this.Port = Port;
            IsSsl = UseSsl;
            CertFile = CertificateFile;
        }

        public bool Start()
        {

            //if (th != null)
             //   return false;

            try {

                listener = new TcpListener(Port);
                listener.Server.ExclusiveAddressUse = true;
                listener.Start();
            
            }
            catch { return false; }

            ThreadPool.SetMaxThreads(2000, 2000);
            
            ThreadPool.QueueUserWorkItem(InternalListen);

            //I have tried both methods, Threads and ThreadPool, each one has its advantages and inconvenients.
            //Choose the metod best fits your needs

            //th = new Thread(internalListen);
            //th.Start();

            return true;
        }

        public void Stop()
        {

            //if (th == null)
            //    return;

            if (listener == null)
                return;

            isActive = false;
            listener.Stop();
            listener.Server.Close();

            //th.Abort();

            //th = null;

            lock (runningInstances)
            {
                var inst = runningInstances.ToArray();

                foreach (var v in inst)
                    v.Abort();

            }

        }

        private void InternalListen(object state)
        {
            try
            {
                
                while (isActive)
                {

                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
                    

                    Socket s = listener.AcceptSocket();
                    
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, 5));

                    GusHttpProcessor processor = new GusHttpProcessor(s, this, IsSsl);
                    processor.MaxPostSize = maxPostSize;

                    processor.Processed += new EventHandler(processor_Processed);

                    lock (runningInstances)
                        runningInstances.Add(processor);

                    processor.StartProcess();

                }
            }
            catch { }
        }

        void processor_Processed(object sender, EventArgs e)
        {
            lock (runningInstances)
                runningInstances.Remove((GusHttpProcessor)sender);
        }

        public abstract void HandleRequest(GusHttpProcessor Processor);
    }

    public class GusHttpProcessor
    {

        public byte[] OriginalRequest
        {

            get;
            internal set;

        }

        public string PostDataFile
        {

            get;
            internal set;
        }

        public bool IsSecure
        {
            get;
            private set;
        }

        private Socket socket;

        public Socket Socket
        {
            get { return socket; }
        }

        private GusHttpServer srv;

        public GusHttpServer Server
        {
            get { return srv; }
        }

        string remoteIP;

        public string RemoteIP
        {
            get { return remoteIP; }
        }

        private Stream inputStream;

        public Stream InputStream
        {
            get { return inputStream; }
            set { inputStream = value; }
        }
        private GusOutputStream outputStream;

        public GusOutputStream OutputStream
        {
            get { return outputStream; }
        }

        private string method;

        public string Method
        {
            get { return method; }
        }
        private string sourceUrl;

        public string SourceUrl
        {
            get { return sourceUrl; }
        }
        private string httpProtocolVersion;

        public string HttpProtocolVersion
        {
            get { return httpProtocolVersion; }
        }

        private Dictionary<string, string> requestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> RequestHeaders
        {
            get { return requestHeaders; }
        }

        public event EventHandler Processed;

        private int maxPostSize = 2 * 1024 * 1024;

        public int MaxPostSize
        {
            get { return maxPostSize; }
            set { maxPostSize = value; }
        }

        Thread th;

        string certFile;

        private List<byte> requestBytes = new List<byte>();

        static X509Certificate cert;

        private const int BufferSize = 4096;

        internal GusHttpProcessor(Socket Client, GusHttpServer Server, bool IsSecure, string CertificateFile = null)
        {
            this.IsSecure = IsSecure;
            this.socket = Client;
            this.srv = Server;
            this.certFile = CertificateFile;
        }

        DateTime start;

        internal void StartProcess()
        {

            //I have tried both methods, Threads and ThreadPool, each one has its advantages and inconvenients.
            //Choose the metod best fits your needs

            //ThreadPool.QueueUserWorkItem(process);

            th = new Thread(process);
            th.Start();
            start = DateTime.Now;
        }

        internal void Abort()
        {
            if (th == null)
                return;

            th.Abort();

            try
            {
                socket.Close();
            }
            catch { }

            if (Processed != null)
                Processed(this, EventArgs.Empty);

        }

        private string ReadLine(Stream inputStream, List<byte> Data)
        {
            int next_char;
            string data = "";

            byte[] b = new byte[1];
            while (true)
            {

                next_char = inputStream.Read(b, 0, 1);

                if (next_char == 0)
                    return null;

                Data.Add(b[0]);

                next_char = b[0];

                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }

                data += Convert.ToChar(next_char);
            }
            return data;
        }

        private void process(object State)
        {


            try
            {

                var stream = new NetworkStream(socket);

                remoteIP = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();

                if (IsSecure)
                {

                    if (cert == null)
                    {

                        if (certFile == null)
                        {

                            if (File.Exists("selfsigned.cer"))
                                cert = X509Certificate.CreateFromCertFile("selfsigned.cer");
                            else
                            {
                                cert = GusCertificate.CreateSelfSignedCertificate(".");
                                byte[] cData = cert.Export(X509ContentType.Cert);
                                File.WriteAllBytes("selfsigned.cer", cData);
                            }
                        }
                        else
                            cert = X509Certificate.CreateFromCertFile(certFile);

                    }


                    SslStream sslStream = new SslStream(stream);
                    sslStream.AuthenticateAsServer(cert, false, System.Security.Authentication.SslProtocols.Default, false);
                    while (!sslStream.IsAuthenticated)
                    {

                        Thread.Sleep(1);

                    }
                    inputStream = sslStream;
                    outputStream = new GusOutputStream(sslStream);


                }
                else
                {

                    inputStream = stream;
                    outputStream = new GusOutputStream(stream);

                }

                try
                {
                    List<byte> originalRequest = new List<byte>();

                    parseRequest(originalRequest);

                    if (originalRequest.Count == 0)
                        return;

                    readHeaders(originalRequest);

                    OriginalRequest = originalRequest.ToArray();
                    originalRequest = null;

                    if (OriginalRequest != null && OriginalRequest.Length > 0)
                    {

                        if (method.ToLower().Equals("post"))
                        {
                            PostDataFile = HandlePost();

                            if (PostDataFile != null)
                                Server.HandleRequest(this);

                        }
                        else
                            Server.HandleRequest(this);
                    }
                }
                catch
                {
                    WriteError();
                }

                if (PostDataFile != null)
                    File.Delete(PostDataFile);

                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Disconnect(false);
                    socket.Close();
                    socket.Dispose();

                    inputStream.Close();
                    inputStream.Dispose();
                    outputStream.Close();
                    outputStream.Dispose();

                    inputStream = null;
                    outputStream = null;
                }
                catch { }

            }
            catch { }


            if (Processed != null)
                Processed(this, EventArgs.Empty);

        }

        private void parseRequest(List<byte> Data)
        {
            string request = ReadLine(inputStream, Data);

            Debug.Print(request);

            if (request == null)
                return;

            string[] parts = request.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                throw new Exception("Invalid request");


            method = parts[0].ToUpper();
            sourceUrl = parts[1];
            httpProtocolVersion = parts[2];

        }

        private void readHeaders(List<byte> Data)
        {

            string line;
            while ((line = ReadLine(inputStream, Data)) != null)
            {
                if (line.Equals(""))
                    return;

                int separator = line.IndexOf(':');

                if (separator == -1)
                    throw new Exception("Invalid request line: " + line);

                string name = line.Substring(0, separator);
                int pos = separator + 1;

                while ((pos < line.Length) && (line[pos] == ' '))
                    pos++;

                string value = line.Substring(pos, line.Length - pos);
                requestHeaders[name] = value;
            }
        }

        private string HandlePost()
        {

            int contentLen = 0;

            string file = Path.GetTempFileName();

            Stream str = File.OpenWrite(file);

            try
            {

                if (this.requestHeaders.ContainsKey("Content-Length"))
                {
                    contentLen = Convert.ToInt32(this.requestHeaders["Content-Length"]);

                    if (contentLen > maxPostSize)
                    {
                        WritePostLong();
                        return null;

                    }

                    byte[] buf = new byte[BufferSize];
                    int toRead = contentLen;

                    while (toRead > 0)
                    {

                        int numread = this.inputStream.Read(buf, 0, Math.Min(BufferSize, toRead));

                        if (numread == 0)
                        {
                            if (toRead == 0)
                                break;
                            else
                                throw new Exception("Client disconnected");
                        }

                        toRead -= numread;
                        str.Write(buf, 0, numread);
                    }

                    return file;

                }
                else if (this.requestHeaders.ContainsKey("Transfer-Encoding") && this.requestHeaders["Transfer-Encoding"].ToLower() == "chunked")
                {

                    //Take care of the Post size limit!!!

                    bool lastChunk = false;

                    string size = "";

                    byte[] tmpBuf = new byte[1];
                    byte[] buf = new byte[BufferSize];

                    int totalLen = 0;


                    while (!lastChunk)
                    {
                        int leidos = this.inputStream.Read(tmpBuf, 0, 1);

                        while (!size.EndsWith("\r\n"))
                        {
                            totalLen += leidos;

                            if (leidos == 0)
                                throw new Exception("Client disconnected");

                            size += Encoding.ASCII.GetString(tmpBuf);
                            leidos = this.inputStream.Read(tmpBuf, 0, 1);
                        }

                        int tamChunk = int.Parse(size.Substring(0, size.Length - 2), System.Globalization.NumberStyles.HexNumber);

                        if (tamChunk == 0)
                            lastChunk = true;
                        else
                        {

                            while (tamChunk > 0)
                            {

                                int numread = this.inputStream.Read(buf, 0, Math.Min(BufferSize, tamChunk));

                                if (numread == 0)
                                {
                                    if (tamChunk == 0)
                                        break;
                                    else
                                        throw new Exception("Client disconnected");
                                }

                                tamChunk -= numread;
                                str.Write(buf, 0, numread);
                            }

                        }
                    }

                    //Last \r\n
                    this.inputStream.Read(buf, 0, 2);

                    return file;
                }
                else
                    return null;

            }
            catch { return null; }
            finally { str.Close(); }

        }

        /// <summary>
        /// Generic 200 response
        /// </summary>
        /// <param name="ContentType">Response content type, if not passed defaults to text/html</param>
        public void WriteSuccess(string ContentType = "text/html", List<KeyValuePair<string, string>> Headers = null)
        {
            outputStream.WriteLine("HTTP/1.1 200 OK");
            outputStream.WriteLine("Content-Type: " + ContentType);

            if (Headers != null)
            {

                foreach (var head in Headers)
                    outputStream.WriteLine(head.Key.Trim() + ": " + head.Value.Trim());

            }

            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");

        }

        /// <summary>
        /// Generic 404 response
        /// </summary>
        public void WriteNotFound(Dictionary<string, string> Headers = null)
        {
            outputStream.WriteLine("HTTP/1.1 404 File not found");
            if (Headers != null)
            {

                foreach (var head in Headers)
                    outputStream.WriteLine(head.Key.Trim() + ": " + head.Value.Trim());

            }
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
        }

        private void WritePostLong()
        {
            outputStream.WriteLine("HTTP/1.1 500 Too long POST request");
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
            outputStream.WriteLine(@"<html><body><h1>HTTP Error 500 - Internal server error</h1><div><p>Request is too long</p></div></body></html>");
        }

        /// <summary>
        /// Generic 500 response
        /// </summary>
        public void WriteError(Dictionary<string, string> Headers = null)
        {
            try
            {
                outputStream.WriteLine("HTTP/1.1 500 Internal error");
                if (Headers != null)
                {

                    foreach (var head in Headers)
                        outputStream.WriteLine(head.Key.Trim() + ": " + head.Value.Trim());

                }
                outputStream.WriteLine("Connection: close");
                outputStream.WriteLine("");
                outputStream.WriteLine(@"<html><body><h1>HTTP Error 500 - Internal server error</h1><div><p>Unknown error</p></div></body></html>");
            }
            catch { }
        }

        /// <summary>
        /// Generic 500 response
        /// </summary>
        public void WriteResponseCode(string Code, Dictionary<string, string> Headers = null)
        {
            outputStream.WriteLine("HTTP/1.1 " + Code);
            if (Headers != null)
            {

                foreach (var head in Headers)
                    outputStream.WriteLine(head.Key.Trim() + ": " + head.Value.Trim());

            }
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
        }
    }

    public class GusOutputStream : Stream
    {


        Stream str;

        public GusOutputStream(Stream BaseStream)
        {
            str = BaseStream;
        }

        public override bool CanRead
        {
            get { return str.CanRead; }
        }

        public override bool CanSeek
        {
            get { return str.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return str.CanWrite; }
        }

        public override void Flush()
        {
            str.Flush();
        }

        public override long Length
        {
            get { return str.Length; }
        }

        public override long Position
        {
            get
            {
                return str.Position;
            }
            set
            {
                str.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return str.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return str.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            str.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            str.Write(buffer, offset, count);
        }

        public void WriteText(string Text)
        {

            byte[] data = Encoding.Default.GetBytes(Text);
            str.Write(data, 0, data.Length);
        
        }

        public void WriteText(string Text, Encoding Encode)
        {

            byte[] data = Encode.GetBytes(Text);
            str.Write(data, 0, data.Length);

        }

        public void WriteLine(string Text)
        {

            byte[] data = Encoding.Default.GetBytes(Text + "\r\n");
            str.Write(data, 0, data.Length);

        }

        public void WriteLine(string Text, Encoding Encode)
        {

            byte[] data = Encode.GetBytes(Text + "\r\n");
            str.Write(data, 0, data.Length);

        }
    }

}
