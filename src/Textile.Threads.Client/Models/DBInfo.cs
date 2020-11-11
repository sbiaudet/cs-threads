using System.Collections.Generic;

namespace Textile.Threads.Client
{
    public class DBInfo
    {
        public string Key { get; set; }

        public List<string> Addrs { get; set; } = new List<string>();
    }
}