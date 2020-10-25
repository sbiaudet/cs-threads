using System;
using Textile.Security;

namespace Textile.Threads.Client
{
    public class ThreadClientOptions
    {
        public ThreadClientOptions()
        {
        }

        public string Host { get; set; }

        public KeyInfo KeyInfo { get; set; }
    }
}
