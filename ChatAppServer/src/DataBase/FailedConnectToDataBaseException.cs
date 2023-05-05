using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppServer
{
    public class FailedConnectToDataBaseException : Exception
    {
        public FailedConnectToDataBaseException() : base("Connection attempt to database failed") { }

        public FailedConnectToDataBaseException(string message) : base(message) { }

        public FailedConnectToDataBaseException(string message, Exception inner) : base(message, inner) { }
    }
}
