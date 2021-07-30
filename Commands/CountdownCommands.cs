namespace EventBot.Commands
{
    using System;
    using System.Threading.Tasks;
    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.SlashCommands;
    using DSharpPlus.SlashCommands.EventArgs;
    using EventBot.Events;
    using EventBot.Timers;

    public class CountdownCommands : SlashCommandModule
    {
        private readonly DiscordClient Bot;
        private readonly EventTimer EventTimer;
        public CountdownCommands(DiscordClient _bot, EventTimer _eventTimer)
        {
            Bot = _bot;
            EventTimer = _eventTimer;
        }

        [SlashCommand("countdown", "Creates a countdown for an event")]
        public async Task TestCommand(InteractionContext _ctx,
                                      [Option("Time", "How long the countdown is for (e.g - 30m, 3h, 3d)")] string _time,
                                      [Option("channel", "channel the timer will be posted in")] DiscordChannel _channel,
                                      [Option("Roles", "The Roles will be mentioned when the countdown is complete")] DiscordRole _roles,
                                      [Option("Users", "Who will be mentioned when the countdown is complete")] DiscordUser _users)
        {
            await _ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder { Content = "Executing...", IsEphemeral = true });

            //variables
            double time;


            switch (_time)
            {
                case var x when x.Contains("m"):

                    _time = _time.Replace("m", "");

                    if (!double.TryParse(_time, out time))
                    {
                        await _ctx.Channel.SendMessageAsync("You did not enter a valid time");
                        return;
                    }

                    time *= 60000;

                    break;

                case var x when x.Contains("h"):

                    _time = _time.Replace("h", "");

                    if (!double.TryParse(_time, out time))
                    {
                        await _ctx.Channel.SendMessageAsync("You did not enter a valid time");
                    }

                    time *= 3600000;

                    break;

                case var x when x.Contains("d"):

                    _time = _time.Replace("d", "");

                    if (!double.TryParse(_time, out time))
                    {
                        await _ctx.Channel.SendMessageAsync("You did not enter a valid time");
                    }

                    time *= 86400000;

                    break;
            }

            var countdown = new Countdown(EventTimer, new TimeSpan(1, 0, 0), DateTime.Now, DateTime.Now.Add(new TimeSpan(1, 0, 0)), 846791791516057640
                , 829097674006331403);

            var type = countdown.GetType();
            Console.WriteLine(type.IsSerializable);

            DiscordMessage x = await Bot.SendMessageAsync(_channel, "Works");
        }
    }
}
