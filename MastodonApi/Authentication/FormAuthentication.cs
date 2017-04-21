using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MastodonApi.Authentication
{
    /// <summary></summary>
    internal class FormAuthentication : IAuthenticator
    {
        internal FormAuthentication(GetFormCredentialDelegate credentialProvider)
        {
            _credentialProvider = credentialProvider;
            return;
        }

        public async Task<string> Connect(string serverName)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            HttpClient client = new HttpClient(clientHandler);
            client.BaseAddress = new Uri(string.Format(ConnectionUrlBasePattern, serverName));
            CookieCollection cookies = null;
            string authenticityToken = null;
            try {
                HttpResponseMessage response = await client.GetAsync(ConnectionUrlPath);
                if (response.IsSuccessStatusCode) {
                    cookies = clientHandler.CookieContainer.GetCookies(client.BaseAddress);
                    HttpResponseHeaders headers = response.Headers;
                    authenticityToken = await RetrieveAuthentivityToken(response);
                }
                NetworkCredential credentials = _credentialProvider();
                if (null == credentials) {
                    throw new ApplicationException();
                }
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("utf8", "%E2%9C%93"));
                postData.Add(new KeyValuePair<string, string>("authenticity_token", authenticityToken));
                postData.Add(new KeyValuePair<string, string>("user%5Bemail%5D",
                    WebUtility.UrlEncode(credentials.UserName)));
                postData.Add(new KeyValuePair<string, string>("user%5Bpassword%5D",
                    WebUtility.UrlEncode(credentials.Password)));
                postData.Add(new KeyValuePair<string, string>("button", ""));
                FormUrlEncodedContent postContent = new FormUrlEncodedContent(postData);
                response = await client.PostAsync(ConnectionUrlPath, postContent);
                if (response.IsSuccessStatusCode) {
                    int i = 1;
                }
                MastodonSession.ConnectionCompletedEvent.Set();
                return authenticityToken;
            }
            catch (Exception e) {
                throw;
            }
            throw new NotImplementedException();
        }

        public void Disconnect(string serverName)
        {
            throw new NotImplementedException();
        }

        private async Task<string> RetrieveAuthentivityToken(HttpResponseMessage from)
        {
            string rawTextContent = await from.Content.ReadAsStringAsync();
            int authenticityTokenPrefixIndex = rawTextContent.IndexOf(AuthenticityTokenRetrievalPrefix);
            if (-1 == authenticityTokenPrefixIndex) {
                throw new ApplicationException();
            }
            int authenticityTokenIndex = authenticityTokenPrefixIndex + AuthenticityTokenRetrievalPrefix.Length;
            int authenticityTokenPostfixIndex = rawTextContent.IndexOf('"', authenticityTokenIndex);
            if (-1 == authenticityTokenPostfixIndex) {
                throw new ApplicationException();
            }
            int authenticityTokenLength = authenticityTokenPostfixIndex - authenticityTokenIndex - 1;
            if (0 >= authenticityTokenLength) {
                throw new ApplicationException();
            }
            return rawTextContent.Substring(authenticityTokenIndex, authenticityTokenLength);
        }

        private const string AuthenticityTokenRetrievalPrefix = "name=\"authenticity_token\" value=\"";
        private const string ConnectionUrlBasePattern = "https://{0}/";
        private const string ConnectionUrlPath = "auth/sign_in";
        private GetFormCredentialDelegate _credentialProvider;
    }
}
