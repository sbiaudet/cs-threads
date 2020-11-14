using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Textile.Threads.Client
{
    public class ThreadClientFactory : IThreadClientFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ThreadClientFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task<IThreadClient> CreateClientAsync()
        {
            IThreadClient threadClient = serviceProvider.GetRequiredService<IThreadClient>();
            return Task.FromResult(threadClient);
        }

        public static IThreadClientFactory Create()
        {
            return Create(options => { });
        }

        public static IThreadClientFactory Create(Action<ThreadClientOptions> configure)
        {
            ServiceCollection services = new();
            services.AddTextile(configure);
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IThreadClientFactory>();
        }
    }
}
