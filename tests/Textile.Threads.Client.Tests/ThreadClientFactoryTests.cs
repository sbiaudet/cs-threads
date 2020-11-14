using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Textile.Crypto;
using Textile.Security;
using Textile.Threads.Core;

namespace Textile.Threads.Client.Tests
{
    [TestClass]
    public class ThreadClientFactoryTests
    {


        [TestMethod]
        public void Should_Create_A_New_Factory()
        {
            var factory = ThreadClientFactory.Create();
            Assert.IsNotNull(factory);
        }

        [TestMethod]
        public async Task Should_Create_A_New_Client()
        {
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            Assert.IsNotNull(client);
        }

        
    }
}
