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
            _serviceKey = serviceKey;
            _readKey = readKey;
        }

        public bool IsDefined => _serviceKey != null;
        public bool CanRead => _readKey != null;

        public byte[] Bytes {
            get
            {
                byte[] bytes = _serviceKey;

                if (CanRead)
                {
                    bytes = bytes.Concat(_readKey).ToArray();
                }
                return bytes;
            }
        }

        public override string ToString()
        {
            return Multibase.Encode(MultibaseEncoding.Base32Lower, Bytes).ToString();
        }

        public static ThreadKey FromRandom(bool withRead = true)
        {
            using RandomNumberGenerator rngCsp = RandomNumberGenerator.Create();

            byte[] randomService = new byte[KeyBytes];
            rngCsp.GetBytes(randomService);

            byte[] randomRead = new byte[KeyBytes];
            if (withRead)
            {
                rngCsp.GetBytes(randomRead);
            }

            return new ThreadKey(randomService, withRead ? randomRead : null);
        }

        public static ThreadKey FromBytes(byte[] bytes)
        {
            if (bytes.Length is not KeyBytes and not KeyBytes * 2)
            {
                throw new InvalidOperationException("Invalid Key");
            }

            Span<byte> copy = bytes.AsSpan();
            byte[] serviceKey = copy.Slice(0, KeyBytes).ToArray();
            byte[] readKey = bytes.Length == KeyBytes * 2 ? copy[KeyBytes..].ToArray() : default;

            return new ThreadKey(serviceKey, readKey);
        }

        public static ThreadKey FromString(string s)
        {
            byte[] data = Multibase.Decode(s, out MultibaseEncoding _);
            return FromBytes(data);
        }
    }
}
