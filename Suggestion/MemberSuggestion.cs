using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Suggestion
{
    public class MemberSuggestion
    {
        public DiscordMessage Suggestion { get; set; }
        public int YesVotes { get; set; }
        public int NoVotes { get; set; }
    }
}
