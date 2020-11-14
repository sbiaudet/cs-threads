using System;

namespace Textile.Security
{
    public class UserAuth
    {
        public string Key { get; set; }

        public string Sig { get; set; }

        public string Msg { get; set; }

        public string Token { get; set; }

        public static UserAuth CreateUserAuth(string key,  string secret, DateTime? date, string token)
        {
            ApiSig partial = ApiSig.CreateApiSig(secret, date);

            return new UserAuth()
            {
                Key = key,
                Sig = partial.Sig,
                Msg = partial.Msg,
                Token = token
            };
        }
    }
}
