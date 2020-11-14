using System;
using Xunit;

namespace Textile.Crypto.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void ShouldBeAbleToSerializeAndRecoverIdentities()
        {
            PrivateKey id = PrivateKey.FromRandom();
            string str = id.ToString();
            PrivateKey back = PrivateKey.FromString(str);

            Assert.Equal(id.GetHashCode(), back.GetHashCode());
        }
    }
}
