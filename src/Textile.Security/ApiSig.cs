using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Multiformats.Base;

namespace Textile.Security
{
    public class ApiSig
    {

        public string Sig { get; set; }

        public string Msg { get; set; }

        public static ApiSig CreateApiSig(string secret, DateTime? date = null)
        {
            if (date is null)
            {
                date = DateTime.UtcNow.AddMinutes(30);
            }

            var sec =  Multibase.Decode(secret, out string _);
            var msg = date.Value.ToString("o", CultureInfo.InvariantCulture);
            using var hash = new HMACSHA256(sec);
            var mac = hash.ComputeHash(Encoding.ASCII.GetBytes(msg));
            var sig = Multibase.Encode(MultibaseEncoding.Base32Lower, mac);

            return new ApiSig()
            {
                Msg = msg,
                Sig = sig
            };

        }

    }
}
