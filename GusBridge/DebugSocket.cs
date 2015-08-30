/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace GusNet.GusBridge
{
    public class DebugSocket : Socket
    {
        public List<byte> enviado = new List<byte>();

        public DebugSocket(AddressFamily Family, SocketType Type, ProtocolType Protocol) : base(Family, Type, Protocol) { }

        public new int Send(byte[] Data)
        {

            enviado.AddRange(Data);
            return base.Send(Data);
        
        }

        public new int Send(byte[] Data, SocketFlags Flags)
        {
            
            enviado.AddRange(Data);
            return base.Send(Data, Flags);

        }

        public new int Send(byte[] Data, int Length, SocketFlags Flags)
        {
            byte[] data = new byte[Length];

            Buffer.BlockCopy(Data, 0, data, 0, Length);

            enviado.AddRange(data);

            if (Length == 0)
                Debug.Print("Cero!");

            return base.Send(Data, Length, Flags);
        
        }

        public new int Send(byte[] Data,int Offset, int Length, SocketFlags Flags)
        {
            byte[] data = new byte[Length];

            Buffer.BlockCopy(Data, Offset, data, 0, Length);

            enviado.AddRange(data);

            if (Length == 0)
                Debug.Print("Cero!");

            return base.Send(Data, Offset, Length, Flags);

        }

        public void WriteDebug(string FileName)
        {

            File.WriteAllBytes(FileName, enviado.ToArray());
        
        }

    }
}
