using System;
using System.Threading.Tasks;
using Textile.Crypto;
using Textile.Security;
using Textile.Threads.Core;
using Xunit;

namespace Textile.Threads.Client.Tests
{
    public class ThreadClientFactoryTests
    {


        [Fact]
        public void Should_Create_A_New_Factory()
        {
            var factory = ThreadClientFactory.Create();
            Assert.NotNull(factory);
        }

        [Fact]
        public async Task Should_Create_A_New_Client()
        {
            var factory = ThreadClientFactory.Create();
            var client = await factory.CreateClientAsync();
            Assert.NotNull(client);
        }

        
    }
}
