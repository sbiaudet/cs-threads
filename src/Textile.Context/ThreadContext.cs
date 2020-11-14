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
        private readonly object syncObj = new();

        public ThreadContext(IOptions<ThreadContextOptions> options)
        {
            this.Host = options.Value.Host;

            if (options.Value.KeyInfo != null)
            {
                _ = WithKeyInfo(options.Value.KeyInfo);
            }
        }

        public Metadata Metadata { get; internal set; } = new Metadata();

        public string Host { get; set; }

        public ThreadContext WithSession(string session)
        {
            return this.WithEntry(ContextKeys.SessionKey, session);
        }

        public ThreadContext WithThread(string threadId)
        {
            return this.WithEntry(ContextKeys.ThreadIdKey, threadId);
        }

        public ThreadContext WithThreadName(string threadName)
        {
            return this.WithEntry(ContextKeys.ThreadNameKey, threadName);
        }

        public ThreadContext WithOrganization(string organization)
        {
            return WithEntry(ContextKeys.OrganizationKey, organization);
        }

        public ThreadContext WithApiKey(string apiKey)
        {
            return WithEntry(ContextKeys.ApiKey, apiKey);
        }

        public ThreadContext WithApiSignature(ApiSig apiSignature)
        {
            return this.WithEntry(ContextKeys.ApiSignatureKey, apiSignature.Sig)
                             .WithEntry(ContextKeys.ApiSignatureRawMessageKey, apiSignature.Msg);
        }

        public ThreadContext WithToken(string token)
        {
            return this.WithEntry(ContextKeys.AuthorizationKey, $"bearer {token}");
        }

        public ThreadContext WithKeyInfo(KeyInfo key, DateTime? keyDate = null)
        {
            ThreadContext context = this;

            context = context.WithApiKey(key.Key);

            if (key.Secret != null)
            {
                ApiSig apiSig = ApiSig.CreateApiSig(key.Secret, keyDate);
                context = context.WithApiSignature(apiSig);
            }

            return context;
        }

        public ThreadContext WithContext(ThreadContext context)
        {
            lock (syncObj)
            {
                System.Collections.Generic.List<Entry> mergedEntries = this.Metadata.Union(context.Metadata).ToList();
                this.Metadata.Clear();
                foreach (Entry entry in mergedEntries)
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
                    Entry oldEntry = this.Metadata.Get(key);
                    if (oldEntry != null)
                    {
                        _ = this.Metadata.Remove(oldEntry);
                    }
                    this.Metadata.Add(new Entry(key, value));
                }
            }
            return this;
        }
    }
}
