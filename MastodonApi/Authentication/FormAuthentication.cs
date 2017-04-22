using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using MastodonApi.Logging;

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
            ConsoleLoggingHandler logger = new ConsoleLoggingHandler(clientHandler);
            HttpClient client = new HttpClient(logger);
            client.BaseAddress = new Uri(string.Format(ConnectionUrlBasePattern, serverName));
            CookieCollection cookies = null;
            string authenticityToken = null;
            string mastodonSession = null;
            try {
                HttpResponseMessage response = await client.GetAsync(ConnectionUrlPath);
                if (response.IsSuccessStatusCode) {
                    cookies = clientHandler.CookieContainer.GetCookies(client.BaseAddress);
                    mastodonSession= cookies["_mastodon_session"].Value;
                    HttpResponseHeaders headers = response.Headers;
                    authenticityToken = await RetrieveAuthentivityToken(response);
                }
                // cookies.Add(new Cookie("_mastodon_session", authenticityToken));
                NetworkCredential credentials = _credentialProvider();
                if (null == credentials) {
                    throw new ApplicationException();
                }
                List<KeyValuePair<string, string>> postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("utf8", "✓"));
                postData.Add(new KeyValuePair<string, string>("authenticity_token", authenticityToken));
                postData.Add(new KeyValuePair<string, string>("user[email]",
                    WebUtility.UrlEncode(credentials.UserName)));
                postData.Add(new KeyValuePair<string, string>("user[password]",
                    WebUtility.UrlEncode(credentials.Password)));
                postData.Add(new KeyValuePair<string, string>("button", ""));
                FormUrlEncodedContent postContent = new FormUrlEncodedContent(postData);
                logger.LoggingEnabled = true;
                HttpRequestHeaders postHeaders = client.DefaultRequestHeaders;
                postHeaders.Accept.Clear();
                postHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                postHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
                postHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
                postHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
                postHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*",0.8));
                postHeaders.AcceptEncoding.Clear();
                //postHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                //postHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                //postHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                postHeaders.AcceptLanguage.Clear();
                postHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr-FR"));
                postHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr", 0.8));
                postHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.6));
                postHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.4));
                postHeaders.CacheControl = new CacheControlHeaderValue() { MaxAge = new TimeSpan() };
                postHeaders.Connection.Clear();
                postHeaders.Connection.Add("keep-alive");
                //Cookie:__cfduid=d59eff9cf3648d1c2347c5f94ae7f1a651492778490; _mastodon_session=Mi9JVjVBRnhUZTRiRW1iM1VQczVycFVOL2Y5MzNpL3FmSm92blRiSWJScy94QmdlR1JzMHVvN0p5NTZUcmRURzVFc1R1QTNWY3lldW5hRVU1NW9Ca3U1R2VDMngvUUlyYkQ3Y2dQRWdFRjlQL1drV2g2aG5CZDVvRjJCc2xnZk5IQmg1STdYNUNuZGgxOTRMTjVMeUxjYzQzbVBGK0NHOXBEZWRnSmdpL2g5VUpIYTlJMHJ5eDM0N3psZU12dXBaLS1aVFAvalV6UE5mdE16WUZ2UldpU29nPT0%3D--f5ce9c8122b54e0633705d143856fa1f4ddc227c
                //Host:mastodon.social
                //Origin:https://mastodon.social
                //Referer:https://mastodon.social/auth/sign_in
                postHeaders.Add("Upgrade-Insecure-Requests", "1");
                //User-Agent:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36
                // TODO : postHeaders.Referrer
                postHeaders.Add("Cookie", string.Format("_mastodon_session={0}", mastodonSession));
                response = await client.PostAsync(ConnectionUrlPath, postContent);
                string rawTextContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(rawTextContent);
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
