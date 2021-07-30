using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Models
{
   public class DiscordOptions
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public ulong AdminRole { get; set; }
        public ulong CommSupportRole { get; set; }
        public ulong MutedRole { get; set; }
        public ulong PvpRole { get; set; }
        public ulong PvpveRole { get; set; }
        public ulong PveRole { get; set; }
        public ulong PvpCategory { get; set; }
        public ulong PvpveCategory { get; set; }
        public ulong PveCategory { get; set; }
        public ulong SuggestionTicketCategory { get; set; }
        public ulong SuggestionTicketLogCategory { get; set; }
        public ulong EmbedChannel { get; set; }
    }
}
