using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Json.Schema;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Textile.Context;
using Textile.Crypto;
using Textile.Security;
using Textile.Threads.Client.Grpc;
using Textile.Threads.Client.Models;
using Textile.Threads.Core;

namespace Textile.Threads.Client.Tests
{
    [TestClass]
    public class ThreadClientTests
    {

        private const string personSchema = "{ \"$id\": \"https://example.com/person.schema.json\", \"$schema\": \"http://json-schema.org/draft-07/schema#\", \"title\": \"Person\", \"type\": \"object\", \"required\": [\"_id\"], \"properties\": { \"_id\": { \"type\": \"string\", \"description\": \"The instance's id.\" }, \"firstName\": { \"type\": \"string\", \"description\": \"The person's first name.\" }, \"lastName\": { \"type\": \"string\", \"description\": \"The person's last name.\" }, \"age\": { \"description\": \"Age in years which must be equal to or greater than zero.\", \"type\": \"integer\", \"minimum\": 0 } } }";

        private const string schema2 = "{ \"properties\": { \"_id\": { \"type\": \"string\" }, \"fullName\": { \"type\": \"string\" }, \"age\": { \"type\": \"integer\", \"minimum\": 0 } } }";


        private static Person CreatePerson => new()
        {
            Id = "",
            FirstName = "Adam",
            LastName = "Doe",
            Age = 21
        };

        [TestMethod]
        public async Task ShouldGetANewToken()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            string token = await client.GetTokenAsync(user);
            Assert.IsNotNull(token);
        }

        [TestMethod]
        public async Task ShouldCreateANewDb()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            _ = await client.NewDBAsync(ThreadId.FromRandom(), "test");
        }

        [TestMethod]
        public async Task ShouldGetAValidDbInfo()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId dbId = await client.NewDBAsync(ThreadId.FromRandom());

            Models.DBInfo invites = await client.GetDbInfoAsync(dbId);
            Assert.IsNotNull(invites);
            Assert.IsNotNull(invites.Addrs[0]);
            Assert.IsNotNull(invites.Key);
        }

        [TestMethod]
        public async Task ShouldListDbs()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            string name2 = "name2";
            _ = await client.NewDBAsync(ThreadId.FromRandom(), name2);
            IDictionary<string, GetDBInfoReply> dbList = await client.ListDBsAsync();
            Assert.IsTrue(dbList.Count >= 1, "Expected 1 or more database");
        }

        [TestMethod]
        public async Task ShouldCleanlyDeleteaDatabase()
        {
            PrivateKey user = PrivateKey.FromRandom();

            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId dbId = await client.NewDBAsync(ThreadId.FromRandom());
            int before = (await client.ListDBsAsync()).Count;
            await client.DeleteDBAsync(dbId);
            int after = (await client.ListDBsAsync()).Count;
            Assert.AreEqual(before, after + 1);
        }

        [TestMethod]
        public async Task NewCollectionShouldWorkAndCreateAnEmptyObject()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollection(db, collection);

            IList<Models.CollectionInfo> collections = await client.ListCollection(db);
            Assert.IsTrue(collections.Any(c=> c.Name == "Person"));
        }

        [TestMethod]
        public async Task UpdateCollectionShouldUpdateAnExistingCollection()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            await client.NewCollection(db, new Models.CollectionInfo() { Name = "PersonToUpdate", Schema = JsonSchema.FromText(personSchema) });
            await client.UpdateCollection(db, new Models.CollectionInfo() { Name = "PersonToUpdate", Schema = JsonSchema.FromText(schema2) });

            CollectionInfo updatedCollection = await client.GetCollectionInfo(db, "PersonToUpdate");
            Assert.AreEqual(3,updatedCollection.Schema.Keywords.FirstOrDefault().GetSubschemas().Count());
        }

        [TestMethod]
        public async Task DeleteCollectionShouldDeleteAnExistingcollection()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "CollectionToDelete", Schema = JsonSchema.FromText(personSchema) };

            await client.NewCollection(db, collection);

            IList<CollectionInfo> beforeDeleteCollections = await client.ListCollection(db);
            Assert.IsTrue(beforeDeleteCollections.Any(c => c.Name == "CollectionToDelete"));

            await client.DeleteCollection(db, "CollectionToDelete");

            IList<CollectionInfo> afterDeleteCollections = await client.ListCollection(db);
            Assert.IsFalse(afterDeleteCollections.Any(c => c.Name == "CollectionToDelete"));
        }

        [TestMethod]
        public async Task GetCollectionInfoShouldThrowForAMissingCollection()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());

            await Assert.ThrowsExceptionAsync<RpcException>(() => client.GetCollectionInfo(db, "Fake"));
        }

        [TestMethod]
        public async Task GetCollectionIndexesShouldListValidCollectionIndexes()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            await client.NewCollection(db, new Models.CollectionInfo() { Name = "PersonIndexes", Schema = JsonSchema.FromText(personSchema), Indexes = new List<Grpc.Index>() { new Grpc.Index() { Path = "age" } } });

            IList<Grpc.Index> indexes = await client.GetCollectionIndexes(db, "PersonIndexes");
            Assert.AreEqual(1, indexes.Count);
        }

        [TestMethod]
        public async Task ListDBSShouldListTheCorrectNumberOfDbsWithTheCorrectName()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId threadId1 = ThreadId.FromRandom();
            _ = await client.NewDBAsync(threadId1, "db1");

            ThreadId threadId2 = ThreadId.FromRandom();
            _ = await client.NewDBAsync(threadId2, "db2");


            IDictionary<string, GetDBInfoReply> databases = await client.ListDBsAsync();
            Assert.IsTrue(databases.Count > 1);
            Assert.AreEqual(databases[threadId1.ToString()].Name, "db1");
            Assert.AreEqual(databases[threadId2.ToString()].Name, "db2");
        }

        [TestMethod]
        public async Task CreateResponseShouldContainAnInstancesList()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollection(db, collection);

            IList<string> instances = await client.Create(db, "Person", new[] { CreatePerson });
            Assert.IsTrue(instances.Count >= 1);
        }
    }
}
