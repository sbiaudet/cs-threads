using System;
using Microsoft.Extensions.Options;

namespace Textile.Context
{
    public class DefaultThreadContextConfigureOptions : ConfigureOptions<ThreadContextOptions>
    {
        public const string DefaultHost = "http://localhost:6006";
        //public const string DefaultHost = "https://api.hub.textile.io:443";
        //public const string DefaultHost = "https://webapi.hub.textile.io";

        public DefaultThreadContextConfigureOptions() : this(DefaultHost)
        {

        }

        public DefaultThreadContextConfigureOptions(string host) : base(options => options.Host = host)
        {
        }
    }
}
