using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Multiformats.Base;
using LibP2P.Crypto;

namespace Textile.Crypto
{
    public class PrivateKey : IPrivate
    {

        private readonly LibP2P.Crypto.PrivateKey _privateKey;

        public PrivateKey(byte[] secretKey) : this(new Ed25519PrivateKey(secretKey))
        {

        }

        internal PrivateKey(LibP2P.Crypto.PrivateKey privateKey)
        {
            this._privateKey = privateKey;
        }

        public IPublicKey PublicKey
        {
            get
            {
                LibP2P.Crypto.PublicKey publicKey = _privateKey.GetPublic();
                return new PublicKey(publicKey);
            }
        }

        public byte[] Bytes => _privateKey.Bytes;

        public byte[] Sign(byte[] data)
        {
            return _privateKey.Sign(data);
        }

        public static PrivateKey FromRawSeed(byte[] rawSeed)
        {
            return new(rawSeed);
        }

        public static PrivateKey FromRandom()
        {
            LibP2P.Crypto.PrivateKey privateKey = KeyPair.Generate(KeyType.Ed25519).PrivateKey;
            return new PrivateKey(privateKey);
        }

        public static PrivateKey FromString(string str)
        {
            byte[] decoded = Multibase.Decode(str, out string _);
            return new PrivateKey(LibP2P.Crypto.PrivateKey.Unmarshal(decoded));
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