using System;
using System.Threading.Tasks;

namespace Textile.Crypto
{
    public interface IIdentity
    {
        byte[] Sign(byte[] data);
        public IPublic Public { get; }
        public byte[] Bytes { get; }
    }

    public interface IPrivate : IIdentity
    {

    }
}
