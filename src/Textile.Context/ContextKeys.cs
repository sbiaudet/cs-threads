using System;
namespace Textile.Context
{
    public static class ContextKeys
    {
        public const string ThreadNameKey = "x-textile-thread-name";
        public const string ThreadIdKey = "x-textile-thread";
        public const string SessionKey = "x-textile-session";
        public const string OrganizationKey = "x-textile-org";
        public const string ApiKey = "x-textile-api-key";
        public const string ApiSignatureKey = "x-textile-api-sig";
        public const string ApiSignatureRawMessageKey = "x-textile-api-sig-msg";
        public const string AuthorizationKey = "authorization";
    }
}
