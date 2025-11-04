using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Entities;
using Venda.Infrastructure.HealthChecks;

namespace Venda.Infrastructure.Tests;

public class OutboxHealthCheckTests : IDisposable
{
    private readonly VendaDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxHealthCheck _healthCheck;

    public OutboxHealthCheckTests()
    {
        // Configurar banco em memória
        var options = new DbContextOptionsBuilder<VendaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VendaDbContext(options);

        // Configurar service provider
        var services = new ServiceCollection();
        services.AddScoped(_ => _context);
        _serviceProvider = services.BuildServiceProvider();

        var logger = Substitute.For<ILogger<OutboxHealthCheck>>();
        _healthCheck = new OutboxHealthCheck(_serviceProvider, logger);
    }

    [Fact]
    public async Task CheckHealthAsync_SemEventos_DeveRetornarHealthy()
    {
        
        var healthCheckContext = new HealthCheckContext();

        
        var result = await _healthCheck.CheckHealthAsync(healthCheckContext);

        
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Outbox processando normalmente");
        result.Data.Should().ContainKey("pendentes");
        result.Data.Should().ContainKey("falhados");
        result.Data["pendentes"].Should().Be(0);
        result.Data["falhados"].Should().Be(0);
    }

    [Fact]
    public async Task CheckHealthAsync_ComEventosPendentesRecentes_DeveRetornarHealthy()
    {
        
        var healthCheckContext = new HealthCheckContext();

        // Adicionar eventos pendentes recentes (menos de 10 minutos)
        for (int i = 0; i < 5; i++)
        {
            _context.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "CompraCriada",
                EventData = "{}",
                Status = "Pending",
                OccurredAt = DateTime.UtcNow.AddMinutes(-5),
                RetryCount = 0
            });
        }
        await _context.SaveChangesAsync();

        
        var result = await _healthCheck.CheckHealthAsync(healthCheckContext);

        
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["pendentes"].Should().Be(0); // Não conta eventos recentes
    }

    [Fact]
    public async Task CheckHealthAsync_ComMuitosEventosPendentes_DeveRetornarDegraded()
    {
        
        var healthCheckContext = new HealthCheckContext();

        // Adicionar mais de 1000 eventos pendentes antigos (mais de 10 minutos)
        for (int i = 0; i < 1001; i++)
        {
            _context.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "CompraCriada",
                EventData = "{}",
                Status = "Pending",
                OccurredAt = DateTime.UtcNow.AddMinutes(-15),
                RetryCount = 0
            });
        }
        await _context.SaveChangesAsync();

        
        var result = await _healthCheck.CheckHealthAsync(healthCheckContext);

        
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Muitos eventos pendentes");
        result.Data["pendentes"].Should().Be(1001);
    }

    [Fact]
    public async Task CheckHealthAsync_ComMuitosEventosFalhados_DeveRetornarUnhealthy()
    {
        
        var healthCheckContext = new HealthCheckContext();

        // Adicionar mais de 100 eventos falhados
        for (int i = 0; i < 101; i++)
        {
            _context.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "CompraCriada",
                EventData = "{}",
                Status = "Failed",
                OccurredAt = DateTime.UtcNow.AddMinutes(-20),
                RetryCount = 5,
                LastError = "Erro de processamento"
            });
        }
        await _context.SaveChangesAsync();

        
        var result = await _healthCheck.CheckHealthAsync(healthCheckContext);

        
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Muitos eventos falhados");
        result.Data["falhados"].Should().Be(101);
    }

    [Fact]
    public async Task CheckHealthAsync_ComEventosMistos_DeveRetornarStatusCorreto()
    {
        
        var healthCheckContext = new HealthCheckContext();

        // Adicionar eventos pendentes antigos (50)
        for (int i = 0; i < 50; i++)
        {
            _context.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "CompraCriada",
                EventData = "{}",
                Status = "Pending",
                OccurredAt = DateTime.UtcNow.AddMinutes(-15),
                RetryCount = 0
            });
        }

        // Adicionar eventos falhados (10)
        for (int i = 0; i < 10; i++)
        {
            _context.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "CompraAlterada",
                EventData = "{}",
                Status = "Failed",
                OccurredAt = DateTime.UtcNow.AddMinutes(-20),
                RetryCount = 5
            });
        }

        // Adicionar eventos processados (não devem afetar)
        for (int i = 0; i < 100; i++)
        {
            _context.OutboxEvents.Add(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "CompraCancelada",
                EventData = "{}",
                Status = "Processed",
                OccurredAt = DateTime.UtcNow.AddHours(-1),
                RetryCount = 0
            });
        }

        await _context.SaveChangesAsync();

        
        var result = await _healthCheck.CheckHealthAsync(healthCheckContext);

        
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["pendentes"].Should().Be(50);
        result.Data["falhados"].Should().Be(10);
    }

    public void Dispose()
    {
        // Don't dispose context here - it's managed by the service provider scope
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
