using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Venda.Infrastructure.Configuration;
using Venda.Infrastructure.Services;

namespace Venda.Infrastructure.Tests;

public class ExponentialBackoffRetryStrategyTests
{
    private readonly ILogger<ExponentialBackoffRetryStrategy> _logger;
    private readonly RetryStrategyOptions _options;
    
    public ExponentialBackoffRetryStrategyTests()
    {
        _logger = Substitute.For<ILogger<ExponentialBackoffRetryStrategy>>();
        _options = new RetryStrategyOptions
        {
            MaxRetries = 3,
            InitialDelayMs = 10 // Usar delay menor para testes rápidos
        };
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceeds_ShouldReturnResult()
    {
        // Arrange
        var strategy = new ExponentialBackoffRetryStrategy(
            Options.Create(_options),
            _logger);
        
        var expectedResult = 42;
        
        // Act
        var result = await strategy.ExecuteAsync(async () =>
        {
            await Task.CompletedTask;
            return expectedResult;
        });
        
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenDbUpdateExceptionWithUniqueConstraint_ShouldRetry()
    {
        // Arrange
        var strategy = new ExponentialBackoffRetryStrategy(
            Options.Create(_options),
            _logger);
        
        var attemptCount = 0;
        var expectedResult = 100;
        
        // Act
        var result = await strategy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                // Simular violação de constraint única
                var innerException = new Exception("UNIQUE constraint failed: IX_Vendas_FilialId_NumeroVenda_Unique");
                throw new DbUpdateException("Database update failed", innerException);
            }
            await Task.CompletedTask;
            return expectedResult;
        });
        
        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(2);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenDbUpdateConcurrencyException_ShouldRetry()
    {
        // Arrange
        var strategy = new ExponentialBackoffRetryStrategy(
            Options.Create(_options),
            _logger);
        
        var attemptCount = 0;
        var expectedResult = 200;
        
        // Act
        var result = await strategy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new DbUpdateConcurrencyException("Concurrency conflict");
            }
            await Task.CompletedTask;
            return expectedResult;
        });
        
        // Assert
        result.Should().Be(expectedResult);
        attemptCount.Should().Be(2);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenMaxRetriesExceeded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var strategy = new ExponentialBackoffRetryStrategy(
            Options.Create(_options),
            _logger);
        
        var attemptCount = 0;
        
        // Act
        var act = async () => await strategy.ExecuteAsync<bool>(async () =>
        {
            attemptCount++;
            await Task.CompletedTask;
            var innerException = new Exception("UNIQUE constraint failed");
            throw new DbUpdateException("Database update failed", innerException);
        });
        
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*após 3 tentativas*");
        attemptCount.Should().Be(3);
    }
    
    [Fact]
    public async Task ExecuteAsync_ShouldUseExponentialBackoff()
    {
        // Arrange
        var strategy = new ExponentialBackoffRetryStrategy(
            Options.Create(_options),
            _logger);
        
        var attemptCount = 0;
        var delays = new List<long>();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                if (attemptCount > 0)
                {
                    delays.Add(stopwatch.ElapsedMilliseconds);
                    stopwatch.Restart();
                }
                
                attemptCount++;
                await Task.CompletedTask;
                
                if (attemptCount <= 3)
                {
                    var innerException = new Exception("UNIQUE constraint failed");
                    throw new DbUpdateException("Database update failed", innerException);
                }
                
                return true;
            });
        }
        catch
        {
            // Ignorar exceção para verificar delays
        }
        
        // Assert
        attemptCount.Should().Be(3);
        delays.Should().HaveCountGreaterThan(0);
        
        // Verificar que os delays aumentam exponencialmente (com margem de erro)
        // Delay 1: ~10ms, Delay 2: ~20ms
        if (delays.Count >= 2)
        {
            delays[1].Should().BeGreaterThan(delays[0]);
        }
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenNonRetriableException_ShouldThrowImmediately()
    {
        // Arrange
        var strategy = new ExponentialBackoffRetryStrategy(
            Options.Create(_options),
            _logger);
        
        var attemptCount = 0;
        
        // Act
        var act = async () => await strategy.ExecuteAsync<bool>(async () =>
        {
            attemptCount++;
            await Task.CompletedTask;
            throw new InvalidOperationException("Non-retriable error");
        });
        
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Non-retriable error");
        attemptCount.Should().Be(1); // Não deve fazer retry
    }
}
