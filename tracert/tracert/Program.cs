using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace tracert
{
    class Program
    {
        static void Main()
        {
            // Ввод адреса конечного узла
            Console.WriteLine("  Введите адрес:");
            string adres;
            adres = Console.ReadLine();

            // Максимальное количество прыжков
            const int MaxHop = 30;
            bool needNames = true; 
            string strHost = "";
            byte[] buffer = new byte[1024];
            byte[] data = new byte[64];

            strHost = adres;

            var objIcmp = new ICMP(data);

            // Получение сведений об адресе конечного узла
            IPHostEntry ipData = Dns.GetHostEntry(strHost);

            // "Создание" сокета(InterNetFork - IPv4-адрес, Raw - доступ к транспортному протоколу(icmp), ICMP - использование протокола ICMP)
            Socket host = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            // Адрес + номер порта(0 - резервный порт)
            IPEndPoint iep = new IPEndPoint(ipData.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork), 0);
            EndPoint ep = (EndPoint)iep;
            // Установка опций (Socket - параметры применяются для всех сокетов, ReceiveTimeout - время ожидания)
            host.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);

            int recv, responceTimeout;
            TimeSpan timestop; DateTime timestart;

            Console.WriteLine("Трассировка маршрута к {0} [{1}]\nс максимальным числом прыжков {2}:\n", strHost, ipData.AddressList[0], MaxHop);

            for (int i = 1; i < MaxHop; i++)
            {
                // Изменение параметра SocketOptionName для установки ttl
                host.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, i);

                string ipNotPort = "";
                responceTimeout = 0;

                Console.Write("{0, 2} ", i);

                // "Сбор" 3-х пакетов для дальнейшей отправки
                for (int j = 0; j < 3; j++)
                {
                    try
                    {
                        timestart = DateTime.Now;
                        //Отправка массива байтов, размера PacketSize по адресу
                        host.SendTo(objIcmp.Message, objIcmp.PacketSize, SocketFlags.None, iep);
                        //Получение данных узла в buffer
                        recv = host.ReceiveFrom(buffer, ref ep);
                        //Время остановки 
                        timestop = DateTime.Now - timestart;
                        //Получение строки с адресом без порта
                        ipNotPort = ep.ToString().Substring(0, ep.ToString().LastIndexOf(':'));

                        Console.Write("{0, 5} ms  ", timestop.Milliseconds.ToString());
                    }
                    catch (SocketException)
                    {
                        responceTimeout++;
                        Console.Write("{0, 10}", '*');
                    }

                    // Inc sequence
                    objIcmp.UpdateSequence();
                }
                if (needNames) //вывод с доменными именами
                {
                    try
                    {
                        IPHostEntry name = Dns.GetHostEntry(ipNotPort);
                        Console.WriteLine(string.Format("{1} [{0}]", ipNotPort, name.HostName));
                    }
                    catch (SocketException)
                    {
                        if (responceTimeout == 3)
                        {
                            Console.WriteLine("Timeout");
                        }
                        else
                        {
                            Console.WriteLine(string.Format("{0} ", ipNotPort));
                        }
                    }
                }
                else
                {
                    if (responceTimeout == 3)
                    {
                        Console.WriteLine("Timeout");
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0} ", ipNotPort));
                    }
                }

                if (buffer[20] == 0)
                {
                    Console.WriteLine("\nТрассировка завершена");
                    break;
                }
            }
            Console.ReadLine();
        }
    }
}
