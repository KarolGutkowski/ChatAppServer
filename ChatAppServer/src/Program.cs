﻿using System;
using System.Data;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using ChatAppServer.src.ManageChat;
using ChatAppServer.src.Clients;

namespace ChatAppServer
{
   
    public class Program
    {
        public static string mainDB = "ChatAppDB";
        static TcpListener listener = new TcpListener(new IPAddress(new byte[] {127,0,0,1}), 10_000);
        public static DataBaseConnection DB;
        static async Task Main(string[] args)
        {
            /*
            DataBaseConnection DB = new DataBaseConnection("ChatAppDB");
            try
            {
                DB.Initiate();
            }
            catch (FailedConnectToDataBaseException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }*/
            IDataReader? reader = DBQueryManager.Select(mainDB, "SELECT user_id, login FROM Users WHERE login='user' AND password='test_password'");
            if(reader!=null)
            {
                while(reader.Read())
                {
                    Console.WriteLine($"UserID: {reader["user_id"]}, Login: {reader["login"]}");
                }
            }


            NetworkStream stream;
            ChatServiceManager chatServiceManager;
            List<TcpClient> clients = new();
            try
            {
                listener.Start();
                TcpClient handler = listener.AcceptTcpClient();
               // Client client = new(String.Empty,ref handler);
                clients.Add(handler);
                chatServiceManager = new(listener, clients);
                //TODO: check if clients lists updates uppon accepting new clients.

                stream = handler.GetStream();
            }catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                listener.Stop();
                DB.Close();
                return;
            }




            var cts = new CancellationTokenSource();
            var serverDuties = new List<Task>()
            {
                new Task(() => chatServiceManager.ReadMessages(cts.Token)),
                new Task(() => chatServiceManager.AcceptClients(cts.Token))
            };

        
            foreach(var task in serverDuties)
            {
                task.Start();
            }



            await Task.WhenAll(serverDuties);
            listener.Stop();
            DB.Close();
        }
    }
}