using DSharpPlus;
using EventBot.Database;
using EventBot.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace EventBot.Services
{
    public class DatabaseManager
    {
        private readonly DiscordClient Bot;
        private readonly Config Config;
        private readonly FileService FileManager;
        private readonly RconManager RconManager;

        public DatabaseManager(DiscordClient _bot, RconManager _rconManager, Config _config, FileService _fileManager)
        {
            Bot = _bot;
            RconManager = _rconManager;
            Config = _config;
            FileManager = _fileManager;
            Config = _fileManager.GetConfig();
        }

        public MySqlConnection MysqlConnection(int _dbIndex)
        {

            var conn = DbUtils.GetDbConnection(Config.MySql.MySqlHost, Config.MySql.MySqlDb, Config.MySql.MySqlUser, Config.MySql.MySqlPass, Config.MySql.MySqlPort);
            Console.WriteLine("Check Connection: " + conn);
            conn.Open();
            return conn;
            // stops here

        }

        public async Task<int> LastEntryId()
        {
            var sql = "SELECT * FROM discord_tickets ORDER BY ID DESC LIMIT 1";
            var sqlResult = 0;

            await Task.Run(() =>
            {
                //create command
                var cmd = new MySqlCommand();
                //set connection for command
                cmd.Connection = MysqlConnection(0);
                cmd.CommandText = sql;

                try
                {
                    using DbDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            sqlResult = int.Parse(reader.GetString(0));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e);
                    Console.WriteLine(e.StackTrace);
                }
                finally
                {
                    //close connection
                    MysqlConnection(0).Close();
                    //Dispose of object, freeing resources
                    MysqlConnection(0).Dispose();
                }
            });
            return sqlResult;
        }

        public async Task<int> UsersOpenTicketsNum(ulong _userId)
        {
            string sql = $"SELECT * FROM discord_tickets WHERE UserID = {_userId} AND Closed = 0";
            var sqlResult = new List<int>();

            await Task.Run(() =>
            {
                //create command
                MySqlCommand cmd = new MySqlCommand();
                //set connection for command
                cmd.Connection = MysqlConnection(0);
                cmd.CommandText = sql;

                try
                {
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                sqlResult.Add(int.Parse(reader.GetString(0)));

                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e);
                    Console.WriteLine(e.StackTrace);
                }
                finally
                {
                    //close connection
                    MysqlConnection(0).Close();
                    //dispose of object, free resources
                    MysqlConnection(0).Dispose();
                }
            });
            var result = sqlResult.Count;
            return result;
        }

        public void CreateTicketEntry(ulong _channelId, ulong _userId, DateTime _createdAt)
        {
            string sql = $"INSERT INTO discord_tickets(ChannelID,UserID,CreatedAt,Closed) VALUES ('{_channelId}','{_userId}','{_createdAt:yyyy/MM/dd HH:MM:ss}',0)";

            //create command
            var cmd = new MySqlCommand {Connection = MysqlConnection(0), CommandText = sql};
            //set connection for command

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                //close connection
                MysqlConnection(0).Close();
                //dispose object, freeing resources
                MysqlConnection(0).Dispose();
            }
        }

        public string GetSteamId(ulong _discordId)
        {
            var sqlResult = "0";
            var sql = "SELECT steamid FROM discord_vote_rewards WHERE discordid = " + _discordId;
            // Create command.
            var cmd = new MySqlCommand {Connection = MysqlConnection(0), CommandText = sql};

            // Set connection for command.


            try
            {
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {

                        while (reader.Read())
                        {
                            sqlResult = reader.GetString(0);
                        }

                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                // Close connection.
                MysqlConnection(0).Close();
                // Dispose object, Freeing Resources.
                MysqlConnection(0).Dispose();
            }

            return sqlResult;

        }
        public List<string> RetrieveLogInfo(ulong _ticketId)
        {
            List<string> sql_result = new List<string>();
            string sql = "SELECT UserID, SteamID, CreatedAt, InGameIssue, Cluster, Issue, ClosedAt, ClosedBy FROM discord_tickets WHERE ChannelID = " + _ticketId;
            // Create command.
            var cmd = new MySqlCommand {Connection = MysqlConnection(0), CommandText = sql};

            // Set connection for command.


            try
            {
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {

                        while (reader.Read())
                        {
                            sql_result.Add(reader.GetString(0));
                            sql_result.Add(reader.GetString(1));
                            sql_result.Add(reader.GetString(2));
                            sql_result.Add(reader.GetString(3));
                            sql_result.Add(reader.GetString(4));
                            sql_result.Add(reader.GetString(5));
                            sql_result.Add(reader.GetString(6));
                            sql_result.Add(reader.GetString(7));
                        }

                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                // Close connection.
                MysqlConnection(0).Close();
                // Dispose object, Freeing Resources.
                MysqlConnection(0).Dispose();
            }

            return sql_result;
        }
        public void UpdateTranscript(string _transcriptLink, int _ticketNum)
        {
            string sql = $"UPDATE discord_tickets SET Transcript = '{_transcriptLink}' WHERE ID = {_ticketNum}";

            //create command
            var cmd = new MySqlCommand {Connection = MysqlConnection(0), CommandText = sql};
            //set connection for command

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                //close connection
                MysqlConnection(0).Close();
                //dispose object, freeing resources
                MysqlConnection(0).Dispose();
            }
        }

        public void UpdateTicket(ulong _steamId, int _ingameIssue, string _cluster, string _category, int _ticketNum)
        {
            string sql = $"UPDATE discord_tickets SET SteamID = {_steamId}, InGameIssue = {_ingameIssue}, Cluster = '{_cluster}', Issue = '{_category}' WHERE ID = {_ticketNum}";

            //create command
            var cmd = new MySqlCommand {Connection = MysqlConnection(0), CommandText = sql};
            //set connection for command

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                //close connection
                MysqlConnection(0).Close();
                //dispose object, freeing resources
                MysqlConnection(0).Dispose();
            }
        }

        public void CloseTicket(int _closed, ulong _closedBy, int _ticketNum)
        {
            var sql = $"UPDATE discord_tickets SET Closed = {_closed}, ClosedBy = {_closedBy}, ClosedAt = '{DateTime.Now:yyyy/MM/dd HH:MM:ss}' WHERE ID = {_ticketNum}";

            //create command
            var cmd = new MySqlCommand {Connection = MysqlConnection(0), CommandText = sql};
            //set connection for command

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                //close connection
                MysqlConnection(0).Close();
                //dispose object, freeing resources
                MysqlConnection(0).Dispose();
            }
        }

        public void CloseTimeOut(int _closed, int _ticketNum)
        {
            string sql = $"UPDATE discord_tickets SET Closed = {_closed} WHERE ID = {_ticketNum}";

            //create command
            var cmd = new MySqlCommand {Connection = MysqlConnection(0), CommandText = sql};
            //set connection for command

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                //close connection
                MysqlConnection(0).Close();
                //dispose object, freeing resources
                MysqlConnection(0).Dispose();

            }
        }
    }
}


