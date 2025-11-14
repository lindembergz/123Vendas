using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Venda.Infrastructure.HealthChecks;

public class SeqHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public SeqHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var enableSeqSink = _configuration.GetValue<bool>("Logging:EnableSeqSink");
        
        if (!enableSeqSink)
        {
            return HealthCheckResult.Healthy("Seq sink não está habilitado");
        }

        var seqServerUrl = _configuration["Seq:ServerUrl"] ?? "http://localhost:5341";

        try
        {
            var response = await _httpClient.GetAsync($"{seqServerUrl}/api", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"Seq está acessível em {seqServerUrl}");
            }

            return HealthCheckResult.Degraded(
                $"Seq respondeu com status {response.StatusCode}",
                data: new Dictionary<string, object>
                {
                    { "url", seqServerUrl },
                    { "statusCode", (int)response.StatusCode }
                });
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Degraded(
                $"Não foi possível conectar ao Seq em {seqServerUrl}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    { "url", seqServerUrl },
                    { "error", ex.Message }
                });
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Degraded(
                $"Timeout ao conectar ao Seq em {seqServerUrl}",
                data: new Dictionary<string, object>
                {
                    { "url", seqServerUrl },
                    { "timeout", "5s" }
                });
        }
    }
}
