using EventBot.Models;
using System.Collections.Generic;

namespace EventBot.Models
{
    public class Config
    {
        public DiscordOptions DiscordOptions { get; set; }
        public List<Servers> Servers { get; set; }
        public MySql MySql { get; set; }
    }
}
