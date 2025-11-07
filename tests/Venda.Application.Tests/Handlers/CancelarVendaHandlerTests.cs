using _123Vendas.Shared.Common;
using _123Vendas.Shared.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Venda.Application.Commands;
using Venda.Application.Handlers;
using Venda.Application.Interfaces;
using Venda.Domain.Aggregates;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.Services;
using Venda.Domain.ValueObjects;
using Xunit;

namespace Venda.Application.Tests.Handlers;

public class CancelarVendaHandlerTests
{
    private readonly IPoliticaDesconto _politicaDesconto = new PoliticaDesconto();
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<CancelarVendaHandler> _logger;
    private readonly CancelarVendaHandler _handler;

    public CancelarVendaHandlerTests()
    {
        _vendaRepository = Substitute.For<IVendaRepository>();
        _idempotencyStore = Substitute.For<IIdempotencyStore>();
        _logger = Substitute.For<ILogger<CancelarVendaHandler>>();

        _handler = new CancelarVendaHandler(
            _vendaRepository,
            _idempotencyStore,
            _logger);
    }

    [Fact]
    public async Task Handle_ComVendaValida_DeveCancelarComSucesso()
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

        var command = new CancelarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        _vendaRepository.AtualizarAsync(Arg.Any<VendaAgregado>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _idempotencyStore.SaveAsync(requestId, nameof(CancelarVendaCommand), vendaId, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();

        await _vendaRepository.Received(1).AtualizarAsync(
            Arg.Is<VendaAgregado>(v =>
                v.Id == vendaId &&
                v.Status == StatusVenda.Cancelada),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.Received(1).SaveAsync(
            requestId,
            nameof(CancelarVendaCommand),
            vendaId,
            Arg.Any<CancellationToken>());

        // Nota: Eventos são salvos no Outbox pelo Repository e publicados pelo OutboxProcessor
    }

    [Fact]
    public async Task Handle_ComVendaNaoEncontrada_DeveRetornarFailure()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();

        var command = new CancelarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns((VendaAgregado?)null);

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
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
    }

    [Fact]
    public async Task Handle_ComRequestIdExistente_DeveRetornarSucessoSemCancelar()
    {
        
        var requestId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();

        var command = new CancelarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(true);

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsSuccess.Should().BeTrue();

        await _vendaRepository.DidNotReceive().ObterPorIdAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComVendaJaCancelada_DeveRetornarFailure()
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

        var command = new CancelarVendaCommand(
            RequestId: requestId,
            VendaId: vendaId
        );

        _idempotencyStore.ExistsAsync(requestId, Arg.Any<CancellationToken>())
            .Returns(false);

        _vendaRepository.ObterPorIdAsync(vendaId, Arg.Any<CancellationToken>())
            .Returns(vendaExistente);

        
        var result = await _handler.Handle(command, CancellationToken.None);

        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("já está cancelada");

        await _vendaRepository.DidNotReceive().AtualizarAsync(
            Arg.Any<VendaAgregado>(),
            Arg.Any<CancellationToken>());

        await _idempotencyStore.DidNotReceive().SaveAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }
}
