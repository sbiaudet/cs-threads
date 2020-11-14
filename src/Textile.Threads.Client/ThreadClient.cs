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

namespace Textile.Threads.Client
{

    public class ThreadClient : IThreadClient
    {
        private readonly IThreadContext _threadContext;
        private readonly IMapper _mapper;
        private readonly API.APIClient _apiClient;

        public ThreadClient(IThreadContext threadContext, IMapper mapper, API.APIClient apiClient)
        {
            this._threadContext = threadContext;
            this._mapper = mapper;
            this._apiClient = apiClient;
        }

        public Task<string> GetTokenAsync(IIdentity identity, CancellationToken cancellationToken = default)
        {
            return GetTokenChallenge(identity.PublicKey.ToString(), challenge => Task.FromResult(identity.Sign(challenge)), cancellationToken);
        }

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
                throw call.GetStatus().DebugException;
            }

        }

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

        public async Task DeleteDBAsync(ThreadId threadId, CancellationToken cancellationToken = default)
        {
            DeleteDBRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            await _apiClient.DeleteDBAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }

        public async Task<IDictionary<string, GetDBInfoReply>> ListDBsAsync(CancellationToken cancellationToken = default)
        {
            ListDBsRequest request = new();

            ListDBsReply list = await _apiClient.ListDBsAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return list.Dbs.ToDictionary(db => ThreadId.FromBytes(db.DbID.ToByteArray()).ToString(), db => db.Info);
        }

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

        public async Task NewCollection(ThreadId threadId, Models.CollectionInfo config, CancellationToken cancellationToken = default)
        {
            NewCollectionRequest request = new()
            {
                Config = _mapper.Map<Grpc.CollectionConfig>(config),
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            await _apiClient.NewCollectionAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }


        public async Task UpdateCollection(ThreadId threadId, Models.CollectionInfo config, CancellationToken cancellationToken = default)
        {
            UpdateCollectionRequest request = new()
            {
                Config = _mapper.Map<Grpc.CollectionConfig>(config),
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            await _apiClient.UpdateCollectionAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }

        public async Task DeleteCollection(ThreadId threadId, string name, CancellationToken cancellationToken = default)
        {
            DeleteCollectionRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                Name = name
            };

            await _apiClient.DeleteCollectionAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }


        public async Task<CollectionInfo> GetCollectionInfo(ThreadId threadId, string name, CancellationToken cancellationToken = default)
        {
            GetCollectionInfoRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                Name = name
            };

            GetCollectionInfoReply reply = await _apiClient.GetCollectionInfoAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return _mapper.Map<CollectionInfo>(reply);
        }

        public async Task<IList<Models.CollectionInfo>> ListCollection(ThreadId threadId, CancellationToken cancellationToken = default)
        {
            ListCollectionsRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            ListCollectionsReply reply = await _apiClient.ListCollectionsAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return _mapper.Map<List<Models.CollectionInfo>>(reply.Collections.ToList());
        }


        public async Task<IList<Grpc.Index>> GetCollectionIndexes(ThreadId threadId, string name, CancellationToken cancellationToken = default)
        {
            GetCollectionIndexesRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                Name = name
            };

            GetCollectionIndexesReply reply = await _apiClient.GetCollectionIndexesAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return reply.Indexes.ToList();
        }

        public async Task<IList<string>> Create<T>(ThreadId threadId, string collectionName, T[] values, CancellationToken cancellationToken = default)
        {
            CreateRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName
            };

            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            request.Instances.AddRange(serializedValues);

            CreateReply reply = await _apiClient.CreateAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
            return reply.InstanceIDs.ToList();
        }


        public async Task Save<T>(ThreadId threadId, string collectionName, T[] values, CancellationToken cancellationToken = default)
        {
            SaveRequest request = new()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes),
                CollectionName = collectionName
            };

            IEnumerable<ByteString> serializedValues = values.Select(v => ByteString.CopyFrom(JsonSerializer.SerializeToUtf8Bytes<T>(v)));
            request.Instances.AddRange(serializedValues);

            SaveReply reply = await _apiClient.SaveAsync(request, headers: _threadContext.Metadata, cancellationToken: cancellationToken);
        }

        public async Task<IList<T>> Find<T>(ThreadId threadId, string collectionName, Query query, CancellationToken cancellationToken = default)
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
    }
}
