using System;
using System.Linq;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Textile.Security;
using static Grpc.Core.Metadata;

namespace Textile.Context
{
    public class ThreadContext : IThreadContext
    {
        private readonly object syncObj = new object();

        public ThreadContext(IOptions<ThreadContextOptions> options)
        {
            this.Host = options.Value.Host;

            if (options.Value.KeyInfo != null)
            {
                WithKeyInfo(options.Value.KeyInfo);
            }
        }

        public Metadata Metadata { get; internal set; } = new Metadata();

        public string Host { get; set; }

        public ThreadContext WithSession(string session)
            => this.WithEntry(ContextKeys.SessionKey, session);


        public ThreadContext WithThread(string threadId)
            => this.WithEntry(ContextKeys.ThreadIdKey, threadId);


        public ThreadContext WithThreadName(string threadName)
            => this.WithEntry(ContextKeys.ThreadNameKey, threadName);

        public ThreadContext WithOrganization(string organization)
           => this.WithEntry(ContextKeys.OrganizationKey, organization);

        public ThreadContext WithApiKey(string apiKey)
           => this.WithEntry(ContextKeys.ApiKey, apiKey);

        public ThreadContext WithApiSignature(ApiSig apiSignature)
           => this.WithEntry(ContextKeys.ApiSignatureKey, apiSignature.Sig)
                  .WithEntry(ContextKeys.ApiSignatureRawMessageKey, apiSignature.Msg);

        public ThreadContext WithToken(string token)
            => this.WithEntry(ContextKeys.AuthorizationKey, $"bearer {token}");

        public ThreadContext WithKeyInfo(KeyInfo key, DateTime? date = null)
        {
            var context = this;

            context = context.WithApiKey(key.Key);

            if (key.Secret != null)
            {
                var apiSig = ApiSig.CreateApiSig(key.Secret, date);
                context = context.WithApiSignature(apiSig);
            }

            return context;
        }

        public ThreadContext WithContext(ThreadContext context)
        {
            lock (syncObj)
            {
                var mergedEntries = this.Metadata.Union(context.Metadata).ToList();
                this.Metadata.Clear();
                foreach (var entry in mergedEntries)
                {
                    this.Metadata.Add(entry);
                }
            }
            return this;
        }

        private ThreadContext WithEntry(string key, string value)
        {
            if (value != null)
            {
                lock (syncObj)
                {
                    var oldEntry = this.Metadata.Get(key);
                    if (oldEntry != null)
                    {
                        this.Metadata.Remove(oldEntry);
                    }
                    this.Metadata.Add(new Entry(key, value));
                }
            }
            return this;
        }
    }
}
