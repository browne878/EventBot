namespace EventBot.EmbedBuilder
{
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using EventBot.Models;
    using EventBot.Services;

    public class EmbedCreate
    {
        private readonly DiscordClient Bot;
        private readonly FileService FileManager;

        public EmbedCreate(DiscordClient _bot, FileService _fileManager)
        {
            Bot = _bot;
            FileManager = _fileManager;
            _fileManager.GetConfig();
        }

        public async Task EmbedGetter(DiscordChannel _channel, DiscordAttachment _embedFile)
        {
            DiscohookFormat embed = FileManager.GetEmbed(_embedFile.Url);

            DiscordMessageBuilder builder = new DiscordMessageBuilder { Content = embed.content, Embed = embed.embeds[0] };

            await _channel.SendMessageAsync(builder);


        }
    }
}