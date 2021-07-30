using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using System.Linq;
using EventBot.Models;
using EventBot.Services;
using EventBot.Suggestion;

namespace EventBot.Commands
{
    public class SuggestionCommands : BaseCommandModule
    {
        private readonly DiscordClient Bot;
        private readonly Config Config;
        private readonly FileService FileManager;
        private readonly DatabaseManager DatabaseManager;
        private readonly Channel Channel;

        public SuggestionCommands(DiscordClient _bot, Config _config, FileService _fileManager, ArkCommandManager _acm, DatabaseManager _databaseManager, Channel _channel)
        {
            Bot = _bot;
            Config = _config;
            FileManager = _fileManager;
            DatabaseManager = _databaseManager;
            Channel = _channel;
            Config = _fileManager.GetConfig();
            FileManager.GetSuggestionConfig();
        }

        [Command("helpcommands")]
        [Description("Shows Available Commands")]
        private async Task HelpCommands(CommandContext _ctx)
        {
            var embedReply = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Title = "Community Bot Commands"
            };
            embedReply.WithAuthor("Bloody Command Helper", null, "https://cdn.discordapp.com/attachments/752642174801281214/802311513913950268/large.5897a03b41dc0_ARKHumanIconCompressed.png.323620cc3ef77936cfbdf57eb869579f.png");
            embedReply.AddField("My Stats", "/MyStats Main - Shows your stats on PVPVE\n/MyStats Smalls - Shows your stats on PVP", false);
            embedReply.AddField("Stats", "/Stats Main @user - Shows a users stats for PVPVE\n/Stats Smalls @user - Shows a users stats for PVP", false);

            await _ctx.Channel.SendMessageAsync(embed: embedReply);
        }

        [Command("SuggestionChannel")]
        [Description("Creates a Suggestion Channel")]
        private async Task SuggestionChannelCreate(CommandContext _ctx, string _channelCategory, string _season, string _time, params string[] _channelName)
        {
            double hour = 3600000;
            double day = 86400000;
            double minute = 60000;
            double calculatedTime = 0;
            DiscordChannel category = null;
            string channelName = "";

            if (!_ctx.Member.Roles.Contains(_ctx.Guild.GetRole(Config.DiscordOptions.AdminRole)) ||
                !_ctx.Member.Roles.Contains(_ctx.Guild.GetRole(Config.DiscordOptions.CommSupportRole))) return; //returns if user is not admin or Community Support

            //gets time format
            switch (_time)
            {
                case var result when _time.Contains("h"):
                    calculatedTime = double.Parse(_time.Replace("h", "").Trim());
                    calculatedTime = calculatedTime * hour;
                    break;

                case var result when _time.Contains("d"):
                    calculatedTime = double.Parse(_time.Replace("d", "").Trim());
                    calculatedTime = calculatedTime * day;
                    break;

                case var result when _time.Contains("m"):
                    calculatedTime = double.Parse(_time.Replace("m", "").Trim());
                    calculatedTime = calculatedTime * minute;
                    break;
            }

            //gets chosen catagory
            switch (_channelCategory.ToLower())
            {
                case "pve":
                    category = _ctx.Guild.GetChannel(Config.DiscordOptions.PveCategory);
                    break;

                case "pvpve":
                    category = _ctx.Guild.GetChannel(Config.DiscordOptions.PvpveCategory);
                    break;

                case "pvp":
                    category = _ctx.Guild.GetChannel(Config.DiscordOptions.PvpCategory);
                    break;
            }

            if (category == null) return;

            channelName = string.Join('-', _channelName);
            channelName = channelName.Replace(" ", "");

            await Channel.CreateSuggestionsChannel(channelName, calculatedTime, category, _season);
        }

        [Command("Suggest")]
        [Description("Converts message to a suggestion")]
        private async Task Suggest(CommandContext _ctx)
        {
            SuggestionChannelConfig suggestionChannels = FileManager.GetSuggestionConfig();

            await _ctx.Message.DeleteAsync(); //deletes command message

            if (_ctx.User.IsBot) return; //returns if user is bot

            //returns if muted or not suggestions channel
            if (_ctx.Member.Roles.Contains(_ctx.Guild.GetRole(Config.DiscordOptions.MutedRole)) || !suggestionChannels.SuggestionChannel.Exists(_x => _x.ChannelId == _ctx.Channel.Id && _x.Closed == false)) return;

            await Channel.CreateSuggestionTicket(_ctx.Member, _ctx.Channel);
        }
    }
}