using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace P2PChat.Additional
{
    static class GetIp
    {
        public static List<IPAddress> GetIPList()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] ipAdrs = Dns.GetHostAddresses(hostName);
            List<IPAddress> IpV4 = new List<IPAddress>();
            foreach(var p in ipAdrs)
            {
                if (p.AddressFamily == AddressFamily.InterNetwork)
                    IpV4.Add(p);
            }

            return IpV4;
        }

    }
}
