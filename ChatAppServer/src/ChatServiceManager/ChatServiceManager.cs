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
using System.Collections.Concurrent;

namespace ChatAppServer.src.ManageChat
{
    public class ChatServiceManager
    {
        private TcpListener? listener;
        private ConcurrentDictionary<ulong, Client> connectedClients;
        public static ulong nextUserId = 0;
        public ChatServiceManager()
        {
            this.listener = null;
            connectedClients = new ConcurrentDictionary<ulong, Client>();
        }
        public ChatServiceManager(TcpListener listener)
        {
            this.listener = listener;
            connectedClients = new ConcurrentDictionary<ulong, Client>();
        }

        public void ReadMessages(CancellationToken cts)
        {
            if (connectedClients == null)
                return;
            while (!cts.IsCancellationRequested)
            {
                var messagesToSend = new List<Task>();
                var messagesToStore = new List<Task>();
                
                foreach (var client in connectedClients)
                {
                    if (client.Value.Connection is null || !client.Value.Connection.Connected)
                    {
                        connectedClients.TryRemove(client);
                        continue;
                    }
                        
                    NetworkStream stream = client.Value.Connection.GetStream();
                    if (stream.DataAvailable)
                    {

                        (string? received, bool success) = StreamRead(stream);
                        if(!success)
                        {
                            connectedClients.TryRemove(client);
                            continue;
                        }

                        StringBuilder messageToSendBuilder = new StringBuilder($"[{client.Value.Login}]");
                        messageToSendBuilder.Append(received);
                        messageToSendBuilder.Append("\n");
                        string messageToSend = messageToSendBuilder.ToString();

                        Console.WriteLine(received);
                        messagesToSend.Add(new Task(() => SendMessages(messageToSend, client.Value)));  
                        messagesToStore.Add(new Task(() => StoreNewMessageIntoDataBase(client.Value.Login! , received!)));

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

                (bool authorizedToJoin, string? login) = AuthenticateUserCredentials(stream);

                if (!authorizedToJoin)
                {
                    NetworkStream clientStream;
                    try
                    {
                        clientStream = tcpClient.GetStream();
                    }catch (InvalidOperationException)
                    {
                        continue;
                    }

                    StreamWrite(clientStream, "FAILED");
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

                if (login is null) login = "";

                Client newlyAcceptedClient = new Client(login, ref tcpClient);

                string storedMessages = GetAllMessages();

                if (tcpClient.GetStream() is not null)
                    await StreamWriteAsync(tcpClient.GetStream(), storedMessages);
                
                connectedClients.TryAdd(nextUserId++, newlyAcceptedClient);
            }
        }
        public void SendMessages(string message, Client sender)
        {
            foreach (var client in connectedClients)
            {
                if (client.Value.Login == sender.Login) continue;
                if (client.Value.Connection is null) continue;

                bool success = StreamWrite(client.Value.Connection.GetStream(), message);
                if(!success)
                {
                    connectedClients.TryRemove(client);
                }

            }
        }

        public (bool, string?) AuthenticateUserCredentials(NetworkStream stream)
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

            string insertText = "INSERT INTO ChatHistory VALUES (@sender_name, @message, @date, @time)";

            List<(string param, string value)> insertParams =
                new List<(string param, string value)>()
                {
                    ("@sender_name", sender_name),
                    ("@message", message),
                    ("@date", DateTime.Now.Date.ToString()),
                    ("@time", DateTime.Now.TimeOfDay.ToString())
                };

            try
            {
                DBQueryManager.Insert(Program.mainDB, insertText, insertParams);
            } catch (SqlException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

        }

        public static string GetAllMessages()
        {
            string command = $"SELECT * FROM ChatHistory ORDER BY [date], [time];";
            SqlDataReader? reader;
            try
            {
                reader = DBQueryManager.Select(Program.mainDB, command, new());
            }
            catch (Exception ex) when (
               ex is ArgumentException ||
               ex is FailedConnectToDataBaseException||
               ex is SqlException
            )
            {
                throw;
            }

            if (reader is null)
                return String.Empty;

            StringBuilder messageBuilder = new StringBuilder();

            

            if(reader.HasRows)
            {
                while(reader.Read())
                {
                    DateTime date = (DateTime)reader["date"];
                    TimeSpan timeOfDay = (TimeSpan)reader["time"];
                    string sender = (string)reader["sender_name"];
                    string message = (string)reader["message"];

                    messageBuilder.Append($"[{date.ToShortDateString()} at {timeOfDay.ToString("hh':'mm")}]\n[{sender}]{message}\n");
                }
            }

            return messageBuilder.ToString();
        }
    }
}
