using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Timers
{
    using EventBot.Events;

    public struct test
    {
        public TimeSpan Duration { get; }
        public DateTime StartedAt { get; }
        public DateTime FinishesAt { get; }

        public TimeSpan RemainingTime => FinishesAt - DateTime.Now;
        public ulong ChannelId { get; }

        public ulong MessageId { get; }
        public bool TimerComplete { get; set; }


        private readonly EventTimer EventTimer;
        public test(EventTimer _eventTimer, TimeSpan _duration, DateTime _startedAt, DateTime _finishesAt, ulong _messageId, ulong _channelId)
        {
            EventTimer = _eventTimer;
            Duration = _duration;
            StartedAt = _startedAt;
            FinishesAt = _finishesAt;
            MessageId = _messageId;
            ChannelId = _channelId;
            TimerComplete = false;
        }
    }
}
