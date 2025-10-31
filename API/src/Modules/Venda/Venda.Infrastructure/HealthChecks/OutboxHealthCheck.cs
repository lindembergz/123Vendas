using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Venda.Infrastructure.Data;

namespace Venda.Infrastructure.HealthChecks;

public class OutboxHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxHealthCheck> _logger;

    public OutboxHealthCheck(IServiceProvider serviceProvider, ILogger<OutboxHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

            // Verificar eventos pendentes há muito tempo (mais de 10 minutos)
            var eventosPendentes = await dbContext.OutboxEvents
                .Where(e => e.Status == "Pending" && e.OccurredAt < DateTime.UtcNow.AddMinutes(-10))
                .CountAsync(cancellationToken);

            var eventosFalhados = await dbContext.OutboxEvents
                .Where(e => e.Status == "Failed")
                .CountAsync(cancellationToken);

            // Unhealthy: Muitos eventos falhados (>100)
            if (eventosFalhados > 100)
            {
                return HealthCheckResult.Unhealthy(
                    $"Muitos eventos falhados: {eventosFalhados}",
                    data: new Dictionary<string, object>
                    {
                        { "pendentes", eventosPendentes },
                        { "falhados", eventosFalhados }
                    });
            }

            // Degraded: Muitos eventos pendentes (>1000)
            if (eventosPendentes > 1000)
            {
                return HealthCheckResult.Degraded(
                    $"Muitos eventos pendentes: {eventosPendentes}",
                    data: new Dictionary<string, object>
                    {
                        { "pendentes", eventosPendentes },
                        { "falhados", eventosFalhados }
                    });
            }

            // Healthy: Tudo funcionando normalmente
            return HealthCheckResult.Healthy(
                "Outbox processando normalmente",
                data: new Dictionary<string, object>
                {
                    { "pendentes", eventosPendentes },
                    { "falhados", eventosFalhados }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar saúde do Outbox");
            return HealthCheckResult.Unhealthy("Erro ao acessar Outbox", ex);
        }
    }
}
