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
using Venda.Domain.Services;
using Venda.Domain.ValueObjects;
using Xunit;

namespace Venda.Application.Tests.Handlers;

public class AtualizarVendaHandlerTests
{
    private readonly IPoliticaDesconto _politicaDesconto = new PoliticaDesconto();
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<AtualizarVendaHandler> _logger;
    private readonly AtualizarVendaHandler _handler;

    public AtualizarVendaHandlerTests()
    {
        _vendaRepository = Substitute.For<IVendaRepository>();
        _idempotencyStore = Substitute.For<IIdempotencyStore>();
        _logger = Substitute.For<ILogger<AtualizarVendaHandler>>();
        
        var updaterLogger = Substitute.For<ILogger<Venda.Application.Services.VendaUpdater>>();
        var vendaUpdater = new Venda.Application.Services.VendaUpdater(updaterLogger);

        _handler = new AtualizarVendaHandler(
            _vendaRepository,
            _idempotencyStore,
            vendaUpdater,
            _logger);
    }

    [Fact]
    public async Task Handle_AdicionandoNovoItem_DeveManterItensExistentes()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();

        // Criar venda existente com um item
        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Comando com item existente + novo item
        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId1, 2, 100m, 0m, 200m), // Item existente (mesma quantidade)
                new ItemVendaDto(produtoId2, 3, 50m, 0m, 150m)   // Novo item
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(vendaId);
        result.Value.Itens.Should().HaveCount(2);
        result.Value.Itens.Should().Contain(i => i.ProdutoId == produtoId1 && i.Quantidade == 2);
        result.Value.Itens.Should().Contain(i => i.ProdutoId == produtoId2 && i.Quantidade == 3);

        await _vendaRepository.Received(1).AtualizarAsync(
            Arg.Is<VendaAgregado>(v =>
                v.Id == vendaId &&
                v.Produtos.Count == 2),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.Received(1).SaveAsync(
            requestId,
            nameof(AtualizarVendaCommand),
            vendaId,
            Arg.Any<CancellationToken>());

        // Nota: Eventos são salvos no Outbox pelo Repository e publicados pelo OutboxProcessor
    }

    [Fact]
    public async Task Handle_ComVendaNaoEncontrada_DeveRetornarFailure()
    {
        
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

        
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        
        await act.Should().ThrowAsync<_123Vendas.Shared.Exceptions.NotFoundException>()
            .WithMessage($"*{vendaId}*");

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
    public async Task Handle_ComRequestIdExistente_DeveRetornarVendaExistenteSemAtualizar()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(vendaId);
        result.Value.Itens.Should().HaveCount(1);
        result.Value.Itens[0].ProdutoId.Should().Be(produtoId); // Item original, não atualizado

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComMaisDe20ItensIguais_DeveRetornarFailure()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Tentar adicionar 21 unidades do mesmo produto
        var itens = new List<ItemVendaDto>
        {
            new ItemVendaDto(produtoId, 21, 100m, 0m, 2100m)
        };

        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: itens
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
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
    public async Task Handle_SubstituindoTodosOsItens_DeveRemoverAntigosEAdicionarNovos()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();
        var produtoId3 = Guid.NewGuid();
        var produtoId4 = Guid.NewGuid();

        // Criar venda existente com 2 itens
        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        vendaExistente.AdicionarItem(new ItemVenda(produtoId2, 1, 50m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Comando com produtos completamente diferentes
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
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

        // Nota: Eventos (ItemCancelado e CompraAlterada) são salvos no Outbox pelo Repository
    }

    [Fact]
    public async Task Handle_ComVendaCancelada_DeveRetornarFailure()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelada");

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AumentandoQuantidadeDeItemExistente_DeveAtualizarQuantidade()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId, 2, 100m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Comando aumentando quantidade de 2 para 5
        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId, 5, 100m, 0m, 500m)
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();
        result.Value!.Itens.Should().HaveCount(1);
        result.Value.Itens[0].ProdutoId.Should().Be(produtoId);
        result.Value.Itens[0].Quantidade.Should().Be(5);

        // Nota: Evento CompraAlterada é salvo no Outbox pelo Repository
    }

    [Fact]
    public async Task Handle_DiminuindoQuantidadeDeItemExistente_DeveAtualizarQuantidade()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId, 5, 100m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Comando diminuindo quantidade de 5 para 2
        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId, 2, 100m, 0m, 200m)
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();
        result.Value!.Itens.Should().HaveCount(1);
        result.Value.Itens[0].ProdutoId.Should().Be(produtoId);
        result.Value.Itens[0].Quantidade.Should().Be(2);

        // Nota: Evento CompraAlterada é salvo no Outbox pelo Repository
    }

    [Fact]
    public async Task Handle_RemovendoItemCompletamente_DevePublicarItemCancelado()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId1 = Guid.NewGuid();
        var produtoId2 = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId1, 2, 100m, 0m));
        vendaExistente.AdicionarItem(new ItemVenda(produtoId2, 3, 50m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Comando removendo produtoId1 (mantém apenas produtoId2)
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();
        result.Value!.Itens.Should().HaveCount(1);
        result.Value.Itens[0].ProdutoId.Should().Be(produtoId2);

        // Nota: Evento ItemCancelado é salvo no Outbox pelo Repository
    }

    [Fact]
    public async Task Handle_MantentoQuantidadeIgual_NaoDeveGerarEventos()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var filialId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var vendaExistente = VendaAgregado.Criar(clienteId, filialId, _politicaDesconto);
        vendaExistente.DefinirNumeroVenda(1);
        vendaExistente.AdicionarItem(new ItemVenda(produtoId, 3, 100m, 0m));
        vendaExistente.ClearDomainEvents();

        // Usar reflexão para definir o Id
        var idProperty = typeof(VendaAgregado).GetProperty("Id");
        idProperty!.SetValue(vendaExistente, vendaId);

        // Comando com mesma quantidade (3)
        var command = new AtualizarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId,
            Itens: new List<ItemVendaDto>
            {
                new ItemVendaDto(produtoId, 3, 100m, 0m, 300m)
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

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();
        result.Value!.Itens.Should().HaveCount(1);
        result.Value.Itens[0].Quantidade.Should().Be(3);

        // Nota: Nenhum evento é gerado pois nada mudou
    }
}
