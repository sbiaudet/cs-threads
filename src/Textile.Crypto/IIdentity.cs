using System;
using System.Threading.Tasks;

namespace Textile.Crypto
{
    public interface IIdentity
    {
        byte[] Sign(byte[] data);
        public IPublicKey PublicKey { get; }
        public byte[] Bytes { get; }
    }

    public interface IPrivate : IIdentity
    {

    }
}
