using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace proxy_server
{
    class Program
    {
        static void Main(string[] args)
        {
            Proxy proxyServer = new Proxy("127.0.0.1", 9999);
            proxyServer.Listen();
            while (true)
            {
                Socket socket = proxyServer.Accept();
                Thread thread = new Thread(() => proxyServer.ReceiveData(socket));
                thread.Start();
            }

        }
    }
}
