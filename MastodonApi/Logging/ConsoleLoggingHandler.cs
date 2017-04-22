using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MastodonApi.Logging
{
    public class ConsoleLoggingHandler : DelegatingHandler
    {
        public ConsoleLoggingHandler(HttpClientHandler innerHandler)
            : base(innerHandler)
        {
            return;
        }

        public bool LoggingEnabled { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (LoggingEnabled) {
                Console.WriteLine("Request:");
                Console.WriteLine(request.ToString());
                if (null != request.Content) {
                    Console.WriteLine(await request.Content.ReadAsStringAsync());
                }
                Console.WriteLine();
            }
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            if (LoggingEnabled) {
                Console.WriteLine("Response:");
                Console.WriteLine(response.ToString());
                if (null != response.Content) {
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }
                Console.WriteLine();
            }
            return response;
        }
    }
}
