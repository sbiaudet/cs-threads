using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Textile.Context;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddThreadContext(this IServiceCollection services)
            => AddThreadContext(services, options => { });
        

        public static IServiceCollection AddThreadContext(this IServiceCollection services, Action<ThreadContextOptions> configure)
        {
            services.AddScoped<IThreadContext, ThreadContext>();

            services.TryAddEnumerable(
             ServiceDescriptor.Singleton<IConfigureOptions<ThreadContextOptions>>(new DefaultThreadContextConfigureOptions()));

            services.Configure(configure);

            return services;
        }
    }
}
