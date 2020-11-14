using System;
using Multiformats.Base;
using BinaryEncoding;
using System.Linq;
using System.Security.Cryptography;

namespace Textile.Threads.Core
{
    public class ThreadId : IEquatable<ThreadId>
    {
        private static readonly ulong V1 = 0x01;

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
                    throw new InvalidOperationException($"expected 1 as the id version number, got: ${ value }.");
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
                    throw new InvalidOperationException($"invalid variant.");
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
                    throw new InvalidOperationException($"random component too small.");
                }
                _randomBytes = value;
            }
        }

        public byte[] Bytes => Binary.Varint.GetBytes(this.Version)
                   .Concat(Binary.Varint.GetBytes((ulong)this.Variant)).ToArray()
                   .Concat(this.RandomBytes).ToArray();

        public bool IsDefined => this.Bytes.Length > 0;

        public static byte[] GenerateRandomBytes(int size = 32)
        {
            byte[] randomBytes = new byte[size];
            using RandomNumberGenerator rngCsp = RandomNumberGenerator.Create();
            rngCsp.GetBytes(randomBytes);
            return randomBytes;
        }

        public static ThreadId FromRandom(Variant variant = Variant.Raw, int size = 32)
        {
            return new ThreadId(ThreadId.V1, variant, ThreadId.GenerateRandomBytes(size));
        }

        public static ThreadId FromString(string encodedId)
        {
            byte[] data = Multibase.Decode(encodedId, out MultibaseEncoding _);
            return ThreadId.FromBytes(data);
        }

        public static ThreadId FromBytes(byte[] bytes)
        {
            Span<byte> copy = bytes.AsSpan();
            (int Size, ulong Version) decodeVersion = ThreadId.DecodeVersion(copy);
            copy = copy[decodeVersion.Size..];
            (int Size, Variant Variant) decodeVariant = ThreadId.DecodeVariant(copy);

            byte[] randomBytes = copy[decodeVariant.Size..].ToArray();

            return new ThreadId(decodeVersion.Version, decodeVariant.Variant, randomBytes);
        }

        public static (int Size, ulong Version) DecodeVersion(Span<byte> bytes)
        {
            int size = Binary.Varint.Read(bytes, out ulong version);
            return version != 1 ? throw new InvalidOperationException($"expected 1 as the id version number, got: { version }.") : (size, version);
        }


        public static (int Size, Variant Variant) DecodeVariant(Span<byte> bytes)
        {
            int variantSize = Binary.Varint.Read(bytes,  out ulong variant);

            return (variantSize, (Variant)variant);
        }

        public static string GetEncoding(string encodedID)
        {
            _ = Multibase.Decode(encodedID, out string encoding);
            return encoding;
        }

        public override bool Equals(object obj)
        {
            return obj != null && this.GetType().Equals(obj.GetType()) && this.Equals((ThreadId)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Version, Variant, RandomBytes);
        }

        public override string ToString()
        {
            return ToString(MultibaseEncoding.Base32Lower);
        }

        public string ToString(MultibaseEncoding encoding)
        {
            return Multibase.Encode(encoding, this.Bytes);
        }

        public bool Equals(ThreadId other)
        {
            return Enumerable.SequenceEqual(this.Bytes, other.Bytes);
        }
    }
}
