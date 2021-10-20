using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PChat.Clients_Mess
{
    public class Message
    {
        public const char CONNECT = '0';
        public const char MESSAGE = '1';
        public const char DISCONNECT = '2';
        public const char GET_HISTORY = '3';
        public const char SHOW_HISTORY = '4';
        public char code { get; }
        public string data { get; }

        public Message(char mesCode, string mesData)
        {
            code = mesCode;
            data = mesData;
        }

    }
}
