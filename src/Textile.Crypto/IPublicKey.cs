using System;
using System.Threading.Tasks;

namespace Textile.Crypto
{
    public interface IPublicKey
    {
        bool Verify(byte[] data, byte[] signature);

        public byte[] Bytes { get; }

    }
}
