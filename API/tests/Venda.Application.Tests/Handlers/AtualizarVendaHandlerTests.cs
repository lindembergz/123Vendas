using _123Vendas.Shared.Common;
using _123Vendas.Shared.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Venda.Application.Commands;
using Venda.Application.DTOs;
using Venda.Application.Handlers;
using Venda.Application.Interfaces;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.ValueObjects;
using Xunit;

namespace Venda.Application.Tests.Handlers;

public class AtualizarVendaHandlerTests
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IMediator _mediator;
    private readonly ILogger<AtualizarVendaHandler> _logger;
    private readonly AtualizarVendaHandler _handler;

    public AtualizarVendaHandlerTests()
    {
        _vendaRepository = Substitute.For<IVendaRepository>();
        _idempotencyStore = Substitute.For<IIdempotencyStore>();
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<AtualizarVendaHandler>>();

        _handler = new AtualizarVendaHandler(
            _vendaRepository,
            _idempotencyStore,
            _mediator,
            _logger);
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveAtualizarVendaComSucesso()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();

        // Criar venda existente com um item
        var vendaExistente = VendaAgregado.Criar(clienteId, filialId.ToString());
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId2, 3, 50m, 0m, 150m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        _vendaRepository.AtualizarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(AtualizarVendaCommand), vendaId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(vendaId);
        result.Value.Itens.Should().HaveCount(1);
        result.Value.Itens[0].ProdutoId.Should().Be(produtoId2);
        result.Value.Itens[0].Quantidade.Should().Be(3);

        await _vendaRepository.Received(1).AtualizarAsync(
            Arg.Is<VendaAgregado>(v =>
                v.Id == vendaId &&
                v.Produtos.Count == 1 &&
                v.Produtos[0].ProdutoId == produtoId2),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.Received(1).SaveAsync(
            requestId,
            nameof(AtualizarVendaCommand),
            vendaId,
            Arg.Any<CancellationToken>());

        // Verifica que eventos foram publicados
        await _mediator.Received().Publish(
            Arg.Is<IDomainEvent>(e => e is ItemCancelado),
            Arg.Any<CancellationToken>());

        await _mediator.Received().Publish(
            Arg.Is<IDomainEvent>(e => e is CompraAlterada),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComVendaNaoEncontrada_DeveRetornarFailure()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 2, 100m, 0m, 200m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns((VendaAgregado?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("não encontrada");
        result.Error.Should().Contain(vendaId.ToString());

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.DidNotReceive().SaveAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        await _mediator.DidNotReceive().Publish(
            Arg.Any<IDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComRequestIdExistente_DeveRetornarVendaExistenteSemAtualizar()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId.ToString());
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId, 2, 100m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 3, 50m, 0m, 150m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(true);

        _idempotencyStore.GetAggregateIdAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(vendaId);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(vendaId);
        result.Value.Itens.Should().HaveCount(1);
        result.Value.Itens[0].ProdutoId.Should().Be(produtoId); // Item original, não atualizado

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());

        await _mediator.DidNotReceive().Publish(
            Arg.Any<IDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComMaisDe20ItensIguais_DeveRetornarFailure()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId.ToString());
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Tentar adicionar 21 itens do mesmo produto
        var itens = Enumerable.Range(1, 21)
            .Select(_ => new ItemVendaDto(produtoId, 1, 100m, 0m, 100m))
            .ToList();

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: itens
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("20 unidades");

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.DidNotReceive().SaveAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComMultiplosItens_DeveSubstituirTodosOsItens()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();
        var produtoId3 = Guid.NewGuid();
        var produtoId4 = Guid.NewGuid();

        // Criar venda existente com 2 itens
        var vendaExistente = VendaAgregado.Criar(clienteId, filialId.ToString());
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        vendaExistente.AdicionarItem(new ItemVenda(produtoId2, 1, 50m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId3, 3, 75m, 0m, 225m),
                new ItemVendaDto(produtoId4, 2, 120m, 0m, 240m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        _vendaRepository.AtualizarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(AtualizarVendaCommand), vendaId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Itens.Should().HaveCount(2);
        result.Value.Itens.Should().Contain(i => i.ProdutoId == produtoId3);
        result.Value.Itens.Should().Contain(i => i.ProdutoId == produtoId4);
        result.Value.Itens.Should().NotContain(i => i.ProdutoId == produtoId1);
        result.Value.Itens.Should().NotContain(i => i.ProdutoId == produtoId2);

        await _vendaRepository.Received(1).AtualizarAsync(
            Arg.Is<VendaAgregado>(v => v.Produtos.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComVendaCancelada_DeveRetornarFailure()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId.ToString());
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId, 2, 100m, 0m));
        vendaExistente.Cancelar(); // Cancelar a venda
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(Guid.NewGuid(), 3, 50m, 0m, 150m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelada");

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComSucesso_DevePublicarEventosDeDominio()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId.ToString());
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId2, 3, 50m, 0m, 150m)
            }
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        _vendaRepository.AtualizarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(AtualizarVendaCommand), vendaId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verifica que evento ItemCancelado foi publicado (ao remover item antigo)
        await _mediator.Received().Publish(
            Arg.Is<IDomainEvent>(e => e is ItemCancelado),
            Arg.Any<CancellationToken>());

        // Verifica que evento CompraAlterada foi publicado (ao adicionar novo item)
        await _mediator.Received().Publish(
            Arg.Is<IDomainEvent>(e => e is CompraAlterada),
            Arg.Any<CancellationToken>());
    }
}
