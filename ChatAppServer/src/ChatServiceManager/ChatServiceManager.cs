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
using System.Net.NetworkInformation;
using static ChatAppServer.src.NetworkStreamMessageProcessor.NetworkStreamMessageProcessor;
using System.Data.SqlClient;

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

                            (string? received, bool success) = StreamRead(stream);
                            if(!success)
                            {
                                connectedClients.Remove(client);
                                continue;
                            }
                            Console.WriteLine(received);
                            messagesToSend.Add(new Task(() => SendMessages(received!, client)));

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
                NetworkStream stream = tcpClient.GetStream();
                
                if(!AuthenticateUserCredentials(stream))
                {
                    StreamWrite(tcpClient.GetStream(), "FAILED");
                    tcpClient.Close();
                    continue;
                    
                }

                bool success = StreamWrite(tcpClient.GetStream(), "ACCEPTED");
                Console.WriteLine("USER Accepted");
                if(!success)
                {
                    tcpClient.Close();
                    continue;
                }

                lock (connectedClients)
                {
                    connectedClients?.Add(tcpClient);
                }
            }
        }
        public void SendMessages(string message, TcpClient sender)
        {
            lock (connectedClients)
            {
                foreach (var client in connectedClients)
                {
                    if (client == sender) continue;
                    bool success = StreamWrite(client.GetStream(), message);
                    if(!success)
                    {
                        connectedClients.Remove(client);
                    }

                }
            }
        }

        public bool AuthenticateUserCredentials(NetworkStream stream)
        {
            (string? received, bool success) = StreamRead(stream);
            if(!success)
            {
                return false;
            }
            string[] separators = new string[2] { "[login]", "[password]" };
            string[] loginData = received!.Split(separators, 
StringSplitOptions.RemoveEmptyEntries);

            string queryText = "SELECT user_id, login FROM Users WHERE login=@login AND password=@password";
            List<(string, string, SqlDbType)> queryParamsList = new List<(string,string, SqlDbType)>()
            {
               ("@login", loginData[0], SqlDbType.VarChar),
               ("@password", loginData[1],SqlDbType.VarChar)
            };

            

            return DBQueryManager.Exists(Program.mainDB,queryText, queryParamsList);
        }
    }
}
