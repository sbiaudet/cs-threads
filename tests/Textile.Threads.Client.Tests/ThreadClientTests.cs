using System;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Json.Schema;
using Microsoft.Extensions.Options;
using Textile.Context;
using Textile.Crypto;
using Textile.Security;
using Textile.Threads.Client.Grpc;
using Textile.Threads.Core;
using Xunit;

namespace Textile.Threads.Client.Tests
{
    public class ThreadClientTests
    {

        private const string personSchema = "{ \"$id\": \"https://example.com/person.schema.json\", \"$schema\": \"http://json-schema.org/draft-07/schema#\", \"title\": \"Person\", \"type\": \"object\", \"required\": [\"_id\"], \"properties\": { \"_id\": { \"type\": \"string\", \"description\": \"The instance's id.\" }, \"firstName\": { \"type\": \"string\", \"description\": \"The person's first name.\" }, \"lastName\": { \"type\": \"string\", \"description\": \"The person's last name.\" }, \"age\": { \"description\": \"Age in years which must be equal to or greater than zero.\", \"type\": \"integer\", \"minimum\": 0 } } }";
        private const string schema2 = "{ \"properties\": { \"_id\": { \"type\": \"string\" }, \"fullName\": { \"type\": \"string\" }, \"age\": { \"type\": \"integer\", \"minimum\": 0 } }";

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
            _ = await client.GetTokenAsync(user);
            _ = await client.NewDBAsync(ThreadId.FromRandom(), "test");
        }

        [Fact]
        public async Task Should_Get_A_Valid_Db_Info()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
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
            Assert.True(dbList.Count >= 1, "Expected 1 or more database");
            Assert.Contains(dbList, db => name2 == db.Value.Name);
        }

        [Fact]
        public async Task Should_Cleanly_Delete_a_Database()
        {
            var user = Private.FromRandom();

            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var dbId = await client.NewDBAsync(ThreadId.FromRandom());
            var before = (await client.ListDBsAsync()).Count;
            await client.DeleteDBAsync(dbId);
            var after = (await client.ListDBsAsync()).Count;
            Assert.Equal(before, after + 1);
        }


        [Fact]
        public async Task NewCollection_should_work_and_create_an_empty_object()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom());
            await client.NewCollection(db, new Models.CollectionConfig() { Name = "Person", Schema = JsonSchema.FromText(personSchema) });
        }
    }
}
