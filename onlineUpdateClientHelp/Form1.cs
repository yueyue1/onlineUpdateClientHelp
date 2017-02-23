using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
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
        class FileInformation
        {
            public byte[] fileName;
            public byte[] fileData;
        }
        List<byte> msg_recebyte = new List<byte>();
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //连接服务器
            Connect();

            //开启比较线程，判断服务器是否关闭
            tdcompareTime = new Thread(new ThreadStart(compareTimeThread));
            tdcompareTime.Start();
            tdcompareTime.IsBackground = true;
            
            //发送心跳包
            tdSend = new Thread(new ThreadStart(SendThread));
            tdSend.Start();
            tdSend.IsBackground = true;

            //接收文件信息
            Thread tdFile = new Thread(new ThreadStart(td_message_handle));
            tdFile.Start();
            tdFile.IsBackground = true;
        }

        /*与服务器连接*/
        private void Connect()
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            EndPoint serverEP = new IPEndPoint(ip, 5555);

            //跟服务器相连的套接字
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(serverEP);
                //接收数据
                user client = new user();
                client.workSocket = clientSocket;
                clientSocket.BeginReceive(client.buffer, 0, user.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallback),
                    client);
            }
            catch
            {
                //MessageBox.Show("连接异常");
            }
        }

        //判断服务器是否关闭
        private void compareTimeThread()
        {
            while(true)
            {
                Thread.Sleep(heartbeat);
                nowTime = DateTime.Now.Ticks;
                if (nowTime - lastTime > 10000*(heartbeat+1000))
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
                        ConnectAgainCount++;
                    }
                }
            }
        }

        //发送心跳包
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
            }
        }
        //FileStream filesream = new FileStream(Application.ExecutablePath + @"\..\UpdateFile\2.rar",
        //    FileMode.Create, FileAccess.Write);

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
                    ConnectAgainCount = 0;
                }
                else
                {
                    lock(msg_recebyte)
                    {
                        for(int i=0;i<bytelength;i++)
                        {
                            msg_recebyte.Add(client.buffer[i]);
                        }
                    }
                    client.workSocket.BeginReceive(client.buffer, 0, user.bufferSize,
                        SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
                }
            }
            catch
            {
                //MessageBox.Show("服务器关闭");
            }
        }
        public static int IndexOf(byte[] s, byte[] pattern)
        {
            int slen = s.Length;
            int plen = pattern.Length;
            for (int i = 0; i <= slen - plen; i++)
            {
                for (int j = 0; j < plen; j++)
                {
                    if (s[i + j] != pattern[j]) goto next;
                }
                return i;
            next: ;
            }
            return -1;
        }
        private void td_message_handle()
        {
            byte[] first = Encoding.Default.GetBytes("1234567");
            byte[] last = Encoding.Default.GetBytes("7654321");
            while (true)
            {
                if (msg_recebyte.Count > 0)
                {
                    lock (msg_recebyte)
                    {
                        byte[] recebyte = msg_recebyte.ToArray();

                        if (IndexOf(recebyte, first) == 0 && IndexOf(recebyte, last) != -1)
                        {
                            string msg = Encoding.Default.GetString(recebyte);
                            msg = msg.Remove(0, 7);
                            msg = msg.Remove(msg.Length - 7);
                          
                            //反序列化
                            JavaScriptSerializer jsser = new JavaScriptSerializer();
                            jsser.MaxJsonLength = Int32.MaxValue;
                            FileInformation fileReceive = jsser.Deserialize<FileInformation>(msg);

                            using (FileStream filesream = new FileStream(Application.ExecutablePath + @"\..\UpdateFile\" +
                                Encoding.Default.GetString(fileReceive.fileName), FileMode.Create, FileAccess.Write))
                            {
                                filesream.Write(fileReceive.fileData, 0, fileReceive.fileData.Length);
                            }                            
                        }
                        else
                        {
                            int index = IndexOf(recebyte, first);
                            if (index != -1)
                            {
                                msg_recebyte.RemoveRange(0, index);
                            }
                            else
                            {
                                index = IndexOf(recebyte, last);
                                if (index != -1)
                                {
                                    msg_recebyte.RemoveRange(0, index + 8);
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
