using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Textile.Crypto;
using Textile.Threads.Client.Grpc;
using Textile.Threads.Core;
using Textile.Threads.Client.Models;
using System.Threading;

namespace Textile.Threads.Client
{
    public interface IThreadClient
    {

        Task<string> GetTokenAsync(IIdentity identity);
        Task<ThreadId> NewDBAsync(ThreadId threadId, string name = null);
        Task DeleteDBAsync(ThreadId threadId);
        Task<IDictionary<string, GetDBInfoReply>> ListDBsAsync();
        Task<DBInfo> GetDbInfoAsync(ThreadId dbId);
        Task<ThreadId> NewDbFromAdd(string address, string key, IList<Models.CollectionInfo> collections);
        Task NewCollection(ThreadId threadId, Models.CollectionInfo config);
        Task<IList<T>> Find<T>(ThreadId threadId, string collectionName, Query query, CancellationToken cancellationToken = default);
        Task Save<T>(ThreadId threadId, string collectionName, T[] values, CancellationToken cancellationToken = default);
        Task<IList<string>> Create<T>(ThreadId threadId, string collectionName, T[] values, CancellationToken cancellationToken = default);
        Task DeleteCollection(ThreadId threadId, string name);
        Task UpdateCollection(ThreadId threadId, CollectionInfo config);
        Task<IList<Models.CollectionInfo>> ListCollection(ThreadId threadId, CancellationToken cancellationToken = default);
        Task<CollectionInfo> GetCollectionInfo(ThreadId threadId, string name);
        Task<IList<Grpc.Index>> GetCollectionIndexes(ThreadId threadId, string name, CancellationToken cancellationToken = default);
    }
}
