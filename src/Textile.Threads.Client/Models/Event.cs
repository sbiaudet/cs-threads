using System;
namespace Textile.Threads.Client
{
    public class Event
    {
        public Event()
        {
        }

        public long Number { get; set; }

        public string Id { get; set; }
        public string CollectionName { get; set; }
        public Patch Patch { get; set; }
    }
}
