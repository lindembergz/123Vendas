using _123Vendas.Api.Endpoints;
using _123Vendas.Api.Extensions;
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
// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "123Vendas API",
        Version = "v1",
        Description = "API para gerenciamento de vendas com regras de negócio e eventos de domínio",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "123Vendas Team"
        }
    });
    
    // Adicionar descrições dos endpoints
    options.TagActionsBy(api => new[] { api.GroupName ?? "Vendas" });
    options.DocInclusionPredicate((name, api) => true);
});

// Registrar serviços de aplicação (MediatR, Repositories, etc.)
builder.Services.AddApplicationServices();

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

// Configurar serviços externos (CRM e Estoque)
// MODO: MOCK para desenvolvimento (sempre retorna sucesso)
// Para usar serviços HTTP reais, descomente o bloco abaixo e comente os mocks

// ===== MOCK SERVICES (DESENVOLVIMENTO) =====
builder.Services.AddScoped<IClienteService, CRM.Application.Services.ClienteServiceMock>();
builder.Services.AddScoped<IProdutoService, Estoque.Application.Services.ProdutoServiceMock>();
Log.Information("Usando MOCK services para CRM e Estoque");

// ===== HTTP SERVICES (PRODUÇÃO) =====
// Descomente para usar serviços HTTP reais
/*
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
            Log.Warning("[Retry CRM] Tentativa {RetryAttempt} após {Delay}s", retryAttempt, timespan.TotalSeconds);
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Log.Error("[Circuit Breaker CRM] ABERTO por {Duration}s", duration.TotalSeconds);
        },
        onReset: () =>
        {
            Log.Information("[Circuit Breaker CRM] RESETADO");
        },
        onHalfOpen: () =>
        {
            Log.Information("[Circuit Breaker CRM] HALF-OPEN");
        }));

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
            Log.Warning("[Retry Estoque] Tentativa {RetryAttempt} após {Delay}s", retryAttempt, timespan.TotalSeconds);
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Log.Error("[Circuit Breaker Estoque] ABERTO por {Duration}s", duration.TotalSeconds);
        },
        onReset: () =>
        {
            Log.Information("[Circuit Breaker Estoque] RESETADO");
        },
        onHalfOpen: () =>
        {
            Log.Information("[Circuit Breaker Estoque] HALF-OPEN");
        }));
*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "123Vendas API v1");
        options.RoutePrefix = "swagger"; // Acesso via /swagger
        options.DocumentTitle = "123Vendas API - Documentação";
        options.DisplayRequestDuration();
    });
    
    Log.Information("Swagger UI disponível em: http://localhost:5197/swagger");
}

app.UseHttpsRedirection();

// Mapear endpoints de vendas
app.MapVendasEndpoints();

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

    // Aplicar migrações automaticamente no startup
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();
        dbContext.Database.Migrate();
        Log.Information("Migrações aplicadas com sucesso");
    }

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

// Make Program class accessible for integration tests
public partial class Program { }
