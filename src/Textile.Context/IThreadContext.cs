using System;
using Grpc.Core;
using Textile.Security;

namespace Textile.Context
{
    public interface IThreadContext
    {
        Metadata Metadata { get; }

        public string Host { get; set; }

        ThreadContext WithApiKey(string apiKey);
        ThreadContext WithApiSignature(ApiSig apiSignature);
        ThreadContext WithContext(ThreadContext context);
        ThreadContext WithKeyInfo(KeyInfo key, DateTime? keyDate = null);
        ThreadContext WithOrganization(string organization);
        ThreadContext WithSession(string session);
        ThreadContext WithThread(string threadId);
        ThreadContext WithThreadName(string threadName);
        ThreadContext WithToken(string token);
    }
}