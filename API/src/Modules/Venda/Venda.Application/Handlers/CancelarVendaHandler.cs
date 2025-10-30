using _123Vendas.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Venda.Application.Commands;
using Venda.Application.Interfaces;
using Venda.Domain.Interfaces;

namespace Venda.Application.Handlers;

public class CancelarVendaHandler : IRequestHandler<CancelarVendaCommand, Result>
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IMediator _mediator;
    private readonly ILogger<CancelarVendaHandler> _logger;

    public CancelarVendaHandler(
        IVendaRepository vendaRepository,
        IIdempotencyStore idempotencyStore,
        IMediator mediator,
        ILogger<CancelarVendaHandler> logger)
    {
        _vendaRepository = vendaRepository;
        _idempotencyStore = idempotencyStore;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelarVendaCommand request, CancellationToken ct)
    {
        // 1. Verificar idempotência
        if (await _idempotencyStore.ExistsAsync(request.RequestId, ct))
        {
            _logger.LogInformation(
                "RequestId {RequestId} já processado. Cancelamento já realizado para VendaId: {VendaId}",
                request.RequestId, request.VendaId);
            return Result.Success();
        }

        // 2. Carregar venda
        var venda = await _vendaRepository.ObterPorIdAsync(request.VendaId, ct);
        if (venda == null)
        {
            _logger.LogWarning(
                "Venda {VendaId} não encontrada para cancelamento",
                request.VendaId);
            return Result.Failure($"Venda {request.VendaId} não encontrada.");
        }

        // 3. Cancelar venda (aplica regra de negócio)
        var resultado = venda.Cancelar();
        if (resultado.IsFailure)
        {
            _logger.LogWarning(
                "Falha ao cancelar venda {VendaId}: {Error}",
                request.VendaId, resultado.Error);
            return resultado;
        }

        // 4. Persistir alterações (que salva eventos no outbox)
        await _vendaRepository.AtualizarAsync(venda, ct);

        _logger.LogInformation(
            "Venda {VendaId} (Número: {NumeroVenda}) cancelada com sucesso",
            venda.Id, venda.NumeroVenda);

        // 5. Publicar eventos de domínio via MediatR
        foreach (var domainEvent in venda.DomainEvents)
        {
            await _mediator.Publish(domainEvent, ct);
        }

        venda.ClearDomainEvents();

        // 6. Salvar RequestId no IdempotencyStore
        await _idempotencyStore.SaveAsync(
            request.RequestId,
            nameof(CancelarVendaCommand),
            venda.Id,
            ct);

        return Result.Success();
    }
}
