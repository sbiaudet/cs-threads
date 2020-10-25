using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Utils;

namespace Textile.Threads.Client.Auth
{
    public class TextileAuthInterceptor
    {
        private const string AuthorizationHeader = "Authorization";
        private const string Schema = "Bearer";

        /// <summary>
        /// Creates an <see cref="AsyncAuthInterceptor"/> that will use given access token as authorization.
        /// </summary>
        /// <param name="accessToken">OAuth2 access token.</param>
        /// <returns>The interceptor.</returns>
        public static AsyncAuthInterceptor FromAccessToken(string accessToken)
        {
            GrpcPreconditions.CheckNotNull(accessToken);
            return new AsyncAuthInterceptor((context, metadata) =>
            {
                metadata.Add(CreateBearerTokenHeader(accessToken));
                return Task.CompletedTask;
            });
        }

        private static Metadata.Entry CreateBearerTokenHeader(string accessToken)
        {
            return new Metadata.Entry(AuthorizationHeader, Schema + " " + accessToken);
        }
    }
}
