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
    public class WriteTransaction : Transaction<WriteTransactionRequest, WriteTransactionReply>
    {

        public WriteTransaction(Context.IThreadContext threadContext, API.APIClient apiClient, ThreadId threadId, string collectionName) : base(threadContext, apiClient, threadId, collectionName)
        {
        }

        public override async Task StartAsync(CancellationToken cancellationToken = default)
        {
            WriteTransactionRequest startRequest = new()
            {
                StartTransactionRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                }
            };

            this.AsyncCall = Client.WriteTransaction(headers: ThreadContext.Metadata, cancellationToken: cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(startRequest);
        }

        public override async Task<bool> HasAsync(IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            CheckTransactionStarted();

            bool valuesExists = false;

            WriteTransactionRequest hasRequest = new()
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
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
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
            CheckTransactionStarted();

            T instance = default;

            WriteTransactionRequest findByIDRequest = new()
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
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.FindByIDReply != null)
                    {
                        if (!message.FindByIDReply.Instance.IsEmpty)
                        {
                            instance = JsonSerializer.Deserialize<T>(message.FindByIDReply.Instance.ToStringUtf8());
                        }
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
            CheckTransactionStarted();

            List<T> instances = new();

            WriteTransactionRequest findRequest = new()
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
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
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

        public async Task<IList<string>> CreateAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken = default)
        {
            CheckTransactionStarted();

            List<string> ids = new();

            WriteTransactionRequest createRequest = new()
            {
                CreateRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                }
            };
            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            createRequest.CreateRequest.Instances.AddRange(serializedValues);

            Task readTask = Task.Run(async () =>
            {
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.CreateReply != null)
                    {
                        ids = message.CreateReply.InstanceIDs.ToList();
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(createRequest);

            await readTask;

            return ids;
        }

        public async Task VerifyAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken = default)
        {
            CheckTransactionStarted();

            WriteTransactionRequest verifyRequest = new()
            {
                VerifyRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                }
            };
            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            verifyRequest.VerifyRequest.Instances.AddRange(serializedValues);

            Task readTask = Task.Run(async () =>
            {
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.VerifyReply != null)
                    {
                        if (!string.IsNullOrEmpty(message.VerifyReply.TransactionError))
                        {
                            throw new InvalidOperationException(message.VerifyReply.TransactionError);
                        }
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(verifyRequest);

            await readTask;
        }

        public async Task SaveAsync<T>(IEnumerable<T> values, CancellationToken cancellationToken = default)
        {
            CheckTransactionStarted();

            WriteTransactionRequest saveRequest = new()
            {
                SaveRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                }
            };
            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            saveRequest.SaveRequest.Instances.AddRange(serializedValues);

            Task readTask = Task.Run(async () =>
            {
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.SaveReply != null)
                    {
                        if (!string.IsNullOrEmpty(message.SaveReply.TransactionError))
                        {
                            throw new InvalidOperationException(message.SaveReply.TransactionError);
                        }
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(saveRequest);

            await readTask;
        }

        public async Task DeleteAsync(IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            CheckTransactionStarted();

            WriteTransactionRequest deleteRequest = new()
            {
                DeleteRequest = new()
                {
                    DbID = ByteString.CopyFrom(ThreadId.Bytes),
                    CollectionName = CollectionName,
                }
            };
            deleteRequest.DeleteRequest.InstanceIDs.AddRange(values);

            Task readTask = Task.Run(async () =>
            {
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message.DeleteReply != null)
                    {
                        if (!string.IsNullOrEmpty(message.DeleteReply.TransactionError))
                        {
                            throw new InvalidOperationException(message.DeleteReply.TransactionError);
                        }
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(deleteRequest);

            await readTask;
        }

        public async Task DiscardAsync(CancellationToken cancellationToken = default)
        {
            CheckTransactionStarted();

            WriteTransactionRequest discardRequest = new()
            {
                DiscardRequest = new()
            };

            Task readTask = Task.Run(async () =>
            {
                await foreach (WriteTransactionReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message?.DiscardReply != null)
                    {
                        break;
                    }
                }
            }, cancellationToken);

            await AsyncCall.RequestStream.WriteAsync(discardRequest);

            await readTask;
        }
    }
}