using System;
using Multiformats.Base;
using BinaryEncoding;
using System.Linq;
using System.Security.Cryptography;

namespace Textile.Threads.Core
{
    public class ThreadId : IEquatable<ThreadId>
    {
        public static ulong V1 = 0x01;
        public static Variant DefaultVariant = default;

        private ulong _version;
        private Variant _variant;
        private byte[] _randomBytes;

        public ThreadId(ulong version, Variant variant, byte[] randomBytes)
        {
            Version = version;
            Variant = variant;
            RandomBytes = randomBytes;
        }

        public ulong Version
        {
            get => _version;
            set
            {
                if (value != 1)
                {
                    throw new Exception($"expected 1 as the id version number, got: ${ value }.");
                }
                _version = value;
            }
        }

        public Variant Variant
        {
            get => _variant;
            set
            {
                if (!(value.HasFlag(Variant.Raw) || value.HasFlag(Variant.AccessControlled)))
                {
                    throw new Exception($"invalid variant.");
                }
                _variant = value;
            }
        }

        internal byte[] RandomBytes
        {
            get => _randomBytes;
            set
            {
                if (value.Length < 16)
                {
                    throw new Exception($"random component too small.");
                }
                _randomBytes = value;
            }
        }

        public byte[] Bytes
        {
            get => Binary.Varint.GetBytes(this.Version)
                   .Concat(Binary.Varint.GetBytes((ulong)this.Variant)).ToArray()
                   .Concat(this.RandomBytes).ToArray();
        }
        public bool IsDefined => this.Bytes.Length > 0;

        public static byte[] GenerateRandomBytes(int size = 32)
        {
            var randomBytes = new byte[size];
            using var rngCsp = RandomNumberGenerator.Create();
            rngCsp.GetBytes(randomBytes);
            return randomBytes;
        }

        public static ThreadId FromRandom(Variant variant = Variant.Raw, int size = 32)
        {
            return new ThreadId(ThreadId.V1, variant, ThreadId.GenerateRandomBytes(size));
        }

        public static ThreadId FromString(string encodedId)
        {
            var data = Multibase.Decode(encodedId, out MultibaseEncoding _);
            return ThreadId.FromBytes(data);
        }

        public static ThreadId FromBytes(byte[] bytes)
        {
            var copy = bytes.AsSpan();
            var decodeVersion = ThreadId.DecodeVersion(copy);
            copy = copy[decodeVersion.Size..];
            var decodeVariant = ThreadId.DecodeVariant(copy);

            var randomBytes = copy[decodeVariant.Size..].ToArray();

            return new ThreadId(decodeVersion.Version, decodeVariant.Variant, randomBytes);
        }

        public static (int Size, ulong Version) DecodeVersion(Span<byte> bytes)
        {
            var size = Binary.Varint.Read(bytes, out ulong version);
            if (version != 1)
            {
                throw new Exception($"expected 1 as the id version number, got: { version }.");
            }

            return (size, version);
        }


        public static (int Size, Variant Variant) DecodeVariant(Span<byte> bytes)
        {
            var variantSize = Binary.Varint.Read(bytes,  out ulong variant);

            return (variantSize, (Variant)variant);
        }

        public string GetEncoding(string encodedID)
        {
            Multibase.Decode(encodedID, out string encoding);
            return encoding;
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return this.Equals((ThreadId)obj);
        }

        public override int GetHashCode() => (this.Version, this.Variant, this.RandomBytes).GetHashCode();

        public override string ToString()
        {
            return ToString(MultibaseEncoding.Base32Lower);
        }

        public string ToString(MultibaseEncoding encoding)
        {
            return Multibase.Encode(encoding, this.Bytes);
        }

        public bool Equals(ThreadId other)
            => Enumerable.SequenceEqual(this.Bytes, other.Bytes);
        
    }
}
