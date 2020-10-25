using System;
using Textile.Security;

namespace Textile.Context
{
    public class ThreadContextOptions
    {
        public ThreadContextOptions()
        {
        }

        public string Host { get; set; }

        public KeyInfo KeyInfo { get; set; }
    }
}
