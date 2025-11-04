using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using System.Text.Json;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.HealthChecks;

namespace _123Vendas.Api.Extensions;

public static class WebServiceCollectionExtensions
{
    public static void AddSerilogConfiguration(this WebApplicationBuilder builder)
    {
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

        builder.Host.UseSerilog();
    }

    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
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

            options.TagActionsBy(api => new[] { api.GroupName ?? "Vendas" });
            options.DocInclusionPredicate((name, api) => true);
        });

        return services;
    }

    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services, string connectionString)
    {
        services.AddHealthChecks()
            .AddSqlite(
                connectionString: connectionString,
                name: "sqlite",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sqlite" })
            .AddCheck("self", () => HealthCheckResult.Healthy("API está respondendo"),
                tags: new[] { "self" })
            .AddCheck<OutboxHealthCheck>("outbox",
                tags: new[] { "outbox" });

        return services;
    }

    public static void MapAppHealthChecks(this WebApplication app)
    {
        // Health Check completo
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
    }

    public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<VendaDbContext>(options =>
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.CommandTimeout(30);
            }));

        return services;
    }
}
