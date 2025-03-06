using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Whitestone.SegnoSharp.Shared.Models.Persistent;

namespace Whitestone.SegnoSharp.Modules.StreamControls.HealthChecks
{
    public class StreamingServerHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;

        public StreamingServerHealthCheck(HttpClient httpClient, StreamingSettings settings)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri($"http://{settings.Hostname}:{settings.Port}");
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