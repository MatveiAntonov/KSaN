using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using P2PChat.Protocols;
using P2PChat.Additional;

namespace P2PChat
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<IPAddress> ipList;
        private IPAddress selectedIP;
        private Protocol Connect;

        public MainWindow()
        {
            InitializeComponent();
            tbMessage.IsEnabled = false;
            btnSend.IsEnabled = false;

            // список всех ip-адресов узла
            ipList = GetIp.GetIPList();
            foreach(IPAddress ip in ipList)
            {
                cbIP.Items.Add(ip.ToString());
            }
            cbIP.SelectedIndex = 0;
            selectedIP = ipList[0];
            Connect = new Protocol(ChatUpdation);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Ip хоста
            Connect.chooseIP = selectedIP;
            
            // подключение к чату
            Connect.JoinChat(tbLogin.Text);
            btnConnect.IsEnabled = false;
            cbIP.IsEnabled = false;
            tbLogin.IsReadOnly = true;
            tbMessage.IsEnabled = true;
            btnSend.IsEnabled = true;
        }

        private void cmboxUserIP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIP = IPAddress.Parse(cbIP.SelectedItem.ToString());
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string currMess = tbMessage.Text;
            Connect.SendOriginalMessage(currMess);
            tbMessage.Text = "";
            tbChat.ScrollToEnd();
        }

        private void ChatUpdation(string text)
        {
            tbChat.AppendText(text);  // добавление данных к существующим
        }

        private void formMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connect.SendDisconnectMessage();
            System.Environment.Exit(0);
        }
    }
}
