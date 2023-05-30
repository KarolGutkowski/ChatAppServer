using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Configuration;
using System.Collections.Specialized;
using System.Data.SqlClient;

namespace ChatAppServer
{
    public static class DBQueryManager
    {
        public static bool Exists(string dataBaseName, string command)
        {
            IDataReader? result = Select(dataBaseName, command);
            if(result is null) return false;
            bool exists = false;
            if (result != null)
            {
                while (result.Read())
                {
                    exists = true;
                }
            }
            return exists;
        }

        public static bool Exists(string dataBaseName, string command, List<(string, string, SqlDbType)> queryParams)
        {
            IDataReader? result = Select(dataBaseName, command, queryParams);
            if (result is null) return false;
            bool exists = false;
            if (result != null)
            {
                while (result.Read())
                {
                    exists = true;
                }
            }
            return exists;
        }


        public static IDataReader? Select(string dataBaseName,string command, List<(string param, string value, SqlDbType type)> queryParams)
        {
            DataBaseConnection DBConn;
            try
            {
                DBConn = new DataBaseConnection(dataBaseName);
            }
            catch (ArgumentException)
            {
                throw;
            }

            try
            {
                DBConn.Initiate();
            }
            catch (FailedConnectToDataBaseException)
            {
                throw;
            }

            if (DBConn?.connection?.State == ConnectionState.Open)
            {   
                SqlCommand query = new SqlCommand(command, DBConn.connection);
                foreach(var queryParam in queryParams)
                {
                    query.Parameters.AddWithValue(queryParam.param, queryParam.value);
                }
                return query.ExecuteReader();
            }

            DBConn?.Close();

            return null;
        }


        public static IDataReader? Select(string dataBaseName, string command)
        {
            DataBaseConnection DBConn;
            try
            {
                DBConn = new DataBaseConnection(dataBaseName);
            }catch (ArgumentException)
            {
                throw;
            }

            try
            {
                DBConn.Initiate();
            }catch(FailedConnectToDataBaseException)
            {
                throw;
            }

            if(DBConn?.connection?.State == ConnectionState.Open)
            {
                IDbCommand query = DBConn.connection.CreateCommand();
                query.CommandType = CommandType.Text;
                query.CommandText = command;
                return query.ExecuteReader();
            }

            DBConn?.Close();

            return null;
        }
    }
}
