using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Textile.Context;
using Textile.Threads.Client.Grpc;
using Textile.Threads.Client.Models;
using Textile.Threads.Core;

namespace Textile.Threads.Client
{
    public class ReadTransaction
    {
        private readonly IThreadContext _threadContext;
        private readonly API.APIClient _client;
        private readonly ThreadId _threadId;
        private readonly string _collectionName;
        private AsyncDuplexStreamingCall<ReadTransactionRequest, ReadTransactionReply> _readCall;

        public ReadTransaction(Context.IThreadContext threadContext, API.APIClient apiClient, ThreadId threadId, string collectionName)
        {
            this._threadContext = threadContext;
            _client = apiClient;
            this._threadId = threadId;
            this._collectionName = collectionName;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ReadTransactionRequest startRequest = new()
            {
                StartTransactionRequest = new()
                {
                    DbID = ByteString.CopyFrom(_threadId.Bytes),
                    CollectionName = _collectionName,
                }
            };

            this._readCall = _client.ReadTransaction(headers: _threadContext.Metadata, cancellationToken: cancellationToken);

            await _readCall.RequestStream.WriteAsync(startRequest);
        }

        public async Task<bool> HasAsync(IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            bool valuesExists = false;

            ReadTransactionRequest hasRequest = new()
            {
                HasRequest = new()
                {
                    DbID = ByteString.CopyFrom(_threadId.Bytes),
                    CollectionName = _collectionName
                }
            };

            hasRequest.HasRequest.InstanceIDs.AddRange(values);

            Task readTask = Task.Run(async () =>
            {
                await foreach (ReadTransactionReply message in _readCall.ResponseStream.ReadAllAsync())
                {
                    if (message.HasReply != null)
                    {
                        valuesExists = message.HasReply.Exists;
                        break;
                    }
                }
            }, cancellationToken);

            await _readCall.RequestStream.WriteAsync(hasRequest);

            await readTask;

            return valuesExists;
        }

        public async Task<T> FindByIdAsync<T>(string instanceId, CancellationToken cancellationToken = default)
        {
            T instance = default;

            ReadTransactionRequest findByIDRequest = new()
            {
                FindByIDRequest = new()
                {
                    DbID = ByteString.CopyFrom(_threadId.Bytes),
                    CollectionName = _collectionName,
                    InstanceID = instanceId
                }
            };

            Task readTask = Task.Run(async () =>
            {
                await foreach (ReadTransactionReply message in _readCall.ResponseStream.ReadAllAsync())
                {
                    if (message.FindByIDReply != null)
                    {
                        instance = JsonSerializer.Deserialize<T>(message.FindByIDReply.Instance.ToStringUtf8());
                        break;
                    }
                }
            }, cancellationToken);

            await _readCall.RequestStream.WriteAsync(findByIDRequest);

            await readTask;

            return instance;
        }

        public async Task<IList<T>> FindAsync<T>(Query query, CancellationToken cancellationToken = default)
        {
            List<T> instances = new();

            ReadTransactionRequest findRequest = new()
            {
                FindRequest = new()
                {
                    DbID = ByteString.CopyFrom(_threadId.Bytes),
                    CollectionName = _collectionName,
                    QueryJSON = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(query))
                }
            };

            Task readTask = Task.Run(async () =>
            {
                await foreach (ReadTransactionReply message in _readCall.ResponseStream.ReadAllAsync())
                {
                    if (message.FindReply != null)
                    {
                        instances = message.FindReply.Instances.Select(i => JsonSerializer.Deserialize<T>(i.ToByteArray())).ToList(); ;
                        break;
                    }
                }
            }, cancellationToken);

            await _readCall.RequestStream.WriteAsync(findRequest);

            await readTask;

            return instances;
        }

        public Task EndAsync()
        {
            return _readCall.RequestStream.CompleteAsync();
        }
    }
}