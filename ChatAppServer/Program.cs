using System;
using System.Data;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using ChatAppServer.src.MsgExchg;

namespace ChatAppServer
{
    public class Program
    {
        static TcpListener listener = new TcpListener(new IPAddress(new byte[] {127,0,0,1}), 10_000);
        static void Main(string[] args)
        {
            DataBaseConnection DB = new DataBaseConnection("Data Source=KAROLPC;Initial Catalog=ChatAppDB;Integrated Security=true");
            try
            {
                DB.Initiate();
            }catch(FailedConnectToDataBaseException ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            DBQueryManager queriesManager = new DBQueryManager(DB);
            IDataReader? reader = queriesManager.Select("SELECT user_id, login FROM Users");
            if(reader!=null)
            {
                while(reader.Read())
                {
                    Console.WriteLine($"UserID: {reader["user_id"]}, Login: {reader["login"]}");
                }
            }
            NetworkStream stream;
            try
            {
                listener.Start();
                TcpClient handler = listener.AcceptTcpClient();
                stream = handler.GetStream();
            }catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                listener.Stop();
                DB.Close();
                return;
            }

            while (true)
            {


                try
                {

                    var message = $"Server Time: {DateTime.Now}";
                    var dateTimeBytes = Encoding.UTF8.GetBytes(message);
                    stream.Write(dateTimeBytes);

                    Console.WriteLine($"Sent message: \"{message}\"");

                    var buffer = new byte[1_024];
                    stream.Read(buffer);
                    string received = Encoding.UTF8.GetString(buffer);

                    Console.WriteLine(received);
                    //Console.Read();
                }
                catch (IOException ex) //change for specific errors
                {
                    Console.WriteLine(ex.Message);
                    listener.Stop();
                    DB.Close();
                    return;
                }
                finally
                {
                    listener.Stop();
                }
            }


            DB.Close();
        }
    }
}