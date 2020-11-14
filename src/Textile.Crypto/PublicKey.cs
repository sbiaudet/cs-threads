using System;
using System.IO;
using System.Threading.Tasks;
using Multiformats.Base;
using System.Linq;
using LibP2P.Crypto;

namespace Textile.Crypto
{
    public class PublicKey : IPublicKey
    {
        private readonly LibP2P.Crypto.PublicKey _publicKey;

        public PublicKey(byte[] pubKey) : this(new Ed25519PublicKey(pubKey))
        {
        }

        public PublicKey(LibP2P.Crypto.PublicKey publicKey)
        {
            _publicKey = publicKey;
        }

        public byte[] Bytes => _publicKey.Bytes;

        public bool Verify(byte[] data, byte[] signature)
        {
            return _publicKey.Verify(data, signature);
        }

        public override string ToString()
        {
            return Multibase.Encode(MultibaseEncoding.Base32Lower, this.Bytes);
        }

        public static PublicKey FromString(string str)
        {
            byte[] decoded = Multibase.Decode(str, out string _);
            return new PublicKey(LibP2P.Crypto.PublicKey.Unmarshal(decoded));
        }
    }
}
