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
using BCrypt.Net;
using BCrypt.Net;

namespace ChatAppServer.src.ManageChat
{
    public class ChatServiceManager
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

        public void ReadMessages(CancellationToken cts)
        {
            if (connectedClients == null)
                return;
            while (cts.IsCancellationRequested)
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

        public async void AcceptClients(CancellationToken cts)
        {
            while(cts.IsCancellationRequested)
            {   
                
                if (listener == null) return;
                TcpClient tcpClient = listener.AcceptTcpClient();
                var acceptTask = listener.AcceptTcpClientAsync();

                var completedTask = await Task.WhenAny(acceptTask, Task.Delay(-1, cts));

                if(completedTask != acceptTask)
                {
                    return;
                }

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
            (string login, string password) = GetLoginDataFromString(received!);

            string queryText = "SELECT user_id, login FROM Users WHERE login=@login AND password=@password";
            
            List<(string, string, SqlDbType)> queryParamsList = new List<(string,string, SqlDbType)>()
            {
               ("@login", login, SqlDbType.VarChar),
               ("@password", password,SqlDbType.VarChar)
            };

            

            return DBQueryManager.GivenCredentialsCorrect(Program.mainDB,queryText, queryParamsList);
        }

        public static (string username, string password) GetLoginDataFromString(string loginDataMessage)
        {
            string[] separators = new string[2] { "[login]", "[password]" };
            string[] loginData = loginDataMessage!.Split(separators,StringSplitOptions.RemoveEmptyEntries);

            if(loginData.Length < 2)
            {
                throw new ArgumentException();
            }

            string login = loginData[0];
            string password = loginData[1];

            if(String.IsNullOrWhiteSpace(login))
            {
                throw new ArgumentException("Invalid argument provided!", "login");
            }
            if(String.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Invalid argument provided!", "password");
            }

            return (login, password);
        }
    }
}
