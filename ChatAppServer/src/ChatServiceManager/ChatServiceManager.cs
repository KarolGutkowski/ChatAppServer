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

namespace ChatAppServer.src.ManageChat
{
    public class ChatServiceManager
    {
        private TcpListener? listener;
        private List<Client>? connectedClients;
        public ChatServiceManager()
        {
            this.listener = null;
            this.connectedClients = null;
        }
        public ChatServiceManager(TcpListener listener, List<Client> clients)
        {
            this.listener = listener;
            this.connectedClients = clients;
        }

        public void ReadMessages(CancellationToken cts)
        {
            if (connectedClients == null)
                return;
            while (!cts.IsCancellationRequested)
            {
                var messagesToSend = new List<Task>();
                var messagesToStore = new List<Task>();
                lock (connectedClients)
                {
                    foreach (var client in connectedClients)
                    {
                        if(client.Connection is null || !client.Connection.Connected) 
                            connectedClients.Remove(client);

                        NetworkStream stream = client.Connection.GetStream();
                        if (stream.DataAvailable)
                        {

                            (string? received, bool success) = StreamRead(stream);
                            if(!success)
                            {
                                connectedClients.Remove(client);
                                continue;
                            }

                            StringBuilder messageToSendBuilder = new StringBuilder($"[{client.Login}]");
                            messageToSendBuilder.Append(received);
                            string messageToSend = messageToSendBuilder.ToString();

                            Console.WriteLine(received);
                            messagesToSend.Add(new Task(() => SendMessages(messageToSend, client)));  
                            messagesToStore.Add(new Task(() => StoreNewMessageIntoDataBase(client.Login! , received!)));

                        }
                    }
                }

                foreach(var task in messagesToSend)
                {
                    task.Start();
                }

                foreach(var task in messagesToStore)
                {
                    task.Start();
                }
            }
        }

        public async void AcceptClients(CancellationToken cts)
        {
            while(!cts.IsCancellationRequested)
            {   
                
                if (listener == null) return;
               // TcpClient tcpClient = listener.AcceptTcpClient();
                var acceptTask = listener.AcceptTcpClientAsync();

                var completedTask = await Task.WhenAny(acceptTask, Task.Delay(-1, cts));

                if(completedTask != acceptTask)
                {
                    return;
                }

                TcpClient tcpClient = acceptTask.Result;
                NetworkStream stream = tcpClient.GetStream();

                (bool authorizedToJoin, string login) = AuthenticateUserCredentials(stream);

                if (!authorizedToJoin)
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

                Client newlyAcceptedClient = new Client(login, ref tcpClient);
                lock (connectedClients)
                {
                    connectedClients?.Add(newlyAcceptedClient);
                }
            }
        }
        public void SendMessages(string message, Client sender)
        {
            lock (connectedClients)
            {
                foreach (var client in connectedClients)
                {
                    if (client.Login == sender.Login) continue;
                    bool success = StreamWrite(client.Connection.GetStream(), message);
                    if(!success)
                    {
                        connectedClients.Remove(client);
                    }

                }
            }
        }

        public (bool, string) AuthenticateUserCredentials(NetworkStream stream)
        {
            (string? received, bool success) = StreamRead(stream);
            if(!success)
            {
                return (false, null);
            }
            (string login, string password) = GetLoginDataFromString(received!);

            string queryText = "SELECT user_id, login FROM Users WHERE login=@login AND password=@password";
            
            List<(string, string, SqlDbType)> queryParamsList = new List<(string,string, SqlDbType)>()
            {
               ("@login", login, SqlDbType.VarChar),
               ("@password", password,SqlDbType.VarChar)
            };

            

            return (DBQueryManager.GivenCredentialsCorrect(Program.mainDB,queryText, queryParamsList), login);
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

        public static void StoreNewMessageIntoDataBase(string sender_name, string message)
        {
            // prepare query to send to database
            // insert data into database

            string insertText = "INSERT INTO ChatHistory" +
                "(sender_name, message)" +
                "VALUES (@sender_name, @message)";

            List<(string param, string value)> insertParams =
                new List<(string param, string value)>()
                {
                    ("@sender_name", sender_name),
                    ("@message", message)
                };

            DBQueryManager.Insert(Program.mainDB, insertText, insertParams);
        }
    }
}
