using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Commands
{
    using System.Linq;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.CommandsNext;
    using DSharpPlus.CommandsNext.Attributes;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;
    using DSharpPlus.Interactivity;
    using DSharpPlus.Interactivity.Extensions;
    using EventBot.EmbedBuilder;
    using EventBot.Models;
    using EventBot.Services;
    using EventBot.Suggestion;

    public class EmbedBuilderCommands : BaseCommandModule
    {
        private readonly DiscordClient Bot;
        private readonly Config Config;
        private readonly FileService FileManager;
        private readonly DatabaseManager DatabaseManager;
        private readonly Channel Channel;
        private readonly EmbedCreate EmbedCreate;

        public EmbedBuilderCommands(DiscordClient _bot, Config _config, FileService _fileManager, DatabaseManager _databaseManager, Channel _channel, EmbedCreate _embedCreate)
        {
            Bot = _bot;
            Config = _config;
            FileManager = _fileManager;
            DatabaseManager = _databaseManager;
            Config = _fileManager.GetConfig();
            Channel = _channel;
            EmbedCreate = _embedCreate;
        }

        [Command("embed")]
        [Description("Creates an Embed")]
        private async Task CreateEmbed(CommandContext _ctx)
        {
            if (_ctx.Channel.Id != Config.DiscordOptions.EmbedChannel) return;
            if (_ctx.User.IsBot) return;

            List<DiscordEmbedBuilder> sourceEmbeds = new List<DiscordEmbedBuilder>();

            if (_ctx.Message.Attachments.Count != 0)
            {
                await EmbedCreate.EmbedGetter(_ctx.Channel, _ctx.Message.Attachments[0]);
            }
            else
            {
                await _ctx.RespondAsync("You have not attached a file!\n\nPlease use DiscoHook to create and embed.\nSave it to a text document\nSend it here with /embed");
            }

        }
    }
}
