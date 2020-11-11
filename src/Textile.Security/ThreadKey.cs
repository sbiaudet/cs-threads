using System;
using System.Linq;
using System.Security.Cryptography;
using Multiformats.Base;

namespace Textile.Security
{
    public class ThreadKey
    {
        // KeyBytes is the length of GCM key.
        private const int KeyBytes = 32;

        private readonly byte[] _serviceKey;
        private readonly byte[] _readKey;

        public ThreadKey(byte[] serviceKey, byte[] readKey)
        {
            this._serviceKey = serviceKey;
            this._readKey = readKey;
        }

        public bool IsDefined => this._serviceKey != null;
        public bool CanRead => this._readKey != null;

        public byte[] Bytes {
            get
            {
                var bytes = this._serviceKey;

                if (CanRead)
                {
                    bytes = bytes.Concat(this._readKey).ToArray();
                }
                return bytes;
            }
        }

        public override string ToString()
        {
            return Multibase.Encode(MultibaseEncoding.Base32Lower, this.Bytes).ToString();
        }

        public static ThreadKey FromRandom(bool withRead = true)
        {
            using var rngCsp = RandomNumberGenerator.Create();

            var randomService = new byte[KeyBytes];
            rngCsp.GetBytes(randomService);

            var randomRead = new byte[KeyBytes];
            if (withRead)
            {
                rngCsp.GetBytes(randomRead);
            }

            return new ThreadKey(randomService, withRead ? randomRead : null);
        }

        public static ThreadKey FromBytes(byte[] bytes)
        {
            if(bytes.Length != KeyBytes && bytes.Length != KeyBytes * 2)
            {
                throw new Exception("Invalid Key");
            }

            var copy = bytes.AsSpan();
            var serviceKey = copy.Slice(0, KeyBytes).ToArray();
            var readKey = bytes.Length == KeyBytes * 2 ? copy[KeyBytes..].ToArray() : default;

            return new ThreadKey(serviceKey, readKey);
        }

        public static ThreadKey FromString(string s)
        {
            var data = Multibase.Decode(s, out MultibaseEncoding _);
            return ThreadKey.FromBytes(data);
        }
    }
}
