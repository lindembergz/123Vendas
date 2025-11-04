using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace _123Vendas.Api.Middleware;

/// <summary>
/// Middleware global que intercepta e trata exceções não tratadas no pipeline HTTP.
/// Converte exceções técnicas em respostas ProblemDetails padronizadas (RFC 7807).
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var path = context.Request.Path;
        
        // Log estruturado com informações contextuais
        _logger.LogError(exception,
            "Exceção não tratada: {ExceptionType} - Path: {Path} - TraceId: {TraceId}",
            exception.GetType().Name,
            path,
            traceId);
        
        // Mapeia exceção para ProblemDetails apropriado
        var problemDetails = MapExceptionToProblemDetails(exception, traceId);
        
        // Configura resposta HTTP
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        
        await context.Response.WriteAsJsonAsync(problemDetails);
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
