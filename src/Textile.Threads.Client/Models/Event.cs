using System;

namespace Textile.Threads.Client.Models
{
    public class CollectionEvent
    {
        public CollectionEvent()
        {
        }

        public long Number { get; set; }

        public string Id { get; set; }
        public string CollectionName { get; set; }
        public Patch Patch { get; set; }
    }
}
