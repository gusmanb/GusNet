/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace GusNet.GusBridge
{
    class DebugList
    {

        static Dictionary<Guid, string> lists = new Dictionary<Guid, string>();

        public static void Write(Guid Id, string Message)
        {

            lock (lists)
            {

                if (!lists.ContainsKey(Id))
                    lists[Id] = Clean(Message) + "\r\n";
                else
                    lists[Id] += Clean(Message) + "\r\n";

            }
        }

        private static string Clean(string Message)
        {
            if (Message != null && Message.Length > 0)
            {
                StringBuilder sb = new StringBuilder(Message.Length);
                foreach (char c in Message)
                {
                    sb.Append(Char.IsControl(c) && c !='\r' && c!= '\n' ? ' ' : c);
                }

                Message = sb.ToString();
            }

            return Message;
        }
        static object locker = new object();

        public static void Print(Guid Id)
        {
            lock (locker)
            {
                Debug.WriteLine(Id.ToString() + "--<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-");
                Debug.Print(lists[Id]);
                Debug.WriteLine(Id.ToString() + "--<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-<-");
            }
        }

    }
}
