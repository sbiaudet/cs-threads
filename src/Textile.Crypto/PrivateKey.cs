using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Multiformats.Base;
using LibP2P.Crypto;

namespace Textile.Crypto
{
    public class Private : IPrivate
    {

        private readonly PrivateKey _privateKey;

        public Private(byte[] secretKey) : this(new Ed25519PrivateKey(secretKey))
        {

        }

        internal Private(PrivateKey privateKey)
        {
            this._privateKey = privateKey;
        }

        public IPublic Public
        {
            get
            {
                var publicKey = _privateKey.GetPublic();
                return new Public(publicKey);
            }
        }

        public byte[] Bytes
        {
            get
            {
                return _privateKey.Bytes;
            }
        }

        public byte[] Sign(byte[] data)
            => _privateKey.Sign(data);

        public static Private FromRawSeed(byte[] rawSeed)
         => new Private(rawSeed);

        public static Private FromRandom()
        {
            var privateKey = KeyPair.Generate(KeyType.Ed25519).PrivateKey;
            return new Private(privateKey);
        }

        public static Private FromString(string str)
        {
            var decoded = Multibase.Decode(str, out string _);
            return new Private(PrivateKey.Unmarshal(decoded));
        }

        public override string ToString()
        {
            return Multibase.Encode(MultibaseEncoding.Base32Lower, this.Bytes);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}