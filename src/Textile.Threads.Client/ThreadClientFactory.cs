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
            var threadClient = serviceProvider.GetRequiredService<IThreadClient>();
            return Task.FromResult(threadClient);
        }

        public static IThreadClientFactory Create()
            => Create(options => { });

        public static IThreadClientFactory Create(Action<ThreadClientOptions> configure)
        {
            var services = new ServiceCollection();
            services.AddTextile(configure);
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IThreadClientFactory>();
        }
    }
}
