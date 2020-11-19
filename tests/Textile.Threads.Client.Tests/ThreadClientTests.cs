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
            await client.NewCollectionAsync(db, collection);

            IList<Models.CollectionInfo> collections = await client.ListCollectionAsync(db);
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
            await client.NewCollectionAsync(db, new Models.CollectionInfo() { Name = "PersonToUpdate", Schema = JsonSchema.FromText(personSchema) });
            await client.UpdateCollectionAsync(db, new Models.CollectionInfo() { Name = "PersonToUpdate", Schema = JsonSchema.FromText(schema2) });

            CollectionInfo updatedCollection = await client.GetCollectionInfoAsync(db, "PersonToUpdate");
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

            await client.NewCollectionAsync(db, collection);

            IList<CollectionInfo> beforeDeleteCollections = await client.ListCollectionAsync(db);
            Assert.IsTrue(beforeDeleteCollections.Any(c => c.Name == "CollectionToDelete"));

            await client.DeleteCollectionAsync(db, "CollectionToDelete");

            IList<CollectionInfo> afterDeleteCollections = await client.ListCollectionAsync(db);
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

            await Assert.ThrowsExceptionAsync<RpcException>(() => client.GetCollectionInfoAsync(db, "Fake"));
        }

        [TestMethod]
        public async Task GetCollectionIndexesShouldListValidCollectionIndexes()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            await client.NewCollectionAsync(db, new Models.CollectionInfo() { Name = "PersonIndexes", Schema = JsonSchema.FromText(personSchema), Indexes = new List<Grpc.Index>() { new Grpc.Index() { Path = "age" } } });

            IList<Grpc.Index> indexes = await client.GetCollectionIndexesAsync(db, "PersonIndexes");
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
        public async Task Save_Response_Should_Contain_An_Instances_List()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollectionAsync(db, collection);

            Person person = CreatePerson;

            IList<string> instances = await client.CreateAsync(db, "Person", new[] { person });
            Assert.IsTrue(instances.Count >= 1);

            person.Id = instances[0];
            person.Age = 30;

            await client.SaveAsync(db, "Person", new[] { person });
        }

        [TestMethod]
        public async Task Delete_Should_Delete_An_Existing_Instance()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollectionAsync(db, collection);

            Person person = CreatePerson;

            IList<string> instances = await client.CreateAsync(db, "Person", new[] { person });
            Assert.IsTrue(instances.Count >= 1);

            person.Id = instances[0];

            await client.DeleteAsync(db, "Person", new[] { person.Id });

            bool nonExists = await client.Has(db, "Person", new[] { person.Id });
            Assert.IsFalse(nonExists);
        }

        [TestMethod]
        public async Task Has_Should_Return_True_With_Existing_Instances_And_False_With_Fake_Id()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollectionAsync(db, collection);

            Person person = CreatePerson;

            IList<string> instances = await client.CreateAsync(db, "Person", new[] { person });
            Assert.IsTrue(instances.Count >= 1);

            person.Id = instances[0];

            bool exists = await client.Has(db, "Person", new[] { person.Id });
            Assert.IsTrue(exists);

            bool nonExists = await client.Has(db, "Person", new[] { "FakeId" });
            Assert.IsFalse(nonExists);
        }


        [TestMethod]
        public async Task Find_Should_Return_Instance_From_Query()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollectionAsync(db, collection);

            Person person = CreatePerson;

            IList<string> instances = await client.CreateAsync(db, "Person", new[] { person });
            Assert.IsTrue(instances.Count >= 1);

            person.Id = instances[0];

            Query query = Query.Where("lastName").Eq(person.LastName);

            IList<Person> results = await client.FindAsync<Person>(db, "Person", query);
            Assert.IsTrue(results.Count >= 1);
            Assert.AreEqual(person.Id, results.FirstOrDefault().Id);
        }

        [TestMethod]
        public async Task FindById_Should_Return_Instance_From()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollectionAsync(db, collection);

            Person person = CreatePerson;

            IList<string> instances = await client.CreateAsync(db, "Person", new[] { person });
            Assert.IsTrue(instances.Count >= 1);

            person.Id = instances[0];

            Person resultPerson = await client.FindByIdAsync<Person>(db, "Person", person.Id);
            Assert.AreEqual(person.Id, resultPerson.Id);
        }


        [TestMethod]
        public async Task ReadTransaction_Should_Work()
        {
            PrivateKey user = PrivateKey.FromRandom();
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            _ = await client.GetTokenAsync(user);
            ThreadId db = await client.NewDBAsync(ThreadId.FromRandom());
            CollectionInfo collection = new() { Name = "Person", Schema = JsonSchema.FromText(personSchema) };
            await client.NewCollectionAsync(db, collection);

            Person person = CreatePerson;

            IList<string> instances = await client.CreateAsync(db, "Person", new[] { person });
            Assert.IsTrue(instances.Count >= 1);

            person.Id = instances[0];

            ReadTransaction readTransaction = client.ReadTransaction(db, "Person");

            await readTransaction.StartAsync();

            bool exists = await readTransaction.HasAsync(new[] { person.Id });
            Assert.IsTrue(exists);

            Person foundPerson = await readTransaction.FindByIdAsync<Person>(person.Id);
            Assert.AreEqual(person.Id, foundPerson.Id);
            Assert.AreEqual(person.FirstName, foundPerson.FirstName);
            Assert.AreEqual(person.LastName, foundPerson.LastName);
            Assert.AreEqual(person.Age, foundPerson.Age);

            Query query = Query.Where("lastName").Eq(person.LastName);
            IList<Person> people = await readTransaction.FindAsync<Person>(query);
            Assert.IsTrue(people.Count >= 1);
        }
    }
}
