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
using Textile.Threads.Core;

namespace Textile.Threads.Client.Tests
{
    [TestClass]
    public class ThreadClientTests
    {

        private const string personSchema = "{ \"$id\": \"https://example.com/person.schema.json\", \"$schema\": \"http://json-schema.org/draft-07/schema#\", \"title\": \"Person\", \"type\": \"object\", \"required\": [\"_id\"], \"properties\": { \"_id\": { \"type\": \"string\", \"description\": \"The instance's id.\" }, \"firstName\": { \"type\": \"string\", \"description\": \"The person's first name.\" }, \"lastName\": { \"type\": \"string\", \"description\": \"The person's last name.\" }, \"age\": { \"description\": \"Age in years which must be equal to or greater than zero.\", \"type\": \"integer\", \"minimum\": 0 } } }";

        private const string schema2 = "{ \"properties\": { \"_id\": { \"type\": \"string\" }, \"fullName\": { \"type\": \"string\" }, \"age\": { \"type\": \"integer\", \"minimum\": 0 } } }";


        private Person CreatePerson => new Person()
        {
            Id = "",
            FirstName = "Adam",
            LastName = "Doe",
            Age = 21
        };

        [TestMethod]
        public async Task Should_Get_A_New_Token()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            var token = await client.GetTokenAsync(user);
            Assert.IsNotNull(token);
        }
       
        [TestMethod]
        public async Task Should_Create_A_New_Db()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            _ = await client.NewDBAsync(ThreadId.FromRandom(), "test");
        }

        [TestMethod]
        public async Task Should_Get_A_Valid_Db_Info()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var dbId = await client.NewDBAsync(ThreadId.FromRandom());

            var invites = await client.GetDbInfoAsync(dbId);
            Assert.IsNotNull(invites);
            Assert.IsNotNull(invites.Addrs[0]);
            Assert.IsNotNull(invites.Key);
        }

        [TestMethod]
        public async Task Should_List_Dbs()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            var token = await client.GetTokenAsync(user);
            var name2 = "name2";
            var db = await client.NewDBAsync(ThreadId.FromRandom(), name2);
            var dbList = await client.ListDBsAsync();
            Assert.IsTrue(dbList.Count >= 1, "Expected 1 or more database");
        }

        [TestMethod]
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
            Assert.AreEqual(before, after + 1);
        }

        [TestMethod]
        public async Task NewCollection_should_work_and_create_an_empty_object()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom());
            var collection = new Models.CollectionInfo() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollection(db, collection);

            var collections = await client.ListCollection(db);
            Assert.IsTrue(collections.Any(c=> c.Name == "Person"));
        }

        [TestMethod]
        public async Task UpdateCollection_should_update_an_existing_collection()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom());
            await client.NewCollection(db, new Models.CollectionInfo() { Name = "PersonToUpdate", Schema = JsonSchema.FromText(personSchema) });
            await client.UpdateCollection(db, new Models.CollectionInfo() { Name = "PersonToUpdate", Schema = JsonSchema.FromText(schema2) });

            var updatedCollection = await client.GetCollectionInfo(db, "PersonToUpdate");
            Assert.AreEqual(3,updatedCollection.Schema.Keywords.FirstOrDefault().GetSubschemas().Count());
        }

        [TestMethod]
        public async Task DeleteCollection_should_delete_an_existing_collection()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom());
            var collection = new Models.CollectionInfo() { Name = "CollectionToDelete", Schema = JsonSchema.FromText(personSchema) };

            await client.NewCollection(db, collection);

            var beforeDeleteCollections = await client.ListCollection(db);
            Assert.IsTrue(beforeDeleteCollections.Any(c => c.Name == "CollectionToDelete"));

            await client.DeleteCollection(db, "CollectionToDelete");

            var afterDeleteCollections = await client.ListCollection(db);
            Assert.IsFalse(afterDeleteCollections.Any(c => c.Name == "CollectionToDelete"));
        }

        [TestMethod]
        public async Task GetCollectionInfo_should_throw_for_a_missing_collection()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom());

            await Assert.ThrowsExceptionAsync<RpcException>(() => client.GetCollectionInfo(db, "Fake"));
        }

        [TestMethod]
        public async Task GetCollectionIndexes_should_list_valid_collection_indexes()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom());
            await client.NewCollection(db, new Models.CollectionInfo() { Name = "PersonIndexes", Schema = JsonSchema.FromText(personSchema), Indexes = new List<Grpc.Index>() { new Grpc.Index() { Path = "age" } } });

            var indexes = await client.GetCollectionIndexes(db, "PersonIndexes");
            Assert.AreEqual(1, indexes.Count);
        }

        [TestMethod]
        public async Task ListDBS_should_list_the_correct_number_of_dbs_with_the_correct_name()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var threadId1 = ThreadId.FromRandom();
            var db1 = await client.NewDBAsync(threadId1, "db1");

            var threadId2 = ThreadId.FromRandom();
            var db2 = await client.NewDBAsync(threadId2, "db2");


            var databases = await client.ListDBsAsync();
            Assert.IsTrue(databases.Count > 1);
            Assert.AreEqual(databases[threadId1.ToString()].Name, "db1");
            Assert.AreEqual(databases[threadId2.ToString()].Name, "db2");
        }
        
        [TestMethod]
        public async Task Create_response_should_contain_a_JSON_parsable_instancesList()
        {
            var user = Private.FromRandom();
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            var db = await client.NewDBAsync(ThreadId.FromRandom());
            var collection = new Models.CollectionInfo() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollection(db, collection);

            var instances = await client.Create(db, "Person", new[] { CreatePerson });
            Assert.IsTrue(instances.Count >= 1);
        }
    }
}
