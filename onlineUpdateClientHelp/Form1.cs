using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace onlineUpdateClientHelp
{
    public partial class Form1 : Form
    {
        //心跳发送间隔
        public int heart = 30000;
        public DateTime lastTime;
        //用来发送
        Thread tdSend;
        Socket clientSocket;

        public class user
        {
            public static int bufferSize = 1024;
            public byte[] buffer = new byte[bufferSize];
            public Socket workSocket;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //连接服务器
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            EndPoint serverEP = new IPEndPoint(ip, 5555);
        
            //跟服务器相连的套接字
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverEP);
            //接收数据
            user client = new user();
            client.workSocket = clientSocket;
            clientSocket.BeginReceive(client.buffer, 0, user.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);

            tdSend = new Thread(new ThreadStart(SendContinue));
            tdSend.Start();
           
        }

        public void SendContinue()
        {
            while(true)
            {
                //发送信号
                byte[] buffer = Encoding.Default.GetBytes("1111111111");
                clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None,
                    new AsyncCallback(SendCallback), clientSocket);
                Thread.Sleep(heart);
            }          
        }
        public void SendCallback(IAsyncResult ar)
        {
            Socket SocketEnd = (Socket)ar.AsyncState;
            int SendBufferSize = SocketEnd.EndSend(ar);
        }
        public void ReceiveCallback(IAsyncResult ar)
        {
            user client = (user)ar.AsyncState;
            try
            {
                int bytelength = client.workSocket.EndReceive(ar);
                string msg = Encoding.Default.GetString(client.buffer, 0, bytelength);
                if (msg == "successful")
                {
                    client.workSocket.BeginReceive(client.buffer, 0, user.bufferSize,
                        SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
                    MessageBox.Show("successful");
                    TimeSpan tn = DateTime.Now - lastTime;
                }
            }
            catch
            {
                MessageBox.Show("服务器关闭");
            }
        }
    }
}
