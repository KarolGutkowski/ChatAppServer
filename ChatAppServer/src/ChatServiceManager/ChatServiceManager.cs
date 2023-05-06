using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Net.Sockets;
using System.Net;
using System.IO;
using ChatAppServer.src.Clients;


namespace ChatAppServer.src.ManageChat
{
    internal class ChatServiceManager
    {
        private TcpListener? listener;
        private List<TcpClient>? connectedClients;
        public ChatServiceManager()
        {
            this.listener = null;
            this.connectedClients = null;
        }
        public ChatServiceManager(TcpListener listener, List<TcpClient> clients)
        {
            this.listener = listener;
            this.connectedClients = clients;
        }

        public void ReadMessages(CancellationToken? cts=null)
        {
            if (connectedClients == null)
                return;
            while (true)
            {
                var messagesToSend = new List<Task>();
                lock (connectedClients)
                {
                    foreach (TcpClient client in connectedClients)
                    {
                        if(!client.Connected) connectedClients.Remove(client);
                        NetworkStream stream = client.GetStream();
                        if (stream.DataAvailable)
                        {
                            var buffer = new byte[1_024];
                            stream.ReadAsync(buffer);
                            string received = Encoding.UTF8.GetString(buffer);
                            Console.WriteLine(received);
                            messagesToSend.Add(new Task(() => SendMessages(received, client)));

                        }
                    }
                }

                foreach(var task in messagesToSend)
                {
                    task.Start();
                }
            }
        }

        public void AcceptClients(CancellationToken? cts= null)
        {
            while(true)
            {   
                
                    if (listener == null) return;
                    TcpClient tcpClient = listener.AcceptTcpClient();
                lock (connectedClients)
                {
                    connectedClients?.Add(tcpClient);
                }
            }
        }


        public void SendMessages(string message, TcpClient sender)
        {
            lock(connectedClients)
            {
                foreach(var client in connectedClients)
                {
                    if (client == sender) continue;
                    var bytesToSend = Encoding.UTF8.GetBytes(message);
                    client.GetStream().Write(bytesToSend, 0, bytesToSend.Length);
                }
            }
        }
    }
}
