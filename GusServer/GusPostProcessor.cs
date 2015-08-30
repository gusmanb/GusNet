/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace GusNet.GusServer
{
    public class GusPostProcessor
    {

        public bool Success { get; private set; }

        public GusPostProcessor(Stream Stream, string ContentType)
        {
            this.Parse(Stream, Encoding.UTF8, ContentType);
        }

        public GusPostProcessor(Stream Stream, Encoding Encoding, string ContentType)
        {
            this.Parse(Stream, Encoding, ContentType);
        }

        private Dictionary<string, string> variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Variables
        {
            get { return variables; }
            set { variables = value; }
        }

        private Dictionary<string, GusPostFile> files = new Dictionary<string, GusPostFile>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, GusPostFile> Files
        {
            get { return files; }
        }

        private void Parse(Stream Stream, Encoding Encoding, string ContentType)
        {

            if (!ContentType.StartsWith("multipart/"))
                return;

            string delimiter;

            try { delimiter = "--" + ContentType.Split(';')[1].Trim().Split("=".ToCharArray(), 2)[1]; }
            catch { return; }

            if (delimiter.Length > 3)
            {

                byte[] delimiterBytes = Encoding.GetBytes(delimiter);

                int startIndex = 0;

                while (startIndex != -1)
                {
                    int endIndex = IndexOf(Stream, delimiterBytes, startIndex + 1);

                    if (endIndex != -1)
                    {
                        string file = CopyToTempFile(Stream, (startIndex + delimiter.Length + 2), endIndex - (startIndex + delimiter.Length + 2));

                        Stream str = File.OpenRead(file);

                        ParsePart(str, Encoding);

                        str.Close();

                        File.Delete(file);

                    }

                    startIndex = endIndex;

                }

                Success = true;

            }
        }

        private string CopyToTempFile(Stream SourceStream, int Start, int Length)
        {

            string file = Path.GetTempFileName();

            Stream destStream = File.Create(file);

            SourceStream.Seek(Start, SeekOrigin.Begin);

            int bytesLeft = Length;

            byte[] buffer = new byte[1024 * 100];

            while (bytesLeft > 0)
            {
                int toRead = Math.Min(bytesLeft, buffer.Length);

                int toWrite = SourceStream.Read(buffer, 0, toRead);

                destStream.Write(buffer, 0, toWrite);

                bytesLeft -= toRead;
            }

            destStream.Close();

            return file;

        }

        private byte[] ReadBlock(Stream Stream, int Start, int Length)
        {

            byte[] b = new byte[Length];
            Stream.Seek(Start, SeekOrigin.Begin);
            Stream.Read(b, 0, Length);
            return b;
        
        }

        private void ParsePart(Stream PartData, Encoding Encoding)
        {
            byte[] data = Encoding.GetBytes("\r\n\r\n");

            int dataStart = IndexOf(PartData, data, 0);

            if (dataStart == -1)
                return;

            string info = Encoding.GetString(ReadBlock(PartData, 0, dataStart));

            Regex re = new Regex(@"(?<=Content\-Type:)(.*)[^\r\n]");
            Match contentTypeMatch = re.Match(info);

            re = new Regex(@"(?<=filename\=\"")(.*?)(?=\"")");
            Match filenameMatch = re.Match(info);

            re = new Regex(" name=\"([^\"]*?)\"");
            Match varnameMatch = re.Match(info);

            if (contentTypeMatch.Success && filenameMatch.Success && varnameMatch.Success)
            {

                dataStart += 4;

                int length = (int)(PartData.Length - dataStart - 2);


                string tmpfile = null;
                
                if(filenameMatch.Value.Trim().Length > 0)
                    tmpfile = CopyToTempFile(PartData, dataStart, length);

                GusPostFile file = new GusPostFile { ContentType = contentTypeMatch.Value.Trim(), FileName = filenameMatch.Value.Trim(), TempFile = tmpfile };

                files.Add(varnameMatch.Groups[1].Value.Trim(), file);
            }
            else
            {

                string part = Encoding.GetString(ReadBlock(PartData, 0, (int)PartData.Length));
                Regex re2 = new Regex("Content-Disposition: form-data; name=\"([^\"]*?)\"\r\n\r\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                Match m = re2.Match(part);

                if (m.Success)
                {

                    string variable = m.Groups[1].Value;
                    string value = part.Substring(part.IndexOf("\r\n\r\n") + 4, part.Length - (part.IndexOf("\r\n\r\n") + 6));
                    variables.Add(variable, value);
                }
            }

        }

        private int IndexOf(Stream SearchWithin, byte[] searchFor, int startIndex)
        {
            int index = 0;
            int startPos = IndexOf(SearchWithin, searchFor[0], startIndex);

            if (startPos != -1)
            {
                while ((startPos + index) < SearchWithin.Length)
                {
                    if (ByteAt(SearchWithin, startPos + index) == searchFor[index])
                    {
                        index++;
                        if (index == searchFor.Length)
                        {
                            return startPos;
                        }
                    }
                    else
                    {
                        startPos = IndexOf(SearchWithin, searchFor[0], startPos + 1);
                        if (startPos == -1)
                        {
                            return -1;
                        }
                        index = 0;
                    }
                }
            }

            return -1;
        }

        private byte ByteAt(Stream searchWithin, int Pos)
        {

            searchWithin.Seek(Pos, SeekOrigin.Begin);
            return (byte)searchWithin.ReadByte();
        
        }

        private int IndexOf(Stream stream, byte searchFor, int startIndex)
        { 
            int pos = -1;
            stream.Seek(startIndex, SeekOrigin.Begin);

            while (pos == -1 && stream.Position < stream.Length)
            {

                int b = stream.ReadByte();

                if (b == searchFor)
                    pos = (int)stream.Position - 1;
            
            }

            return pos;

        }

        private byte[] ToByteArray(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);

                    if (read <= 0)
                        return ms.ToArray();

                    ms.Write(buffer, 0, read);
                }
            }
        }

    }

    public class GusPostFile
    {

        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string TempFile { get; set; }
    }
}
