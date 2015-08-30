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
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace GusNet.GusBridge
{

    public class GusBridgePath : GusProxyPath
    {

        IPAddress socksAddress;
        int port;
        string user = "";
        string password = "";

        public GusBridgePath(IPAddress Socks5Address, int Port, string User, string Password)
        {
            socksAddress = Socks5Address;
            port = Port;
            user = User;
            password = Password;
        }
        
        protected override Socket CreateSocket(string Host, int Port)
        {
            SOCKS5Socket sck = new SOCKS5Socket(new IPEndPoint(socksAddress, this.port), user, password);

            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(true, 5));

            sck.ExclusiveAddressUse = true;

            sck.Connect(Host, Port);

            return sck;
        }
        
    }

}
