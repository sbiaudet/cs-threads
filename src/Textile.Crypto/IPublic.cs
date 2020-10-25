using System;
using System.Threading.Tasks;

namespace Textile.Crypto
{
    public interface IPublic
    {
        bool Verify(byte[] data, byte[] signature);

        public byte[] Bytes { get; }

    }
}
