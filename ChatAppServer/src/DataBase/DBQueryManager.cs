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

        public static bool GivenCredentialsCorrect(string dataBaseName, string command, List<(string, string, SqlDbType)> queryParams)
        {
            string query = "SELECT TOP 1 password FROM Users WHERE login = @login";
            List<(string, string, SqlDbType)> parameters = new List<(string, string, SqlDbType)>()
            {
                queryParams[0]
            };

            IDataReader? result = Select(dataBaseName, query, parameters);
            string? passwordStoredInDB = string.Empty;
            if (result is null) return false;
            if (result != null)
            {
                while(result.Read())
                {
                    passwordStoredInDB = result.GetValue(0).ToString();
                }
            }

            if (passwordStoredInDB is null)
                return false;

            string passwordGivenByUser = queryParams[1].Item2;
            return BCrypt.Net.BCrypt.Verify(passwordGivenByUser, passwordStoredInDB); ;
        }



        public static SqlDataReader? Select(string dataBaseName,string command, List<(string param, string value, SqlDbType type)> queryParams)
        {
            DataBaseConnection DBConn;
            try
            {
                DBConn = DataBaseConnection.CreateAndInitiateConnection(dataBaseName);
            }
            catch (Exception ex) when (
               ex is ArgumentException ||
               ex is FailedConnectToDataBaseException
            )
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

                SqlDataReader? reader = null;
                try
                {
                    reader = query.ExecuteReader();
                }catch(SqlException)
                {
                    throw;
                }

                return reader;
            }

            DBConn?.Close();

            return null;
        }


        public static void Insert(string dataBaseName, string commandString, List<(string param, string value)> insertParams)
        {
            DataBaseConnection DBConn;
            try
            {
                DBConn = DataBaseConnection.CreateAndInitiateConnection(dataBaseName);
            }catch(Exception ex) when (
                ex is ArgumentException ||
                ex is FailedConnectToDataBaseException
            )
            {
                throw;
            }


            if (DBConn?.connection?.State == ConnectionState.Open)
            {
                SqlCommand command = new SqlCommand(commandString, DBConn.connection);
                foreach (var insertParam in insertParams)
                {
                    command.Parameters.AddWithValue(insertParam.param, insertParam.value);
                }
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException)
                {
                    throw;
                }
            }

        }
    }
}
