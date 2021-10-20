using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.Net.NetworkInformation;
using P2PChat.Clients_Mess;

namespace P2PChat.Protocols
{
    class Protocol
    {
        public delegate void UpdateWindowChat(string text);

        private const int udpPort = 33333;
        private const int tcpPort = 11111;
        private string myOwnLogin;
        private List<Client> clients = new List<Client>();
        public IPAddress chooseIP;
        public UpdateWindowChat updateChat;
        private StringBuilder chatHistory;
        private DateTime currentTime;
        private readonly SynchronizationContext synchronization;

        public Protocol(UpdateWindowChat del)
        {
            updateChat = del;
            chatHistory = new StringBuilder();
            currentTime = new DateTime();
            synchronization = SynchronizationContext.Current;
        }

        public void JoinChat(string login)
        {
            IPEndPoint originIP = new IPEndPoint(chooseIP, udpPort);
            IPEndPoint destinationIP = new IPEndPoint(MakeBroadcastAdr(chooseIP), udpPort);

            // новый клиент
            UdpClient udpClient = new UdpClient(originIP);

            // принятие широковещательных пакетов
            udpClient.EnableBroadcast = true;

            myOwnLogin = login;
            byte[] connectMessBytes = Encoding.UTF8.GetBytes(login);

            try
            {
                // отправка широковещательных пакетов с логином 
                udpClient.Send(connectMessBytes, connectMessBytes.Length, destinationIP);

                // закрытие подключения
                udpClient.Close();

                currentTime = DateTime.Now;
                string connectMess = $"{currentTime} : IP [{chooseIP}] {login} join chat\n";

                // добавление к истории 
                chatHistory.Append(connectMess);
                updateChat($"{currentTime} : IP [{chooseIP}] You ({login}) join chat\n");

                // новый поток с принятием сообщений
                Task recieveUdpBroadcast = new Task(ReceiveBroadcast);
                recieveUdpBroadcast.Start();

                Task recieveTCP = new Task(ReceiveTCP);
                recieveTCP.Start();
            }
           catch
           {
               MessageBox.Show("Sending Error!", "BAD", MessageBoxButton.OKCancel);
           }
        }

        // принимаем широковещательный пакет
        private void ReceiveBroadcast()
        {
            IPEndPoint originIP = new IPEndPoint(chooseIP, udpPort);
            IPEndPoint destinationIP = new IPEndPoint(IPAddress.Any, udpPort);
            UdpClient udpReceiver = new UdpClient(originIP);
            

            while(true)
            {
                // принимаем сообщение в буфер
                byte[] receivedData = udpReceiver.Receive(ref destinationIP);

                // устанавливает логин клиента
                string clientLogin = Encoding.UTF8.GetString(receivedData);
                Client newClient = new Client(clientLogin, destinationIP.Address, tcpPort); 

                // установка соединения
                newClient.MakeConnection();  
                
                // список клиентов 
                clients.Add(newClient);

                // ответ на udp-запрос (tcp-ответ)
                newClient.SendMessage(new Message(Message.CONNECT, myOwnLogin));

                currentTime = DateTime.Now;
                string infoMess = $"{currentTime} : IP [{newClient.IP}] {newClient.login} connect chat\n";

                synchronization.Post(delegate { updateChat(infoMess); }, null);
                

                Task.Factory.StartNew(() => ListenClient(newClient));
            }
        }


        // принимаем сообщения TCP
        private void ReceiveTCP()
        {
            TcpListener tcpListener = new TcpListener(chooseIP, tcpPort);
            tcpListener.Start();

            while(true)
            {
                TcpClient tcpNewClient = tcpListener.AcceptTcpClient();
                Client newClient = new Client(tcpNewClient, tcpPort);

                Task.Factory.StartNew(() => ListenClient(newClient));
            }

        }
        
