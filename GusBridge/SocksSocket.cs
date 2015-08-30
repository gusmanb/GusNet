/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace GusNet.GusBridge
{
    public class SOCKS5Socket : Socket
    {
        IPEndPoint socksAddress;
        string user;
        string password;

        private static string[] errorMsgs =	{
										"Operation completed successfully.",
										"General SOCKS server failure.",
										"Connection not allowed by ruleset.",
										"Network unreachable.",
										"Host unreachable.",
										"Connection refused.",
										"TTL expired.",
										"Command not supported.",
										"Address type not supported.",
										"Unknown error."
									};

        public SOCKS5Socket(EndPoint SOCKSAddress, string User, string Password) : base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            if (SOCKSAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new Exception("Only IPv4 supported");

            socksAddress = (IPEndPoint)SOCKSAddress;
            user = User;
            password = Password;
        
        }

        public new IAsyncResult BeginConnect(string RemoteAddress,int RemotePort, AsyncCallback CallBack, object State)
        {

            ConnectionResult Result = new ConnectionResult(RemoteAddress, (ushort)RemotePort, CallBack, State);
            ThreadPool.QueueUserWorkItem(DoConnect, Result);
            return Result;
        }

        public new void Connect(string RemoteAddres, int RemotePort)
        {

            SynchronousConnect(RemoteAddres, (ushort)RemotePort);
        
        }

        void DoConnect(object State)
        {

            ConnectionResult Result = State as ConnectionResult;

            try
            {
                string Address = Result.RemoteAddress;
                ushort RemotePort = Result.RemotePort;

                SynchronousConnect(Address, RemotePort);

                Result.handle.Set();

                if (Result.CallBack != null)
                    Result.CallBack(Result);

            }
            catch
            {
  
                if(Result.CallBack != null)
                    Result.CallBack(Result);
            
            }
        }

        //This code is based on a Internet found code, but can't find the 
        //original source to place credits and Copyright.
        //If you recognize this code, send a message to the project forum and
        //the credits and copyright will be added.
        //Thanks!

        private void SynchronousConnect(string Address, ushort RemotePort)
        {

            byte[] request = new byte[257];
            byte[] response = new byte[257];
            ushort nIndex;

            Connect(socksAddress);

            nIndex = 0;
            request[nIndex++] = 0x05; // Version 5.
            request[nIndex++] = 0x02; // 2 Authentication methods are in packet...
            request[nIndex++] = 0x00; // NO AUTHENTICATION REQUIRED
            request[nIndex++] = 0x02; // USERNAME/PASSWORD

            // Send the authentication negotiation request...
            Send(request, nIndex, SocketFlags.None);

            // Receive 2 byte response...
            int nGot = Receive(response, 2, SocketFlags.None);

            if (nGot != 2)
                throw new InvalidOperationException("Bad response received from proxy server.");

            if (response[1] == 0xFF)
            {	// No authentication method was accepted close the socket.
                Close();
                throw new InvalidOperationException("None of the authentication method was accepted by proxy server.");
            }

            byte[] rawBytes;

            if (response[1] == 0x02)
            {
                //Username/Password Authentication protocol
                nIndex = 0;
                request[nIndex++] = 0x05; // Version 5.

                // add user name
                request[nIndex++] = (byte)user.Length;
                rawBytes = Encoding.Default.GetBytes(user);
                rawBytes.CopyTo(request, nIndex);
                nIndex += (ushort)rawBytes.Length;

                // add password
                request[nIndex++] = (byte)password.Length;
                rawBytes = Encoding.Default.GetBytes(password);
                rawBytes.CopyTo(request, nIndex);
                nIndex += (ushort)rawBytes.Length;

                // Send the Username/Password request
                Send(request, nIndex, SocketFlags.None);
                // Receive 2 byte response...
                nGot = Receive(response, 2, SocketFlags.None);
                if (nGot != 2)
                    throw new InvalidOperationException("Bad response received from proxy server.");
                if (response[1] != 0x00)
                    throw new InvalidOperationException("Bad Usernaem/Password.");
            }

            // This version only supports connect command. 
            // UDP and Bind are not supported.

            // Send connect request now...
            nIndex = 0;
            request[nIndex++] = 0x05;	// version 5.
            request[nIndex++] = 0x01;	// command = connect.
            request[nIndex++] = 0x00;	// Reserve = must be 0x00

            IPAddress destIP = null;
            
            if (IPAddress.TryParse(Address, out destIP))
            {
                // Destination adress in an IP.
                switch (destIP.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        // Address is IPV4 format
                        request[nIndex++] = 0x01;
                        rawBytes = destIP.GetAddressBytes();
                        rawBytes.CopyTo(request, nIndex);
                        nIndex += (ushort)rawBytes.Length;
                        break;
                    case AddressFamily.InterNetworkV6:
                        // Address is IPV6 format
                        request[nIndex++] = 0x04;
                        rawBytes = destIP.GetAddressBytes();
                        rawBytes.CopyTo(request, nIndex);
                        nIndex += (ushort)rawBytes.Length;
                        break;
                }
            }
            else
            {
                // Dest. address is domain name.
                request[nIndex++] = 0x03;	// Address is full-qualified domain name.
                request[nIndex++] = Convert.ToByte(Address.Length); // length of address.
                rawBytes = Encoding.Default.GetBytes(Address);
                rawBytes.CopyTo(request, nIndex);
                nIndex += (ushort)rawBytes.Length;
            }

            // using big-edian byte order
            byte[] portBytes = BitConverter.GetBytes(RemotePort);
            for (int i = portBytes.Length - 1; i >= 0; i--)
                request[nIndex++] = portBytes[i];

            // send connect request.
            Send(request, nIndex, SocketFlags.None);
            Receive(response);	// Get variable length response...

            if (response[1] != 0x00)
                throw new Exception(errorMsgs[response[1]]);
        }

        public new void EndConnect(IAsyncResult Result)
        {

            ConnectionResult res = Result as ConnectionResult;

            res.handle.Dispose();

            if (res.Ex != null)
                throw res.Ex;
        
        }
    }

    public class ConnectionResult : IAsyncResult
    {

        internal string RemoteAddress;
        internal ushort RemotePort;
        internal AsyncCallback CallBack;
        internal object State;
        internal bool Failed = false;

        internal Exception Ex;

        internal ManualResetEvent handle = new ManualResetEvent(false);

        internal ConnectionResult(string RemoteAddress, ushort RemotePort, AsyncCallback CallBack, object State)
        {

            this.RemoteAddress = RemoteAddress;
            this.RemotePort = RemotePort;
            this.CallBack = CallBack;
            this.State = State;
        
        }

        public object  AsyncState
        {
	        get { return State; }
        }

        public System.Threading.WaitHandle  AsyncWaitHandle
        {
            get { return handle; }
        }

        public bool  CompletedSynchronously
        {
	        get { return true; }
        }

        public bool  IsCompleted
        {
	        get { return true; }
        }
    }
}
