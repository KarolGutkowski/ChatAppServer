using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace ChatAppServer
{
    public class DataBaseConnection
    {
        private SqlConnection? Connection = null;
        private string connectionString;
        public SqlConnection? connection
        {
            get => Connection;
            set => Connection = value;
        }

        public DataBaseConnection(string dataBaseName) 
        {

            string? sAttr = ConfigurationManager.AppSettings.Get($"{dataBaseName}");
            if(sAttr is null)
            {
                throw new ArgumentException("Data Base of provide name not found in configuration file.");
            }
            this.connectionString = sAttr;
        }

        public void Initiate()
        {
            Connection = new SqlConnection();
            Connection.ConnectionString = this.connectionString;
            try
            {
                Connection.Open();
            }catch (SqlException ex)
            {
                throw new FailedConnectToDataBaseException(string.Empty, ex);
            }
            Console.WriteLine("Data Base Connection initiated!");
        }

        public void Close()
        {
            if (Connection != null)
            {
                Connection.Close();
            }
            Console.WriteLine("Data Base Conenction closed.");
        }
    }
}
