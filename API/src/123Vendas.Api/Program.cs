using _123Vendas.Shared.Interfaces;
using CRM.Application.Services;
using Estoque.Application.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using System.Text.Json;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.HealthChecks;

// Configurar Serilog com logs estruturados
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "123Vendas.API")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/123vendas-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Iniciando 123Vendas API");

    var builder = WebApplication.CreateBuilder(args);

    // Configurar Serilog como provider de logging
    builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configurar DbContext (necessário para health checks)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=vendas.db";
builder.Services.AddDbContext<VendaDbContext>(options =>
    options.UseSqlite(connectionString, sqliteOptions =>
    {
        sqliteOptions.CommandTimeout(30);
    }));

// Registrar serviços de domínio
builder.Services.AddScoped<Venda.Domain.Interfaces.IPoliticaDesconto, Venda.Domain.Services.PoliticaDesconto>();

// Configurar Health Checks
builder.Services.AddHealthChecks()
    .AddSqlite(
        connectionString: connectionString,
        name: "sqlite",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sqlite" })
    .AddCheck("self", () => HealthCheckResult.Healthy("API está respondendo"), 
        tags: new[] { "self" })
    .AddCheck<OutboxHealthCheck>("outbox", 
        tags: new[] { "outbox" });

// Configurar HttpClient para ClienteService com Polly
builder.Services.AddHttpClient<IClienteService, ClienteService>(client =>
{
    var baseUrl = builder.Configuration["Services:CRM:BaseUrl"] 
        ?? throw new InvalidOperationException("CRM BaseUrl não configurada");
    var timeoutSeconds = int.Parse(builder.Configuration["Services:CRM:TimeoutSeconds"] ?? "30");
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine(
                $"[Retry CRM] Tentativa {retryAttempt} após {timespan.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Console.WriteLine(
                $"[Circuit Breaker CRM] ABERTO por {duration.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        },
        onReset: () =>
        {
            Console.WriteLine("[Circuit Breaker CRM] RESETADO - Serviço voltou ao normal");
        },
        onHalfOpen: () =>
        {
            Console.WriteLine("[Circuit Breaker CRM] HALF-OPEN - Testando serviço");
        }));

// Configurar HttpClient para ProdutoService com Polly
builder.Services.AddHttpClient<IProdutoService, ProdutoService>(client =>
{
    var baseUrl = builder.Configuration["Services:Estoque:BaseUrl"] 
        ?? throw new InvalidOperationException("Estoque BaseUrl não configurada");
    var timeoutSeconds = int.Parse(builder.Configuration["Services:Estoque:TimeoutSeconds"] ?? "30");
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine(
                $"[Retry Estoque] Tentativa {retryAttempt} após {timespan.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Console.WriteLine(
                $"[Circuit Breaker Estoque] ABERTO por {duration.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        },
        onReset: () =>
        {
            Console.WriteLine("[Circuit Breaker Estoque] RESETADO - Serviço voltou ao normal");
        },
        onHalfOpen: () =>
        {
            Console.WriteLine("[Circuit Breaker Estoque] HALF-OPEN - Testando serviço");
        }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Health Check Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        }, new JsonSerializerOptions { WriteIndented = true });

        await context.Response.WriteAsync(result);
    }
});

// Readiness probe (apenas self check)
app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("self"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString()
        }));
    }
});

// Liveness probe (verifica dependências críticas: db e self)
app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("self"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString()
        }));
    }
});

    app.Run();
    
    Log.Information("123Vendas API encerrada com sucesso");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro fatal ao iniciar 123Vendas API");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program class accessible for integration tests
public partial class Program { }
