using System;
using Multiformats.Address;
using Multiformats.Address.Protocols;
using Multiformats.Base;

namespace Textile.Threads.Core.Protocols
{
    public class Thread : MultiaddressProtocol
    {
        static Thread()
        {
            
        }
        public Thread() : base("thread", 406, -1)
        {
           
        }

        public override void Decode(string value) => Value = Multibase.Decode(value, out MultibaseEncoding _);
        public override void Decode(byte[] bytes) => Value = bytes;
        public override byte[] ToBytes() => (byte[])Value; 
        public override string ToString() => Multibase.Encode(MultibaseEncoding.Base32Lower, (byte[])Value);
    }
}
