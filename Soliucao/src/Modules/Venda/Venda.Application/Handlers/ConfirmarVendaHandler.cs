using _123Vendas.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Venda.Application.Commands;
using Venda.Application.Interfaces;
using Venda.Domain.Interfaces;

namespace Venda.Application.Handlers;

public class ConfirmarVendaHandler : IRequestHandler<ConfirmarVendaCommand, Result>
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IMediator _mediator;
    private readonly ILogger<ConfirmarVendaHandler> _logger;

    public ConfirmarVendaHandler(
        IVendaRepository vendaRepository,
        IIdempotencyStore idempotencyStore,
        IMediator mediator,
        ILogger<ConfirmarVendaHandler> logger)
    {
        _vendaRepository = vendaRepository;
        _idempotencyStore = idempotencyStore;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result> Handle(ConfirmarVendaCommand request, CancellationToken ct)
    {
        // 1. Verificar idempotência
        if (await _idempotencyStore.ExistsAsync(request.RequestId, ct))
        {
            _logger.LogInformation(
                "RequestId {RequestId} já processado. Confirmação já realizada para VendaId: {VendaId}",
                request.RequestId, request.VendaId);
            return Result.Success();
        }

        // 2. Carregar venda
        var venda = await _vendaRepository.ObterPorIdAsync(request.VendaId, ct);
        if (venda == null)
        {
            _logger.LogWarning(
                "Venda {VendaId} não encontrada para confirmação",
                request.VendaId);
            return Result.Failure($"Venda {request.VendaId} não encontrada.");
        }

        // 3. Confirmar venda (altera status para Ativa)
        var resultado = venda.Confirmar();
        if (resultado.IsFailure)
        {
            _logger.LogWarning(
                "Falha ao confirmar venda {VendaId}: {Error}",
                request.VendaId, resultado.Error);
            return resultado;
        }

        // 4. Persistir alterações
        await _vendaRepository.AtualizarAsync(venda, ct);

        _logger.LogInformation(
            "Venda {VendaId} (Número: {NumeroVenda}) confirmada com sucesso. Status: {Status}",
            venda.Id, venda.NumeroVenda, venda.Status);

        // 5. Publicar eventos de domínio via MediatR
        foreach (var domainEvent in venda.DomainEvents)
        {
            await _mediator.Publish(domainEvent, ct);
        }

        venda.ClearDomainEvents();

        // 6. Salvar RequestId no IdempotencyStore
        await _idempotencyStore.SaveAsync(
            request.RequestId,
            nameof(ConfirmarVendaCommand),
            venda.Id,
            ct);

        return Result.Success();
    }
}
