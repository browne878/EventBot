using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Persistence
{
    public class PersistenceContainer
    {
        public Dictionary<int, object> PersistentObjects { get; private set; }

        public PersistenceContainer()
        {
            PersistentObjects = new Dictionary<int, object>();
        }


    }
}
