using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Net.Client;
using Grpc.Core;
using Textile.Threads.Client.Grpc;
using Textile.Context;
using Textile.Crypto;
using Textile.Security;
using System.Threading;
using Textile.Threads.Core;
using System.Collections.Generic;
using System.Linq;
using Multiformats.Address;
using System.Text.Json;
using Textile.Threads.Client.Models;
using AutoMapper;
using System.Runtime.CompilerServices;

namespace Textile.Threads.Client
{
    /// <summary>
    ///  <see cref="ThreadClient"/> is a gRPC wrapper client for communicating with a Threads server.
    /// This client library can be used to interact with a local or remote Textile gRPC-service
    /// It is a wrapper around Textile Thread's 'DB' API, which is defined here:
    /// https://github.com/textileio/go-threads/blob/master/api/pb/api.proto.
    /// </summary>
    public class ThreadClient : IThreadClient
    {
        private readonly IThreadContext _threadContext;
        private readonly IMapper _mapper;
        private readonly API.APIClient _apiClient;

        /// <summary>
        /// Initializes a new instance with the specified configuration.
        /// </summary>
        /// <param name="threadContext"></param>
        /// <param name="mapper"></param>
        /// <param name="apiClient"></param>
        public ThreadClient(IThreadContext threadContext, IMapper mapper, API.APIClient apiClient)
        {
            this._threadContext = threadContext;
            this._mapper = mapper;
            this._apiClient = apiClient;
        }

        /// <summary>
        /// Obtain a token per user (identity) for interacting with the remote API.
        /// </summary>
        /// <param name="identity">A user identity to use for creating records in the database. A random identity
        /// can be created with `Client.randomIdentity(), however, it is not easy/possible to migrate
        /// identities after the fact.Please store or otherwise persist any identity information if
        /// you wish to retrieve user data later, or use an external identity provider.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public Task<string> GetTokenAsync(IIdentity identity, CancellationToken cancellationToken = default)
        {
            return GetTokenChallenge(identity.PublicKey.ToString(), challenge => Task.FromResult(identity.Sign(challenge)), cancellationToken);
        }

        /// <summary>
        /// Obtain a token per user (identity) for interacting with the remote API.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="VerifySignature"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<string> GetTokenChallenge(string publicKey, Func<byte[], Task<byte[]>> VerifySignature, CancellationToken cancellationToken = default)
        {
            string token = string.Empty;

            using AsyncDuplexStreamingCall<GetTokenRequest, GetTokenReply> call = _apiClient.GetToken(headers: _threadContext.Metadata, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: cancellationToken);

            GetTokenRequest keyReq = new()
            {
                Key = publicKey
            };

            Task readTask = Task.Run(async () =>
            {
                await foreach (GetTokenReply message in call.ResponseStream.ReadAllAsync())
                {
                    if (!message.Challenge.IsEmpty)
                    {
                        byte[] challenge = message.Challenge.ToByteArray();
                        byte[] signature = await VerifySignature(challenge);
                        GetTokenRequest signReq = new()
                        {
                            Signature = ByteString.CopyFrom(signature)
                        };
                        await call.RequestStream.WriteAsync(signReq);
                        await call.RequestStream.CompleteAsync();
                    }
                    else if (message.Token != null)
                    {
                        token = message.Token;
                    }
                }
            }, cancellationToken);

            await call.RequestStream.WriteAsync(keyReq);

            await readTask;

            if (call.GetStatus().StatusCode == StatusCode.OK)
            {
                _threadContext.WithToken(token);
                return token;
            }
            else
            {
                throw new InvalidOperationException(call.GetStatus().Detail);
            }

        }