        // прослушивание (получаем сообщение)
        private void ListenClient(Client client)
        {
            while (true)
            {
                
                    Message tcpMessage = client.ReceiveMessage();
                    string infoMes;

                    switch (tcpMessage.code)
                    {
                        case Message.CONNECT: 
                            client.login = tcpMessage.data; // в дате - логин отправителя
                            clients.Add(client);
                            GetHistoryMessageToConnect(client); // запрос на историю
                            break;

                        case Message.MESSAGE:
                            currentTime = DateTime.Now;
                            infoMes = $"{currentTime} : IP [{client.IP}] {client.login} : {tcpMessage.data}\n";
                            synchronization.Post(delegate { updateChat(infoMes); chatHistory.Append(infoMes); }, null);
                            break;

                        case Message.DISCONNECT:
                            currentTime = DateTime.Now;
                            infoMes = $"{currentTime} : IP [{client.IP}] {client.login} left chat\n";
                            synchronization.Post(delegate { updateChat(infoMes); chatHistory.Append(infoMes); }, null);
                            clients.Remove(client);
                            return;

                        case Message.GET_HISTORY:
                            SendHistoryMessage(client);
                            break;

                        case Message.SHOW_HISTORY:
                            synchronization.Post(delegate { updateChat(tcpMessage.data); chatHistory.Append(tcpMessage.data); }, null);
                            break;

                        default:
                            MessageBox.Show("Incorrect message format", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }
                
            }
        }

        public void SendHistoryMessage(Client client)
        {
            Message historyMessage = new Message(Message.SHOW_HISTORY, chatHistory.ToString());
            client.SendMessage(historyMessage);
        }

        public void GetHistoryMessageToConnect(Client client)
        {
            Message historyMessage = new Message(Message.GET_HISTORY, "");
            client.SendMessage(historyMessage);
        }

        public void SendDisconnectMessage()
        {
            string disconnectStr = $"{myOwnLogin} left";
            Message disconnectMes = new Message(Message.DISCONNECT, disconnectStr);
            SendMessageToAllClients(disconnectMes);
        }

        public void SendOriginalMessage(string mes)
        {
            if (mes != "")
            {
                Message originMess = new Message(Message.MESSAGE, mes);
                SendMessageToAllClients(originMess);
            }
        }

        public void SendMessageToAllClients(Message tcpMes)
        {
            foreach (var user in clients)
            {
                try
                {
                        user.SendMessage(tcpMes);
                }
                catch
                {
                    MessageBox.Show($"Can't send message to user {user.login}.",
                        "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (tcpMes.code == Message.MESSAGE)
            {
                currentTime = DateTime.Now;
                string infoMessage = $"{currentTime} : IP [{chooseIP}] You : {tcpMes.data}\n";

                updateChat(infoMessage);

                infoMessage = $"{currentTime} : IP [{chooseIP}] {myOwnLogin} : {tcpMes.data}\n";
                chatHistory.Append(infoMessage);
            }

        }

        public static IPAddress GetSubnetMask(IPAddress address)  // КАК ПРАВИЛЬНО РАБОТАЕТ?
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException($"Can't find subnetmask for IP address '{address}'");
        }

        private IPAddress Logic(string operand, string ipState1, string ipState_sub)
        {
            byte buf;
            bool point;
            string sIp = "", sIp_sub = "", res="";

            do
            {
                point = true;
                if (ipState1.Contains('.'))
                {
                    sIp = ipState1.Substring(0, ipState1.IndexOf('.'));
                    sIp_sub = ipState_sub.Substring(0, ipState_sub.IndexOf('.'));
                }
                else
                {
                    sIp = ipState1;
                    sIp_sub = ipState_sub;
                    point = false;
                }

                if (operand == "&")
                    buf = (byte)(byte.Parse(sIp) & byte.Parse(sIp_sub));
                else
                    buf = (byte)(byte.Parse(sIp) | ~byte.Parse(sIp_sub));
                if (point)
                res = res + buf.ToString() + '.';
                else
                    res = res + buf.ToString();


                ipState1 = ipState1.Substring(ipState1.IndexOf('.') + 1);
                ipState_sub = ipState_sub.Substring(ipState_sub.IndexOf('.') + 1);
            }
            while (point);
            return IPAddress.Parse(res);
        }
        private IPAddress MakeBroadcastAdr(IPAddress ip)
        {
            IPAddress submask = GetSubnetMask(ip);
            IPAddress netAddres;
            netAddres = Logic("&", ip.ToString(), submask.ToString());
            
            return Logic("|", netAddres.ToString(), submask.ToString());
        }

    }
}
