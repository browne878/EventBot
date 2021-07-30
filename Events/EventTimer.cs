using DSharpPlus;
using EventBot.Models;
using EventBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EventBot.Events
{
    using System.Reflection.Metadata.Ecma335;
    using EventBot.Timers;

    public class EventTimer
    {
        private readonly DiscordClient Bot;
        private readonly Config Config;
        private readonly DatabaseManager DatabaseManager;
        private SuggestionChannelConfig SuggestionConfig;
        private Timer SuggestionChannelTimer;
        private SuggestionChannel TimedSuggestionChannel;
        private Countdown Countdown;
        private Timer CountdownTimer;

        public EventTimer(DiscordClient _bot, Config _config, FileService _fileManager, DatabaseManager _databaseManager, SuggestionChannelConfig _suggestionConfig)
        {
            Bot = _bot;
            Config = _config;
            DatabaseManager = _databaseManager;
            SuggestionConfig = _suggestionConfig;
            Config = _fileManager.GetConfig();
            SuggestionConfig = _fileManager.GetSuggestionConfig();
        }

        public void SuggestionsTimerStart(SuggestionChannel _suggestionChannel, double _timeUntilClose)
        {
            TimedSuggestionChannel = _suggestionChannel;

            SuggestionChannelTimer = new Timer(_timeUntilClose);

            SuggestionChannelTimer.Elapsed += OnSuggestionsComplete;
            SuggestionChannelTimer.AutoReset = false;
            SuggestionChannelTimer.Enabled = true;
        }

        public event EventHandler<SuggestionsCompletedEventArgs> SuggestionsComplete;

        private void OnSuggestionsComplete(object _sender, EventArgs _e)
        {
            SuggestionsComplete?.Invoke(this, new SuggestionsCompletedEventArgs()
            {
                SuggestionChannel = TimedSuggestionChannel,
                CompletedAt = DateTime.Now
            });
        }

        public void TimerStart(Countdown _countdown)
        {
            Countdown = _countdown;

            CountdownTimer = new Timer(60000);

            CountdownTimer.Elapsed += OnTimerUpdate;
            CountdownTimer.AutoReset = false;
            CountdownTimer.Enabled = true;
        }

        public event EventHandler<TimerUpdateEventArgs> TimerUpdate;

        private void OnTimerUpdate(object _sender, EventArgs _e)
        {

            if (Countdown.RemainingTime >= new TimeSpan(0))
            {
                TimerUpdate?.Invoke(this, new TimerUpdateEventArgs()
                {
                    FinishesAt = Countdown.FinishesAt,
                    Duration = Countdown.Duration,
                    ChannelId = Countdown.ChannelId,
                    MessageId = Countdown.MessageId,
                    StartedAt = DateTime.Now,
                    TimerComplete = true
                });
                return;
            }

            TimerUpdate?.Invoke(this, new TimerUpdateEventArgs()
            {
                FinishesAt = Countdown.FinishesAt,
                Duration = Countdown.Duration,
                ChannelId = Countdown.ChannelId,
                MessageId = Countdown.MessageId,
                StartedAt = DateTime.Now
            });

            TimerStart(Countdown);
        }
    }
}