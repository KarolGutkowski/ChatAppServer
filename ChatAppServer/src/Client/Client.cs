using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace ChatAppServer.src.Clients
{
    public class Client
    {
        private string? login;
        private TcpClient? clientConnection;

        public TcpClient? Connection
        {
            get { return clientConnection; }
        }
        public string? Login
        {
            get { return login; }
        }

        public Client(string login,ref TcpClient conn)
        {
            this.login = login;
            this.clientConnection = conn;
        }
    }
}
