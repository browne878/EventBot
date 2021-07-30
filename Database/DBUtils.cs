using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace EventBot.Database
{
    class DbUtils
    {
        public static MySqlConnection GetDbConnection(string _mySqlHost, string _mySqlDatabase, string _mySqlUsername, string _mySqlPass, int _mySqlPort )
        {
            string host = _mySqlHost;
            string database = _mySqlDatabase;
            string username = _mySqlUsername;
            string password = _mySqlPass;
            int port = _mySqlPort;

            return DbMySqlUtils.GetDbConnection(host, port, database, username, password);
        }
    }
}
