using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Persistence
{
    public interface IPersistent
    {
        public int Id { get; set; }

        void Persist();

        void Dispose();
    }
}
