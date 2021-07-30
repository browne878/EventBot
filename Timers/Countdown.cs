namespace EventBot.Timers
{
    using System;
    using System.Threading.Tasks;
    using DSharpPlus.Entities;
    using EventBot.Events;

    [Serializable]
    public class Countdown
    {
        public TimeSpan Duration { get; }
        public DateTime StartedAt { get; }
        public DateTime FinishesAt { get; }

        public TimeSpan RemainingTime => FinishesAt - DateTime.Now;
        public ulong ChannelId { get; }

        public ulong MessageId { get; }
        public bool TimerComplete { get; set; }


        private readonly EventTimer EventTimer;
        public Countdown(EventTimer _eventTimer, TimeSpan _duration, DateTime _startedAt, DateTime _finishesAt, ulong _messageId, ulong _channelId)
        {
            EventTimer = _eventTimer;
            Duration = _duration;
            StartedAt = _startedAt;
            FinishesAt = _finishesAt;
            MessageId = _messageId;
            ChannelId = _channelId;
            TimerComplete = false;
        }

        private async Task SubscibeTimer()
        {
            EventTimer.TimerUpdate += OnCountdownUpdateHandler;
        }

        private async Task UnsubscribeTimer()
        {
            EventTimer.TimerUpdate -= OnCountdownUpdateHandler;
        }

        private void OnCountdownUpdateHandler(object _source, TimerUpdateEventArgs _args)
        {
            if (_args.TimerComplete)
            {
                Task completeTask = OnCountdownComplete(_args);
                Task.WaitAll(completeTask);
                return;
            }

            Task updateTask = OnCoundownUpdate(_args);
            Task.WaitAll(updateTask);
        }

        private async Task OnCoundownUpdate(TimerUpdateEventArgs _timerUpdate)
        {

        }

        private async Task OnCountdownComplete(TimerUpdateEventArgs _timerComplete)
        {

        }

        public async Task StartCountdown()
        {
            
        }

        public async Task StopCountdown()
        {

        }


    }
}
