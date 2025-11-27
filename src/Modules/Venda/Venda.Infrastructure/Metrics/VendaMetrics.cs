using System.Diagnostics.Metrics;

namespace Venda.Infrastructure.Metrics;

/// <summary>
/// Encapsula as métricas de negócio do módulo de Vendas.
/// Utiliza System.Diagnostics.Metrics para integração com OpenTelemetry.
/// </summary>
public class VendaMetrics
{
    public const string MeterName = "Venda.Infrastructure";
    private readonly Counter<long> _vendasCriadasCounter;

    public VendaMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _vendasCriadasCounter = meter.CreateCounter<long>(
            "vendas_criadas_total",
            description: "Total de vendas criadas com sucesso");
    }

    /// <summary>
    /// Incrementa o contador de vendas criadas.
    /// </summary>
    public void ContarVendaCriada() => _vendasCriadasCounter.Add(1);
}
