using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Textile.Threads.Client
{
    /// <summary>
    /// Provides support for creating <see cref="IThreadClient"/> objects.
    /// </summary>
    public class ThreadClientFactory : IThreadClientFactory
    {
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance with the specified configuration.
        /// </summary>
        /// <remarks>
        /// This constructor is not intent with external use. Use <see cref="ThreadClientFactory.Create"/> instead.
        /// </remarks>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance.</param>
        public ThreadClientFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Create a new client instance that implement <see cref="IThreadClient"/> interface.
        /// </summary>
        /// <returns>A instance of a client with implements <see cref="IThreadClient"/></returns>
        public Task<IThreadClient> CreateClientAsync()
        {
            IThreadClient threadClient = serviceProvider.GetRequiredService<IThreadClient>();
            return Task.FromResult(threadClient);
        }

        /// <summary>
        /// Create a new <see cref="ThreadClientFactory"/> instance with default configuration
        /// </summary>
        /// <returns>The <see cref="ThreadClientFactory"/> instance</returns>
        public static IThreadClientFactory Create()
        {
            return Create(options => { });
        }

        /// <summary>
        /// Create a new <see cref="ThreadClientFactory"/> instance with specific configuration
        /// </summary>
        /// <param name="configure">A configure action</param>
        /// <returns>The <see cref="ThreadClientFactory"/> instance</returns>
        public static IThreadClientFactory Create(Action<ThreadClientOptions> configure)
        {
            ServiceCollection services = new();
            services.AddTextile(configure);
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IThreadClientFactory>();
        }
    }
}
