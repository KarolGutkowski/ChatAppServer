using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;

namespace ChatAppServer
{
    public class DBQueryManager
    {
        private DataBaseConnection DBConnection;

        public DBQueryManager(DataBaseConnection DBConnection)
        {
            if(DBConnection == null || DBConnection._connection == null)
            {
                throw new ArgumentNullException("Null database connection argument in DBQuery Constructor");
            }
            this.DBConnection = DBConnection;
        }

        public bool Exists(string command)
        {
            DbDataReader? result = (DbDataReader?)Select(command);
            if(result == null) return false;
            return result.HasRows;
        }

        public IDataReader? Select(string command)
        {
            if(DBConnection== null || DBConnection._connection == null)
                return null;

            if(DBConnection._connection.State == ConnectionState.Open)
            {
                IDbCommand query = DBConnection._connection.CreateCommand();
                query.CommandType = CommandType.Text;
                query.CommandText = command;
                return query.ExecuteReader();
            }

            return null;
        }
    }
}
