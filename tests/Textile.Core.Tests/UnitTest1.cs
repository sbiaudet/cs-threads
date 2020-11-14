using System;
using Textile.Threads.Core;
using Xunit;

namespace Textile.Core.Tests
{
    public class ThreadIdTests
    {
        [Fact]
        public void ShouldBeAbleToCreateARandomID()
        {
            ThreadId i = ThreadId.FromRandom(Variant.Raw, 16);
            Assert.NotNull(i);
            Assert.Equal(18, i.Bytes.Length);
            Assert.True(i.IsDefined);
        }

        [Fact]
        public void ShouldBeAbleToRoundTripToAndFromBytes()
        {
            ThreadId i = ThreadId.FromRandom(Variant.Raw, 16);
            byte[] b = i.Bytes;
            ThreadId n = ThreadId.FromBytes(b);
            Assert.Equal(n, i);
        }
    }
}
