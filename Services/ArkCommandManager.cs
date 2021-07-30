using DSharpPlus;
using EventBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBot.Services
{
    public class ArkCommandManager
    {
        private readonly DiscordClient Bot;
        private readonly Config Config;
        private readonly FileService FileManager;
        private readonly RconManager RconManager;
        public ArkCommandManager(DiscordClient _bot, RconManager _rconManager, Config _config, FileService _fileManager)
        {
            Bot = _bot;
            RconManager = _rconManager;
            Config = _config;
            FileManager = _fileManager;
            Config = _fileManager.GetConfig();
      
        }

        public async Task<string> RconSendCommand(string _command, string _serverName)
        {
            int serverId = -1;
            for (int i = 0; i < Config.Servers.Count; i++)
            {
                if (Config.Servers[i].ServerName == _serverName)
                {
                    serverId = i;
                }
            }
            var result = await RconManager.RconCommand(_command, serverId);
            return result;
        }
    }
}
