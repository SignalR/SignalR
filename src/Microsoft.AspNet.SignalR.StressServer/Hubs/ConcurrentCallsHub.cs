// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.StressServer.Hubs
{
    public class ConcurrentCallsHub : Hub
    {
        public void AsyncEcho(string str)
        {
            Clients.Caller.AsyncEcho(str);
        }

        public void EchoAll(string str, DataClass c)
        {
            Clients.All.EchoAll(str, c, c.GetHashCode());
        }

        public void EchoMessage(string str, DataClass c)
        {
            Clients.Client(Context.ConnectionId).Echo(str, c);
        }

        public void EchoCaller(string str, DataClass c)
        {
            Clients.Caller.Echo(str, c);
        }
    }

    [Serializable]
    public class DataClass
    {
        private static object syncLock = new object();
        private static Random random = new Random();

        public string TheString
        {
            get;
            set;
        }

        public int TheLong
        {
            get;
            set;
        }

        public double TheDouble
        {
            get;
            set;
        }

        public Guid TheGuid
        {
            get;
            set;
        }

        public byte[] TheByteArray
        {
            get;
            set;
        }

        public List<Guid> TheList
        {
            get;
            set;
        }

        public static DataClass CreateDataClass()
        {
            int rnd;
            int number;
            lock (syncLock)
            {
                rnd = random.Next(8) + 1;
                number = random.Next();
            }

            string str = string.Empty;
            Guid guid = Guid.NewGuid();

            for (int i = 0; i < rnd; i++)
            {
                str += guid;
            }

            DataClass c = new DataClass();

            c.TheString = str;
            c.TheDouble = random.NextDouble();
            c.TheLong = number * 2;
            c.TheGuid = guid;
            c.TheByteArray = Encoding.ASCII.GetBytes(str);
            c.TheList = new List<Guid>();

            for (int i = 0; i < rnd; i++)
            {
                c.TheList.Add(guid);
            }

            return c;
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();

            foreach (PropertyInfo p in this.GetType().GetProperties())
            {
                object o = p.GetValue(this, null);

                if (o != null)
                {
                    b.AppendLine(string.Format("{0}: {1}", p.Name, o.ToString()));
                }
            }

            return b.ToString();
        }
    }
}
