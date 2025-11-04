using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace _123Vendas.Api.Filters;

/// <summary>
/// Filtro global que intercepta e trata exceções não tratadas no pipeline HTTP.
/// Converte exceções técnicas em respostas ProblemDetails padronizadas (RFC 7807).
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;
    
    public GlobalExceptionFilter(
        ILogger<GlobalExceptionFilter> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }
    
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var traceId = context.HttpContext.TraceIdentifier;
        var path = context.HttpContext.Request.Path;
        
        // Log estruturado com informações contextuais
        _logger.LogError(exception,
            "Exceção não tratada: {ExceptionType} - Path: {Path} - TraceId: {TraceId}",
            exception.GetType().Name,
            path,
            traceId);
        
        // Mapeia exceção para ProblemDetails apropriado
        var problemDetails = MapExceptionToProblemDetails(exception, traceId);
        
        // Configura resposta HTTP
        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
        
        // Marca exceção como tratada para evitar propagação
        context.ExceptionHandled = true;
    }
    
    /// <summary>
    /// Mapeia diferentes tipos de exceções para ProblemDetails com status HTTP apropriado.
    /// </summary>
    private ProblemDetails MapExceptionToProblemDetails(Exception exception, string traceId)
    {
        var (status, title, detail) = exception switch
        {
            // Erros de validação de argumentos
            ArgumentException or ArgumentNullException => (
                StatusCodes.Status400BadRequest,
                "Erro de validação",
                exception.Message
            ),
            
            // Erros de persistência (banco de dados)
            DbUpdateException => (
                StatusCodes.Status500InternalServerError,
                "Erro de persistência",
                "Ocorreu um erro ao salvar dados no banco de dados"
            ),
            
            // Operação cancelada pelo cliente
            TaskCanceledException or OperationCanceledException => (
                499, // Client Closed Request (não oficial mas amplamente usado)
                "Operação cancelada",
                "A requisição foi cancelada pelo cliente"
            ),
            
            // Falha de comunicação com serviços externos
            HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "Erro de comunicação externa",
                "Falha ao comunicar com serviço externo"
            ),
            
            // Timeout em operações
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                "Timeout",
                "A operação excedeu o tempo limite"
            ),
            
            // Exceção genérica (catch-all)
            _ => (
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                "Ocorreu um erro inesperado ao processar a requisição"
            )
        };
        
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            // Em desenvolvimento: mostra mensagem real da exceção
            // Em produção: mostra mensagem genérica
            Detail = _environment.IsDevelopment() ? exception.Message : detail,
            Extensions = { ["traceId"] = traceId }
        };
        
        // Adiciona stack trace apenas em ambiente de desenvolvimento
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }
        
        return problemDetails;
    }
}
