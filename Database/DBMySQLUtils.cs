using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;


namespace EventBot.Database
{
    class DbMySqlUtils
    {
        public static MySqlConnection
               GetDbConnection(string _host, int _port, string _database, string _username, string _password)
        {
            // Connection String.
            var connString = "Server=" + _host + ";Database=" + _database
                             + ";port=" + _port + ";User Id=" + _username + ";password=" + _password;

            var conn = new MySqlConnection(connString);

            return conn;
        }
    }
}
