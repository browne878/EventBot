namespace EventBot.Events
{
    using System;

    public class TimerUpdateEventArgs : EventArgs
    {
        public TimeSpan Duration { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishesAt { get; set; }
        public TimeSpan RemainingTime => FinishesAt - DateTime.Now;
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public bool TimerComplete { get; set; }
    }
}
