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
    public class ReadTransaction : Transaction<ReadTransactionRequest, ReadTransactionReply>
    {

        public ReadTransaction(Context.IThreadContext threadContext, API.APIClient apiClient, ThreadId threadId, string collectionName) : base(threadContext, apiClient, threadId, collectionName)
        {
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ReadTransactionRequest startRequest = new()
            {
                StartTransactionRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                }
            };

            this.AsyncCall = Client.ReadTransaction(headers: ThreadContext.Metadata, cancellationToken: cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(startRequest);
        }

        public override async Task<bool> HasAsync(IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            bool valuesExists = false;

            ReadTransactionRequest hasRequest = new()
            {
                HasRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName
                }
            };

            hasRequest.HasRequest.InstanceIDs.AddRange(values);

            Task readTask = Task.Run(async () =>
            {
                await foreach (ReadTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.HasReply != null)
                    {
                        valuesExists = message.HasReply.Exists;
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(hasRequest);

            await readTask;

            return valuesExists;
        }

        public override async Task<T> FindByIdAsync<T>(string instanceId, CancellationToken cancellationToken = default)
        {
            T instance = default;

            ReadTransactionRequest findByIDRequest = new()
            {
                FindByIDRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                    InstanceID = instanceId
                }
            };

            Task readTask = Task.Run(async () =>
            {
                await foreach (ReadTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.FindByIDReply != null)
                    {
                        instance = JsonSerializer.Deserialize<T>(message.FindByIDReply.Instance.ToStringUtf8());
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(findByIDRequest);

            await readTask;

            return instance;
        }

        public override async Task<IList<T>> FindAsync<T>(Query query, CancellationToken cancellationToken = default)
        {
            List<T> instances = new();

            ReadTransactionRequest findRequest = new()
            {
                FindRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                    QueryJSON = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(query))
                }
            };

            Task readTask = Task.Run(async () =>
            {
                await foreach (ReadTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.FindReply != null)
                    {
                        instances = message.FindReply.Instances.Select(i => JsonSerializer.Deserialize<T>(i.ToByteArray())).ToList(); ;
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(findRequest);

            await readTask;

            return instances;
        }

    }
}