        /// <summary>
        /// Creates a new store on the remote node.
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="name"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<ThreadId> NewDBAsync(ThreadId threadId, string name = null, CancellationToken cancellationToken = default)
        {
            ThreadId dbId = threadId ?? ThreadId.FromRandom();

            NewDBRequest request = new()
            {
                DbID = ByteString.CopyFrom(dbId.Bytes)
            };

            if (!string.IsNullOrEmpty(name))
            {
                _threadContext.WithThreadName(name);
                request.Name = name;
            }

            await _apiClient.NewDBAsync(request, headers: _threadContext.WithThread(dbId.ToString()).Metadata, cancellationToken: cancellationToken);
            return dbId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task DeleteDBAsync(ThreadId threadId, CancellationToken cancellationToken = default)
        {
            DeleteDBRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            await _apiClient.DeleteDBAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<IDictionary<string, GetDBInfoReply>> ListDBsAsync(CancellationToken cancellationToken = default)
        {
            ListDBsRequest request = new();

            ListDBsReply list = await _apiClient.ListDBsAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return list.Dbs.ToDictionary(db => ThreadId.FromBytes(db.DbID.ToByteArray()).ToString(), db => db.Info);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbId">The ID of the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<DBInfo> GetDbInfoAsync(ThreadId dbId, CancellationToken cancellationToken = default)
        {
            GetDBInfoRequest request = new()
            {
                DbID = ByteString.CopyFrom(dbId.Bytes)
            };

            GetDBInfoReply dbInfo = await _apiClient.GetDBInfoAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return new DBInfo()
            {
                Key = ThreadKey.FromBytes(dbInfo.Key.ToByteArray()).ToString(),
                Addrs = dbInfo.Addrs.Select(addr => Multiaddress.Decode(addr.ToByteArray()).ToString()).ToList()
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="key"></param>
        /// <param name="collections"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<ThreadId> NewDbFromAdd(string address, string key, IList<Models.CollectionInfo> collections, CancellationToken cancellationToken = default)
        {
            Multiaddress addr = Multiaddress.Decode(address);
            byte[] keyBytes = ThreadKey.FromString(key).Bytes;

            NewDBFromAddrRequest request = new()
            {
                Addr = ByteString.CopyFrom(addr.ToBytes()),
                Key = ByteString.CopyFrom(keyBytes)
            };

            if (collections != null)
            {
                //TODO: Finish mapping
                request.Collections.AddRange(collections.Select(c =>
                {
                    return _mapper.Map<Grpc.CollectionConfig>(c);
                }).ToArray());
            }

            await _apiClient.NewDBFromAddrAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            //TODO: Get the threadID and Address
            string threadId = string.Empty;
            return ThreadId.FromString(threadId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">the ID of the database</param>
        /// <param name="config"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task NewCollectionAsync(ThreadId threadId, Models.CollectionInfo config, CancellationToken cancellationToken = default)
        {
            NewCollectionRequest request = new()
            {
                Config = _mapper.Map<Grpc.CollectionConfig>(config),
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            await _apiClient.NewCollectionAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="config"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task UpdateCollectionAsync(ThreadId threadId, Models.CollectionInfo config, CancellationToken cancellationToken = default)
        {
            UpdateCollectionRequest request = new()
            {
                Config = _mapper.Map<Grpc.CollectionConfig>(config),
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            await _apiClient.UpdateCollectionAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="name">The human-readable name for the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task DeleteCollectionAsync(ThreadId threadId, string name, CancellationToken cancellationToken = default)
        {
            DeleteCollectionRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                Name = name
            };

            await _apiClient.DeleteCollectionAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="name">The human-readable name for the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<CollectionInfo> GetCollectionInfoAsync(ThreadId threadId, string name, CancellationToken cancellationToken = default)
        {
            GetCollectionInfoRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                Name = name
            };

            GetCollectionInfoReply reply = await _apiClient.GetCollectionInfoAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return _mapper.Map<CollectionInfo>(reply);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<IList<Models.CollectionInfo>> ListCollectionAsync(ThreadId threadId, CancellationToken cancellationToken = default)
        {
            ListCollectionsRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            ListCollectionsReply reply = await _apiClient.ListCollectionsAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return _mapper.Map<List<Models.CollectionInfo>>(reply.Collections.ToList());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="name">The human-readable name for the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<IList<Grpc.Index>> GetCollectionIndexesAsync(ThreadId threadId, string name, CancellationToken cancellationToken = default)
        {
            GetCollectionIndexesRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                Name = name
            };

            GetCollectionIndexesReply reply = await _apiClient.GetCollectionIndexesAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return reply.Indexes.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <param name="values"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<IList<string>> CreateAsync<T>(ThreadId threadId, string collectionName, IEnumerable<T> values, CancellationToken cancellationToken = default)
        {
            CreateRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName
            };

            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            request.Instances.AddRange(serializedValues);

            try
            {
                CreateReply reply = await _apiClient.CreateAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);

                return reply.InstanceIDs.ToList();
            }
            catch (RpcException ex) when (ex.Status.Detail == "app denied net record body")
            {
                throw new InvalidOperationException(ex.Status.Detail);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <param name="values"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task SaveAsync<T>(ThreadId threadId, string collectionName, IEnumerable<T> values, CancellationToken cancellationToken = default)
        {
            SaveRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName
            };

            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            request.Instances.AddRange(serializedValues);

            try
            {
                SaveReply reply = await _apiClient.SaveAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            }
            catch (RpcException ex) when (ex.Status.Detail == "app denied net record body")
            {
                throw new InvalidOperationException(ex.Status.Detail);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <param name="values"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task VerifyAsync<T>(ThreadId threadId, string collectionName, IEnumerable<T> values, CancellationToken cancellationToken = default)
        {
            VerifyRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName
            };

            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            request.Instances.AddRange(serializedValues);

            try
            {
                VerifyReply reply = await _apiClient.VerifyAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            }
            catch (RpcException ex) when (ex.Status.Detail == "app denied net record body")
            {
                throw new InvalidOperationException(ex.Status.Detail);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <param name="values"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task DeleteAsync(ThreadId threadId, string collectionName, IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            DeleteRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName,
            };

            request.InstanceIDs.AddRange(values);

            try
            {
                DeleteReply reply = await _apiClient.DeleteAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            }
            catch (RpcException ex)
            {
                throw new InvalidOperationException(ex.Status.Detail);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <param name="values"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<bool> Has(ThreadId threadId, string collectionName, IEnumerable<string> values, CancellationToken cancellationToken = default)
        {
            HasRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName,
            };

            request.InstanceIDs.AddRange(values);

            HasReply reply = await _apiClient.HasAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);

            return reply.Exists;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <param name="query"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<IList<T>> FindAsync<T>(ThreadId threadId, string collectionName, Query query, CancellationToken cancellationToken = default)
        {
            FindRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName,
                QueryJSON = ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes(query))
            };

            FindReply reply = await _apiClient.FindAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);

            return reply.Instances.Select(i => JsonSerializer.Deserialize<T>(i.ToByteArray())).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <param name="instanceId"></param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async Task<T> FindByIdAsync<T>(ThreadId threadId, string collectionName, string instanceId, CancellationToken cancellationToken = default)
        {
            FindByIDRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName,
                InstanceID = instanceId
            };

            FindByIDReply reply = await _apiClient.FindByIDAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return JsonSerializer.Deserialize<T>(reply.Instance.ToStringUtf8());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="listenOptions">The human-readable name for the database.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
        /// <returns></returns>
        public async IAsyncEnumerable<ListenAction<T>> ListenAsync<T>(ThreadId threadId, IEnumerable<ListenOption> listenOptions, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ListenRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
            };

            IEnumerable<ListenRequest.Types.Filter> filters = listenOptions.Select(o =>
                 new ListenRequest.Types.Filter()
                 {
                     CollectionName = o.CollectionName,
                     InstanceID = o.InstanceId ?? string.Empty,
                     Action = (ListenRequest.Types.Filter.Types.Action)o.Action
                 });

            request.Filters.AddRange(filters);

            using AsyncServerStreamingCall<ListenReply> call = _apiClient.Listen(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);

            await foreach (ListenReply message in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                if (message != null)
                {
                    ListenAction<T> action = new()
                    {
                        Collection = message.CollectionName,
                        Action = (ActionType)message.Action,
                        InstanceId = message.InstanceID,
                        Instance = JsonSerializer.Deserialize<T>(message.Instance.ToStringUtf8())
                    };

                    yield return action;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <returns></returns>
        public ReadTransaction ReadTransaction(ThreadId threadId, string collectionName)
        {
            return new ReadTransaction(_threadContext, _apiClient, threadId, collectionName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadId">The ID of the database.</param>
        /// <param name="collectionName">The human-readable name for the database.</param>
        /// <returns></returns>
        public WriteTransaction WriteTransaction(ThreadId threadId, string collectionName)
        {
            return new WriteTransaction(_threadContext, _apiClient, threadId, collectionName);
        }
    }
}
