using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Textile.Context;
using Textile.Threads.Client;
using Textile.Threads.Client.Grpc;
using System.Net.Http;
using Grpc.Net.Client.Web;
using Grpc.Core;
using AutoMapper;
using Textile.Threads.Client.Models;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ServiceCollection AddTextile(this ServiceCollection services, Action<ThreadClientOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.AddThreadContext();

            services.TryAddSingleton<IThreadClientFactory, ThreadClientFactory>();

            services.TryAddScoped<IThreadClient, ThreadClient>();

            services.TryAddSingleton<IPostConfigureOptions<ThreadContextOptions>, ThreadContextPostConfigureOptions>();

            services.AddAutoMapper(typeof(ThreadProfile));

            services.AddGrpcClient<API.APIClient>((serviceProvider, options) =>
            {
                var context = serviceProvider.GetRequiredService<IThreadContext>();
                options.Address = new Uri(context.Host);
            });

            services.Configure(configure);


            return services;
        }
    }
}
