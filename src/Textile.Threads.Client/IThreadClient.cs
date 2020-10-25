using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Textile.Crypto;
using Textile.Threads.Client.Grpc;
using Textile.Threads.Core;

namespace Textile.Threads.Client
{
    public interface IThreadClient
    {

        Task<string> GetTokenAsync(IIdentity identity);
        Task<ThreadId> NewDBAsync(ThreadId threadId, string name = null);
        Task DeleteDBAsync(ThreadId threadId);
        Task<IDictionary<string, GetDBInfoReply>> ListDBsAsync();
        Task<DBInfo> GetDbInfoAsync(ThreadId dbId);
        Task<ThreadId> NewDbFromAdd(string address, string key, IList<CollectionConfig> collections);
    }
}
