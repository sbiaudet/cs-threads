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
        public void ShouldCreateANewFactory()
        {
            IThreadClientFactory factory = ThreadClientFactory.Create();
            Assert.IsNotNull(factory);
        }

        [TestMethod]
        public async Task ShouldCreateANewClient()
        {
            IThreadClientFactory factory = ThreadClientFactory.Create();
            IThreadClient client = await factory.CreateClientAsync();
            Assert.IsNotNull(client);
        }

    }
}
