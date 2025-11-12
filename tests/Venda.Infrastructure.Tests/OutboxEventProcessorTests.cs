using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Venda.Infrastructure.Entities;
using Venda.Infrastructure.Services;
using _123Vendas.Shared.Events;

namespace Venda.Infrastructure.Tests;

public class OutboxEventProcessorTests
{
    private readonly IMediator _mediatorMock;
    private readonly ILogger<OutboxEventProcessor> _loggerMock;
    private readonly OutboxEventProcessor _processor;

    public OutboxEventProcessorTests()
    {
        _mediatorMock = Substitute.For<IMediator>();
        _loggerMock = Substitute.For<ILogger<OutboxEventProcessor>>();
        _processor = new OutboxEventProcessor(_mediatorMock, _loggerMock);
    }

    [Fact]
    public async Task ProcessAsync_WhenEventValid_ShouldPublishAndReturnSuccess()
    {
        // Arrange
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var evento = new CompraCriada(vendaId, 1, clienteId);
        var eventData = JsonSerializer.Serialize(evento);
        var eventType = typeof(CompraCriada).AssemblyQualifiedName!;

        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EventData = eventData,
            OccurredAt = DateTime.UtcNow,
            Status = "Pending"
        };

        // Act
        var result = await _processor.ProcessAsync(outboxEvent);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        await _mediatorMock.Received(1).Publish(
            Arg.Any<CompraCriada>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenEventTypeNotFound_ShouldReturnFailure()
    {
        // Arrange
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "NonExistent.Type, NonExistent.Assembly",
            EventData = "{}",
            OccurredAt = DateTime.UtcNow,
            Status = "Pending"
        };

        // Act
        var result = await _processor.ProcessAsync(outboxEvent);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage.Should().Contain("desserializar");
        await _mediatorMock.DidNotReceive().Publish(
            Arg.Any<INotification>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenDeserializationFails_ShouldReturnFailure()
    {
        // Arrange
        var eventType = typeof(CompraCriada).AssemblyQualifiedName!;
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EventData = "invalid json {{{",
            OccurredAt = DateTime.UtcNow,
            Status = "Pending"
        };

        // Act
        var result = await _processor.ProcessAsync(outboxEvent);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
        await _mediatorMock.DidNotReceive().Publish(
            Arg.Any<INotification>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenPublishFails_ShouldReturnFailure()
    {
        // Arrange
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var evento = new CompraCriada(vendaId, 1, clienteId);
        var eventData = JsonSerializer.Serialize(evento);
        var eventType = typeof(CompraCriada).AssemblyQualifiedName!;

        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EventData = eventData,
            OccurredAt = DateTime.UtcNow,
            Status = "Pending"
        };

        var expectedException = new InvalidOperationException("MediatR publish failed");
        _mediatorMock
            .Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(expectedException));

        // Act
        var result = await _processor.ProcessAsync(outboxEvent);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(expectedException.Message);
    }

    [Fact]
    public async Task ProcessAsync_WhenOutboxEventNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _processor.ProcessAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessAsync_WithCancellationToken_ShouldPassToMediator()
    {
        // Arrange
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var evento = new CompraCriada(vendaId, 1, clienteId);
        var eventData = JsonSerializer.Serialize(evento);
        var eventType = typeof(CompraCriada).AssemblyQualifiedName!;

        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            EventData = eventData,
            OccurredAt = DateTime.UtcNow,
            Status = "Pending"
        };

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Act
        await _processor.ProcessAsync(outboxEvent, cancellationToken);

        // Assert
        await _mediatorMock.Received(1).Publish(
            Arg.Any<CompraCriada>(), 
            cancellationToken);
    }
}
