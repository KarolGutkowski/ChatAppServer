using System;
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
        static async Task Main(string[] args)
        {

            ChatServiceManager chatServiceManager;
            listener.Start();
            Console.WriteLine("Server started!");
            chatServiceManager = new ChatServiceManager(listener);
               
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

            string? command = string.Empty;
            while(command is not null && command!="quit()")
            {
                command = Console.ReadLine();
            }

            cts.Cancel();

            await Task.WhenAll(serverDuties);
            listener.Stop();
        }
    }
}