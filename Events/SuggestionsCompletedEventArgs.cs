using EventBot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Events
{
    public class SuggestionsCompletedEventArgs : EventArgs
    {
        public SuggestionChannel SuggestionChannel { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
