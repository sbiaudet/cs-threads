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

namespace Textile.Threads.Client
{
    public class ThreadClient : IThreadClient
    {
        private readonly IThreadContext _threadContext;
        private readonly API.APIClient _apiClient;

        public ThreadClient(IThreadContext threadContext, API.APIClient apiClient)
        {
            this._threadContext = threadContext;
            this._apiClient = apiClient;
        }

       
        public Task<string> GetTokenAsync(IIdentity identity)
            => GetTokenChallenge(identity.Public.ToString(), challenge => Task.FromResult(identity.Sign(challenge)));

        public async Task<String> GetTokenChallenge(string publicKey, Func<byte[], Task<byte[]>> VerifySignature)
        {
            string token = string.Empty;

            using var call = _apiClient.GetToken(headers: _threadContext.Metadata,  deadline: DateTime.UtcNow.AddSeconds(5));

            var keyReq = new GetTokenRequest()
            {
                Key = publicKey
            };

            var readTask = Task.Run(async () =>
            {
                await foreach(var message in call.ResponseStream.ReadAllAsync())
                {
                    if (!message.Challenge.IsEmpty)
                    {
                        var challenge = message.Challenge.ToByteArray();
                        var signature = await VerifySignature(challenge);
                        var signReq = new GetTokenRequest
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
            });

            await call.RequestStream.WriteAsync(keyReq);

            await readTask;

            if (call.GetStatus().StatusCode == StatusCode.OK)
            {
                _threadContext.WithToken(token);
                return token;
            }
            else
            {
                throw new Exception("");
            }
            
        }

        public async Task<ThreadId> NewDBAsync(ThreadId threadId, string name = null)
        {
            var dbId = threadId ?? ThreadId.FromRandom();

            var request = new NewDBRequest()
            {
                DbID = ByteString.CopyFrom(dbId.Bytes)
            };

            if (!string.IsNullOrEmpty(name))
            {
                _threadContext.WithThreadName(name);
                request.Name = name;
            }

            var reply = await _apiClient.NewDBAsync(request, headers: _threadContext.WithThread(dbId.ToString()).Metadata);
            return dbId;
        }

        public async Task DeleteDBAsync(ThreadId threadId)
        {
            var request = new DeleteDBRequest()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };

            await _apiClient.DeleteDBAsync(request, headers: _threadContext.Metadata);
        }

        public async Task<IDictionary<string, GetDBInfoReply>> ListDBsAsync()
        {
            var request = new ListDBsRequest();

            var list = await _apiClient.ListDBsAsync(request, headers: _threadContext.Metadata);
            return list.Dbs.ToDictionary(db => ThreadId.FromBytes(db.DbID.ToByteArray()).ToString(), db => db.Info);
        }

        public async Task<DBInfo> GetDbInfoAsync(ThreadId threadId)
        {
            var request = new GetDBInfoRequest()
            {
                DbID = ByteString.CopyFrom(threadId.Bytes)
            };
            
            var dbInfo = await _apiClient.GetDBInfoAsync(request, headers: _threadContext.Metadata);
            return new DBInfo()
            {
                Key = ThreadKey.FromBytes(dbInfo.Key.ToByteArray()).ToString(),
                Addrs = dbInfo.Addrs.Select(addr => Multiaddress.Decode(addr.ToByteArray()).ToString()).ToList()
            };
        }

        public async Task<ThreadId> NewDbFromAdd(string address, string key, IList<CollectionConfig> collections)
        {
            var addr = Multiaddress.Decode(address);
            var keyBytes = ThreadKey.FromString(key).Bytes;

            var request = new NewDBFromAddrRequest()
            {
                Addr = ByteString.CopyFrom(addr.ToBytes()),
                Key = ByteString.CopyFrom(keyBytes)
            };

            if(collections != null)
            {
                //TODO: Finish mapping
                //collections.Select(c =>
                //{
                //    var config = new CollectionConfig();
                //});
            }

            await _apiClient.NewDBFromAddrAsync(request, headers: _threadContext.Metadata);
            //TODO: Get the threadID and Address
            var threadId = string.Empty;
            return ThreadId.FromString(threadId);
        }

    }
}
