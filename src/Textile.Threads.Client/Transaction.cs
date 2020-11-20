using System;
using System.Collections.Generic;
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
    public abstract class Transaction<TRequest, TReply>
    {
        public Transaction(Context.IThreadContext threadContext, API.APIClient apiClient, ThreadId threadId, string collectionName)
        {
            this.ThreadContext = threadContext;
            Client = apiClient;
            ThreadId = threadId;
            CollectionName = collectionName;
        }

        protected IThreadContext ThreadContext { get; }
        protected API.APIClient Client { get; }
        protected ThreadId ThreadId { get; }
        protected string CollectionName { get; }

        protected AsyncDuplexStreamingCall<TRequest, TReply> AsyncCall { get; set; }

        public abstract Task StartAsync(CancellationToken cancellationToken = default);
        public abstract Task<bool> HasAsync(IEnumerable<string> values, CancellationToken cancellationToken = default);
        public abstract Task<T> FindByIdAsync<T>(string instanceId, CancellationToken cancellationToken = default);
        public abstract Task<IList<T>> FindAsync<T>(Query query, CancellationToken cancellationToken = default);


        protected void CheckTransactionStarted()
        {
            if (AsyncCall == null)
            {
                throw new InvalidOperationException("Transaction is not started. Call StartAsync before use this");
            }
        }

        public async Task EndAsync(CancellationToken cancellationToken = default)
        {
            CheckTransactionStarted();

            Task readTask = Task.Run(async () =>
            {
                await foreach (TReply message in AsyncCall.ResponseStream.ReadAllAsync())
                {
                    if (message != null)
                    {
                        break;
                    }
                }
            }, cancellationToken);

            try
            {
                await AsyncCall.RequestStream.CompleteAsync();

                await readTask;
            }
            catch (RpcException ex)
            {
                throw new InvalidOperationException(ex.Status.Detail);
            }
        }
    }
}