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
        public int heartbeat = 20000;
        public long lastTime;
        public long nowTime;
        public int ConnectAgainCount = 0;

        //发送线程
        Thread tdSend;
        //监听线程
        Thread tdcompareTime;
        Socket clientSocket;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Connect();

            tdcompareTime = new Thread(new ThreadStart(compareTimeThread));
            tdcompareTime.Start();
            tdcompareTime.IsBackground = true;

            //接收数据
            user client = new user();
            client.workSocket = clientSocket;
            clientSocket.BeginReceive(client.buffer, 0, user.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback),
                client);

            tdSend = new Thread(new ThreadStart(SendThread));
            tdSend.Start();
            tdSend.IsBackground = true;          
        }

        //与服务器连接
        private void Connect()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            EndPoint serverEP = new IPEndPoint(ip, 5555);

            //跟服务器相连的套接字
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(serverEP);
            }
            catch
            {
                //MessageBox.Show("连接异常");
            }
        }

        //比较线程，判断服务器是否关闭
        private void compareTimeThread()
        {
            while(true)
            {
                Thread.Sleep(8000);
                nowTime = DateTime.Now.Ticks;
                if (nowTime - lastTime > 90000000)
                {
                    if(ConnectAgainCount > 3)
                    {
                        MessageBox.Show("服务器关闭");
                        break;
                    }
                    else
                    {
                        Connect();
                        tdSend = new Thread(new ThreadStart(SendThread));
                        tdSend.Start();
                        tdSend.IsBackground = true;
                        //接收数据
                        user client = new user();
                        client.workSocket = clientSocket;
                        clientSocket.BeginReceive(client.buffer, 0, user.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback),
                            client);

                        ConnectAgainCount++;
                    }
                }
            }
        }

        //发送线程，发送心跳包
        public void SendThread()
        {
            while(true)
            {
                try
                {
                    //发送信号
                    byte[] buffer = Encoding.Default.GetBytes("1111111111");
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None,
                        new AsyncCallback(SendCallback), clientSocket);
                    Thread.Sleep(heartbeat);
                }
                catch(SocketException e)
                {
                    //MessageBox.Show("服务器已关闭");
                }
            }          
        }
        public void SendCallback(IAsyncResult ar)
        {
            Socket SocketEnd = (Socket)ar.AsyncState;
            try
            {
                int SendBufferSize = SocketEnd.EndSend(ar);
            }
            catch
            {
                Connect();
            }
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
                    lastTime = DateTime.Now.Ticks;
                    client.workSocket.BeginReceive(client.buffer, 0, user.bufferSize,
                        SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
                    //MessageBox.Show("successful");
                    ConnectAgainCount = 0;
                }
            }
            catch
            {
                //MessageBox.Show("服务器关闭");
            }
        }
    }
}
