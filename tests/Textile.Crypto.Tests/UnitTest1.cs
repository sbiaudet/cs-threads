using System;
using Xunit;

namespace Textile.Crypto.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Should_Be_Able_To_Serialize_And_Recover_Identities()
        {
            var id = Private.FromRandom();
            var str = id.ToString();
            var back = Private.FromString(str);

            Assert.Equal(id.GetHashCode(), back.GetHashCode());
        }
    }
}
