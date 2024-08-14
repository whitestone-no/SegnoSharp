using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StreamingServer = Whitestone.SegnoSharp.Common.Models.Configuration.StreamingServer;

namespace Whitestone.SegnoSharp.Modules.BassService.HealthChecks
{
    public class StreamingServerHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;

        public StreamingServerHealthCheck(IOptions<StreamingServer> streamingServerConfig, HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri($"http://{streamingServerConfig.Value.Address}:{streamingServerConfig.Value.Port}");
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            HealthCheckResult result = HealthCheckResult.Healthy();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "status-json.xsl");
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    result = HealthCheckResult.Unhealthy();
                }
            }
            catch
            {
                result = HealthCheckResult.Unhealthy();
            }

            return result;
        }
    }
}