using System;
using Textile.Threads.Core;
using Xunit;

namespace Textile.Core.Tests
{
    public class ThreadIdTests
    {
        [Fact]
        public void Should_Be_Able_To_Create_A_Random_ID()
        {
            var i = ThreadId.FromRandom(Variant.Raw, 16);
            Assert.NotNull(i);
            Assert.Equal(18, i.Bytes.Length);
            Assert.True(i.IsDefined);
        }

        [Fact]
        public void Should_be_able_to_round_trip_to_and_from_bytes()
        {
            var i = ThreadId.FromRandom(Variant.Raw, 16);
            var b = i.Bytes;
            var n = ThreadId.FromBytes(b);
            Assert.Equal(n, i);
        }
    }
}
