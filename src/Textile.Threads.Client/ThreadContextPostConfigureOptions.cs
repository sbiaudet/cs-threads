using System;
using Microsoft.Extensions.Options;
using Textile.Context;

namespace Textile.Threads.Client
{
    public class ThreadContextPostConfigureOptions : IPostConfigureOptions<ThreadContextOptions>
    {
        private readonly ThreadClientOptions _options;

        public ThreadContextPostConfigureOptions(IOptions<ThreadClientOptions> options)
        {
            this._options = options.Value;
        }

        public void PostConfigure(string name, ThreadContextOptions options)
        {
            options.Host = _options.Host ?? options.Host;
            options.KeyInfo = _options.KeyInfo ?? options.KeyInfo;
        }
    }
}
