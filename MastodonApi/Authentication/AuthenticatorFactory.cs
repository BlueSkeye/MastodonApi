using System;

namespace MastodonApi.Authentication
{
    /// <summary>Let retrieve one of the defined authenticator providers.</summary>
    public static class AuthenticatorFactory
    {
        /// <summary>Get a form based authenticator provider.</summary>
        /// <param name="credentialProvider"></param>
        /// <returns></returns>
        public static IAuthenticator GetFormAuthenticator(GetFormCredentialDelegate credentialProvider)
        {
            if (null == credentialProvider) { throw new ArgumentNullException("credentialProvider"); }
            return new FormAuthentication(credentialProvider);
        }
    }
}
