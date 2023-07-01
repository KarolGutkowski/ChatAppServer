using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppServer.src.NetworkStreamMessageProcessor
{
    public static class NetworkStreamMessageProcessor
    {
        public static bool StreamWrite(NetworkStream stream,string message)
        {
            var bytesToSend = Encoding.UTF8.GetBytes(message);
            try
            {
                stream.Write(bytesToSend, 0, bytesToSend.Length);
            }catch(IOException)
            {
                return false;
            }
            return true;
        }

        public async static Task<bool> StreamWriteAsync(NetworkStream stream, string message)
        {
            var bytesToSend = Encoding.UTF8.GetBytes(message);
            try
            {
                await stream.WriteAsync(bytesToSend, 0, bytesToSend.Length);
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        public static (string? received, bool success) StreamRead(NetworkStream stream)
        {
            var buffer = new byte[1_024];
            int readBytesCount = 0;
            try
            {
                readBytesCount = stream.Read(buffer, 0, buffer.Length);
            }catch(IOException)
            {
                return (null, false);
            }
            string received = Encoding.UTF8.GetString(buffer,0,readBytesCount);
            return (received, true);
        }

        public async static Task<(string? received, bool success)> StreamReadAsync(NetworkStream stream)
        {
            var buffer = new byte[1_024];
            int readBytesCount = 0;
            try
            {
                readBytesCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (IOException)
            {
                return (null, false);
            }
            string received = Encoding.UTF8.GetString(buffer, 0, readBytesCount);
            return (received, true);
        }
    }
}
