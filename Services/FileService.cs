using DSharpPlus.Entities;
using EventBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBot.Services
{
    using System.Net;

    public class FileService
    {
        //deserialize config.json and return config object to work with
        public Config GetConfig()
        {
            const string file = "./Config/Config.json";
            string data = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<Config>(data);
        }

        public SuggestionChannelConfig GetSuggestionConfig()
        {
            const string file = "./Config/SuggestionChannelsConfig.json";
            string data = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<SuggestionChannelConfig>(data);
        }

        public void AddSuggestionChannel(SuggestionChannelConfig _channelInfo)
        {
            File.WriteAllText("./Config/SuggestionChannelsConfig.json", JsonConvert.SerializeObject(_channelInfo));
        }

        public DiscohookFormat GetEmbed(string _url)
        {
            string file = new WebClient().DownloadString(_url);
            return JsonConvert.DeserializeObject<DiscohookFormat>(file);
        }
    }
}
