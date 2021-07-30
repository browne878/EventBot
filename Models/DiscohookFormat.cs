using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Models
{
    using DSharpPlus.CommandsNext.Converters;
    using DSharpPlus.Entities;

    public class DiscohookFormat
    {
        public string content { get; set; }
        public DiscordEmbed[] embeds { get; set; }
        public string username { get; set; }
        public string avatar_url { get; set; }
    }
}
