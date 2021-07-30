using DSharpPlus;
using EventBot.Models;
using RconSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBot.Services
{
   public class RconManager
    {
        private readonly DiscordClient Bot;
        private readonly Config Config;
        private readonly FileService FileManager;
        private RconClient RconClient;

        public RconManager(DiscordClient _bot, Config _config, FileService _fileManager)
        {
            this.Bot = _bot;
            this.Config = _config;
            this.FileManager = _fileManager;
            this.Config = FileManager.GetConfig();
            
        }

        public async Task<bool> OpenRcon(int _serverId)
        {

            RconClient = RconClient.Create(Config.Servers[_serverId].RconIp, Config.Servers[_serverId].RconPort);
            await RconClient.ConnectAsync();
            var isAuth = await RconClient.AuthenticateAsync(Config.Servers[_serverId].RconPass);
            return isAuth;
        }


        public async Task<string> RconCommand(string _command, int _serverId)
        {
            //serverid = check which server in array for execute
            //openRcon check if connection is valid
            //command like add points etc
            if (await OpenRcon(_serverId) == true)
            {
                var response = await RconClient.ExecuteCommandAsync(_command);
                RconClient.Disconnect();

                //response is rcon respond like if you use !rcon island etc
                return response;

            }
            else
            {
                RconClient.Disconnect();
                return "Server is offline.";

            }

        }

    }
}
