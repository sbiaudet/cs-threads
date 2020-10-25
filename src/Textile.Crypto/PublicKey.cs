using System;
using System.IO;
using System.Threading.Tasks;
using Multiformats.Base;
using System.Linq;
using LibP2P.Crypto;

namespace Textile.Crypto
{
    public class Public : IPublic
    {
        private readonly PublicKey _publicKey;

        public Public(byte[] pubKey) : this(new Ed25519PublicKey(pubKey))
        {
        }

        public Public(PublicKey publicKey)
        {
            _publicKey = publicKey;
        }

        public byte[] Bytes
        {
            get
            {
                return _publicKey.Bytes;
            }
        }

        public bool Verify(byte[] data, byte[] signature)
        {
            return _publicKey.Verify(data, signature);
        }

        public override string ToString()
        {
            return Multibase.Encode(MultibaseEncoding.Base32Lower, this.Bytes);
        }

        public static Public FromString(string str)
        {
            var decoded = Multibase.Decode(str, out string _);
            return new Public(PublicKey.Unmarshal(decoded));
        }
    }
}
