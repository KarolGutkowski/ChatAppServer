using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;


namespace ChatAppServer.src.MsgExchg
{
    internal class MessageExchanger
    {
        private TcpListener? listener;
        private TcpClient? client;
        public MessageExchanger(TcpListener listener, TcpClient client)
        {
            this.listener = listener;
            this.client = client;
        }
    }
}
