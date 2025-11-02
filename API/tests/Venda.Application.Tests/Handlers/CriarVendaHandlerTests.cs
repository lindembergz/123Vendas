using _123Vendas.Shared.Common;
using _123Vendas.Shared.Events;
using _123Vendas.Shared.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Venda.Application.Commands;
using Venda.Application.DTOs;
using Venda.Application.Handlers;
using Venda.Application.Interfaces;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.Services;
using Xunit;

namespace Venda.Application.Tests.Handlers;

public class CriarVendaHandlerTests
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IClienteService _clienteService;
    private readonly IMediator _mediator;
    private readonly ILogger<CriarVendaHandler> _logger;
    private readonly IPoliticaDesconto _politicaDesconto;
    private readonly CriarVendaHandler _handler;

    public CriarVendaHandlerTests()
    {
        _vendaRepository = Substitute.For<IVendaRepository>();
        _idempotencyStore = Substitute.For<IIdempotencyStore>();
        _clienteService = Substitute.For<IClienteService>();
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<CriarVendaHandler>>();
        _politicaDesconto = new PoliticaDesconto();

        _handler = new CriarVendaHandler(
            _vendaRepository,
            _idempotencyStore,
            _clienteService,
            _mediator,
            _logger,
            _politicaDesconto);
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveCriarVendaComSucesso()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var command = new CriarVendaCommand(
            RequestId: requestId,
            ClienteId: clienteId,
            FilialId: filialId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId, 2, 100m, 0m, 200m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _clienteService.ClienteExisteAsync(clienteId, Arg.Any<CancellationToken>())
            .Returns(true);

        _vendaRepository.AdicionarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(CriarVendaCommand), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _vendaRepository.Received(1).AdicionarAsync(
            Arg.Is<VendaAgregado>(v => 
                v.ClienteId == clienteId && 
                v.FilialId == filialId &&
                v.Status == StatusVenda.Ativa &&
                v.Produtos.Count == 1),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.Received(1).SaveAsync(
            requestId,
            nameof(CriarVendaCommand),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        // Verifica que evento CompraAlterada foi publicado (ao adicionar item)
        await _mediator.Received().Publish(
            Arg.Is<IDomainEvent>(e => e is CompraAlterada),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComMaisDe20ItensIguais_DeveRetornarFailure()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var itens = Enumerable.Range(1, 21)
            .Select(_ => new ItemVendaDto(produtoId, 1, 100m, 0m, 100m))
            .ToList();

        var command = new CriarVendaCommand(
            RequestId: requestId,
            ClienteId: clienteId,
            FilialId: filialId,
            Itens: itens
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _clienteService.ClienteExisteAsync(clienteId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("20 unidades");

        await _vendaRepository.DidNotReceive().AdicionarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.DidNotReceive().SaveAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComRequestIdExistente_DeveRetornarAggregateIdSemCriarNovaVenda()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var existingAggregateId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();

        var command = new CriarVendaCommand(
            RequestId: requestId,
            ClienteId: clienteId,
            FilialId: filialId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0m, 200m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(true);
        
        _idempotencyStore.GetAggregateIdAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(existingAggregateId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingAggregateId);

        await _vendaRepository.DidNotReceive().AdicionarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());

        await _clienteService.DidNotReceive().ClienteExisteAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        await _mediator.DidNotReceive().Publish(
            Arg.Any<IDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComCrmIndisponivel_DeveCriarVendaPendenteValidacao()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var command = new CriarVendaCommand(
            RequestId: requestId,
            ClienteId: clienteId,
            FilialId: filialId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId, 2, 100m, 0m, 200m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _clienteService.ClienteExisteAsync(clienteId, Arg.Any<CancellationToken>())
            .Throws(new Exception("CRM indisponível"));

        _vendaRepository.AdicionarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(CriarVendaCommand), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _vendaRepository.Received(1).AdicionarAsync(
            Arg.Is<VendaAgregado>(v => 
                v.Status == StatusVenda.PendenteValidacao),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.Received(1).SaveAsync(
            requestId,
            nameof(CriarVendaCommand),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComClienteInvalido_DeveCriarVendaPendenteValidacao()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var command = new CriarVendaCommand(
            RequestId: requestId,
            ClienteId: clienteId,
            FilialId: filialId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId, 2, 100m, 0m, 200m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _clienteService.ClienteExisteAsync(clienteId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.AdicionarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(CriarVendaCommand), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _vendaRepository.Received(1).AdicionarAsync(
            Arg.Is<VendaAgregado>(v => 
                v.Status == StatusVenda.PendenteValidacao),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComSucesso_DevePublicarEventosDeDominio()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var command = new CriarVendaCommand(
            RequestId: requestId,
            ClienteId: clienteId,
            FilialId: filialId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId, 2, 100m, 0m, 200m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _clienteService.ClienteExisteAsync(clienteId, Arg.Any<CancellationToken>())
            .Returns(true);

        _vendaRepository.AdicionarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(CriarVendaCommand), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verifica que evento CompraAlterada foi publicado (ao adicionar item)
        await _mediator.Received().Publish(
            Arg.Is<IDomainEvent>(e => e is CompraAlterada),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComMultiplosItens_DeveAdicionarTodosOsItens()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();

        var command = new CriarVendaCommand(
            RequestId: requestId,
            ClienteId: clienteId,
            FilialId: filialId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0m, 200m),
                new ItemVendaDto(Guid.NewGuid(), 3, 50m, 0m, 150m),
                new ItemVendaDto(Guid.NewGuid(), 1, 200m, 0m, 200m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _clienteService.ClienteExisteAsync(clienteId, Arg.Any<CancellationToken>())
            .Returns(true);

        _vendaRepository.AdicionarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(CriarVendaCommand), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _vendaRepository.Received(1).AdicionarAsync(
            Arg.Is<VendaAgregado>(v => v.Produtos.Count == 3),
            Arg.Any<CancellationToken>());
    }
}
