using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Serilog;

namespace _123Vendas.Api.Extensions;

public static class HttpResilienceExtensions
{
    public static IHttpClientBuilder AddResilientPolicies(this IHttpClientBuilder builder, string serviceName)
    {
        return builder
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        Log.Warning("[Retry {Service}] Tentativa {RetryAttempt} após {Delay}s", 
                            serviceName, retryAttempt, timespan.TotalSeconds);
                    }))
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, duration) =>
                    {
                        Log.Error("[Circuit Breaker {Service}] ABERTO por {Duration}s", 
                            serviceName, duration.TotalSeconds);
                    },
                    onReset: () => Log.Information("[Circuit Breaker {Service}] RESETADO", serviceName),
                    onHalfOpen: () => Log.Information("[Circuit Breaker {Service}] HALF-OPEN", serviceName)
                ));
    }
}
