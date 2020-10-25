using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Textile.Context;
using Textile.Crypto;
using Textile.Security;
using Textile.Threads.Client.Grpc;
using Textile.Threads.Core;
using Xunit;

namespace Textile.Threads.Client.Tests
{
    public class ThreadClientFactoryTests
    {
        [Fact]
        public void Should_Create_A_New_Client()
        {
            var options = Options.Create(new ThreadContextOptions() { Host = DefaultThreadContextConfigureOptions.DefaultHost });

            var context = new ThreadContext(options);
            var client = new ThreadClient(context, new API.APIClient(GrpcChannel.ForAddress(options.Value.Host)));

            Assert.NotNull(client);
        }

        [Fact]
        public async Task Should_Get_A_New_Token()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            var token = await client.GetTokenAsync(user);
            Assert.NotNull(token);
        }
       
        [Fact]
        public async Task Should_Create_A_New_Db()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            var token = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom(), "test");
        }

        [Fact]
        public async Task Should_Get_A_Valid_Db_Info()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            var token = await client.GetTokenAsync(user);
            var dbId = await client.NewDBAsync(ThreadId.FromRandom());

            var invites = await client.GetDbInfoAsync(dbId);
            Assert.NotNull(invites);
            Assert.NotNull(invites.Addrs[0]);
            Assert.NotNull(invites.Key);
        }

        [Fact]
        public async Task Should_List_Dbs()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            var token = await client.GetTokenAsync(user);
            var name2 = "name2";
            var db = await client.NewDBAsync(ThreadId.FromRandom(), name2);
            var dbList = await client.ListDBsAsync();
            Assert.True(dbList.Count > 1, "Expected 1 or more database");
            Assert.Contains(dbList, db => name2 == db.Value.Name);
        }

        [Fact]
        public async Task Should_Cleanly_Delete_a_Database()
        {
            var user = Private.FromRandom();

            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            var token = await client.GetTokenAsync(user);
            var dbId = await client.NewDBAsync(ThreadId.FromRandom());
            var before = (await client.ListDBsAsync()).Count;
            await client.DeleteDBAsync(dbId);
            var after = (await client.ListDBsAsync()).Count;
            Assert.Equal(before, after + 1);
        }
    }
}
