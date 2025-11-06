using Serilog.Context;

namespace _123Vendas.Api.Middleware;

/// <summary>
/// Middleware que adiciona CorrelationId para rastreamento de requisições
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Usa o CorrelationId do header ou gera um novo
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Adiciona ao response header
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        // Adiciona ao contexto do Serilog para aparecer em todos os logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
