using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Models
{
    public class SuggestionChannel
    {
        public string ChannelName { get; set; }
        public ulong ChannelId { get; set; }
        public DateTime CreatedAt { get; set; }
        public double ChannelDuration { get; set; }
        public bool Closed { get; set; }
    }
}