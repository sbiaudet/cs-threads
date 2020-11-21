using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Textile.Crypto;
using Textile.Threads.Client.Grpc;
using Textile.Threads.Core;
using Textile.Threads.Client.Models;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Textile.Threads.Client
{
    public interface IThreadClient
    {

        Task<string> GetTokenAsync(IIdentity identity, CancellationToken cancellationToken = default);
        Task<ThreadId> NewDBAsync(ThreadId threadId, string name = null, CancellationToken cancellationToken = default);
        Task DeleteDBAsync(ThreadId threadId, CancellationToken cancellationToken = default);
        Task<IDictionary<string, GetDBInfoReply>> ListDBsAsync(CancellationToken cancellationToken = default);
        Task<DBInfo> GetDbInfoAsync(ThreadId dbId, CancellationToken cancellationToken = default);
        Task<ThreadId> NewDbFromAdd(string address, string key, IList<Models.CollectionInfo> collections, CancellationToken cancellationToken = default);
        Task NewCollectionAsync(ThreadId threadId, Models.CollectionInfo config, CancellationToken cancellationToken = default);
        Task<IList<T>> FindAsync<T>(ThreadId threadId, string collectionName, Query query, CancellationToken cancellationToken = default);
        Task SaveAsync<T>(ThreadId threadId, string collectionName, IEnumerable<T> values, CancellationToken cancellationToken = default);
        Task<IList<string>> CreateAsync<T>(ThreadId threadId, string collectionName, IEnumerable<T> values, CancellationToken cancellationToken = default);
        Task DeleteCollectionAsync(ThreadId threadId, string name, CancellationToken cancellationToken = default);
        Task UpdateCollectionAsync(ThreadId threadId, CollectionInfo config, CancellationToken cancellationToken = default);
        Task<IList<Models.CollectionInfo>> ListCollectionAsync(ThreadId threadId, CancellationToken cancellationToken = default);
        Task<CollectionInfo> GetCollectionInfoAsync(ThreadId threadId, string name, CancellationToken cancellationToken = default);
        Task<IList<Grpc.Index>> GetCollectionIndexesAsync(ThreadId threadId, string name, CancellationToken cancellationToken = default);
        Task DeleteAsync(ThreadId threadId, string collectionName, IEnumerable<string> values, CancellationToken cancellationToken = default);
        Task<bool> Has(ThreadId threadId, string collectionName, IEnumerable<string> values, CancellationToken cancellationToken = default);
        Task<T> FindByIdAsync<T>(ThreadId threadId, string collectionName, string instanceId, CancellationToken cancellationToken = default);
        ReadTransaction ReadTransaction(ThreadId threadId, string collectionName);
        WriteTransaction WriteTransaction(ThreadId threadId, string collectionName);
        Task VerifyAsync<T>(ThreadId threadId, string collectionName, IEnumerable<T> values, CancellationToken cancellationToken = default);
        IAsyncEnumerable<ListenAction<T>> ListenAsync<T>(ThreadId threadId, IEnumerable<ListenOption> listenOptions, CancellationToken cancellationToken = default);
    }
}
