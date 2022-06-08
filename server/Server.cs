using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;


namespace server1
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
        }
        Listener listener = new Listener();
        private void btnListen_Click(object sender, EventArgs e)
        {
            
            if (btnListen.Text == "Listen")
            {
                btnListen.Text = "Disconnect";

                listener = new Listener();
                //Listener mission: Find other socket 
                listener.listen();
            }
            else
            {
                btnListen.Text = "Listen";
                listener.listenSock.Close();
            }
        }
    }

    public class Listener
    {
        public Socket listenSock;
        public int port = 1234;

        public Listener()
        {
            listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        
        public void listen()
        {
            listenSock.Bind(new IPEndPoint(IPAddress.Any, port));
            listenSock.Listen(10);
            listenSock.BeginAccept(callback, listenSock);
            
        }
        private void callback(IAsyncResult ar)
        {
            try
            {
                Socket acceptedSocket = listenSock.EndAccept(ar);
                ManageClient.AddClient(acceptedSocket);
                listenSock.BeginAccept(callback, listenSock);
            }
            catch(ObjectDisposedException ex)
            {
                MessageBox.Show("Server has closed");
            }
        }
    }
    public class Client
    {
        public Socket clientSock { get; set; }
        public int id { get; set; }
        public byte[] data;
        public bool isWaiting = false;
        
        public Client(Socket _socket,int _id)
        {
            clientSock = _socket;
            id = _id;
        }
        public void send(byte[] data)
        {
            clientSock.Send(data,0,data.Length,SocketFlags.None);
        }
        public void StartReceiving()
        {
            try
            {
                data = new byte[8];
                clientSock.BeginReceive(data, 0, data.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch { }
        }
        private void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                if (clientSock.EndReceive(AR) > 1)
                {
                    clientSock.Receive(data, data.Length, SocketFlags.None);
                    MessageBox.Show($"Server receive data from id {id}");
                }
                else
                {
                    Disconnect();
                }
            }
            catch
            {
                // if exeption is throw check if socket is connected because than you can startreive again else Dissconect
                if (!clientSock.Connected)
                {
                    Disconnect();
                }
                else
                {
                    StartReceiving();
                }
            }
        }

        private void Disconnect()
        {
            // Close connection
            clientSock.Disconnect(true);
            
        }
    }
    public static class ManageClient
    {
        static List<Client> Clients = new List<Client>();

        public static void AddClient(Socket _socket)
        {
            Clients.Add(new Client(_socket, Clients.Count));
            MessageBox.Show($"adding new socket success! id:{Clients[Clients.Count -1].id}");
            
            //Có người đang đợi
            if (Clients.Count % 2 == 0)
            {
                foreach(Client client in Clients)
                {
                    if (client.isWaiting)
                    {
                        client.isWaiting = false;
                        MatchBetween(client,Clients[Clients.Count-1]);
                        break;
                    }
                }
            }
            else
            {
                Clients[Clients.Count - 1].isWaiting = true;
            }
        }

        public static void RemoveClient(int _id)
        {
            int findIndex = Clients.FindIndex(x => x.id == _id);
            Clients.RemoveAt(findIndex);
            MessageBox.Show($"Remove id {_id} which is Clients[{findIndex}] ");
        }

        public static void MatchBetween(Client firstPlayer, Client secondPlayer)
        {
            //buffer để gửi
            byte[] buffer = new byte[8];

            #region TRƯỚC VÁN ĐẤU

            while (true)
            {
                DialogResult result = MessageBox.Show($"2. Ấn Yes để bắt đầu kết nối 2 client id:{firstPlayer.id} và id:{secondPlayer.id}",
                    "QuestionDialog", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                    break;
            }
            //Gửi lần đầu để gọi hàm CallBack trong client
            //Gửi thông tin vào lần 2

            //Quân trắng
            firstPlayer.clientSock.Send(buffer);
            buffer[0] = 1;
            firstPlayer.clientSock.Send(buffer);

            //Quân đen
            secondPlayer.clientSock.Send(buffer);
            buffer[0] = 0;
            secondPlayer.clientSock.Send(buffer);

            #endregion

            #region VÁN ĐẤU

            bool WhiteToPlay = true;
            while (true)
            {
                buffer = new byte[8];

                //Cần phải gửi gói tin mẫu trước để gọi hàm CallBack của client
                if (WhiteToPlay)
                {
                    firstPlayer.StartReceiving();
                    
                    while (firstPlayer.data[3] == firstPlayer.data[5]
                        && firstPlayer.data[4] == firstPlayer.data[6])//Chưa thực hiện nước đi
                    {
                        Thread.Sleep(3000);
                    }
                    secondPlayer.send(buffer);
                    buffer = firstPlayer.data;
                    secondPlayer.send(buffer);

                    MessageBox.Show($"Server receive buffer byte[0]:{buffer[0]} \r\n" +
                        $"byte[3]:{buffer[3]}\r\n" +
                        $"byte[4]:{buffer[4]}\r\n" +
                        $"Send buffer to second player!");
                    firstPlayer.data = new byte[8];
                }
                else
                {
                    secondPlayer.StartReceiving();
                    while (secondPlayer.data[3] == secondPlayer.data[5]
                        && secondPlayer.data[4] == secondPlayer.data[6])//Chưa thực hiện nước đi
                    {
                        Thread.Sleep(3000);
                    }
                    firstPlayer.send(buffer);
                    buffer = secondPlayer.data;
                    firstPlayer.send(buffer);

                    MessageBox.Show($"Server Receive buffer byte[0]:{buffer[0]} \r\n" +
                        $"byte[3]:{buffer[3]}\r\n" +
                        $"byte[4]:{buffer[4]}\r\n" +
                        $"Send buffer to first player!");
                    secondPlayer.data = new byte[8];
                }
                WhiteToPlay = !WhiteToPlay;
                //Ván kết thúc
                if (buffer[1]==1)
                {
                    break;
                }
            }

            #endregion
        }
    }
}
