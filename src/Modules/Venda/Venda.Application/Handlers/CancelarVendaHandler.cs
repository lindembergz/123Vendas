using _123Vendas.Shared.Common;
using _123Vendas.Shared.Exceptions;
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
    private readonly ILogger<CancelarVendaHandler> _logger;

    public CancelarVendaHandler(
        IVendaRepository vendaRepository,
        IIdempotencyStore idempotencyStore,
        ILogger<CancelarVendaHandler> logger)
    {
        _vendaRepository = vendaRepository;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelarVendaCommand request, CancellationToken ct)
    {
        //Verificar idempotência
        if (await _idempotencyStore.ExistsAsync(request.RequestId, ct))
        {
            _logger.LogInformation(
                "RequestId {RequestId} já processado. Cancelamento já realizado para VendaId: {VendaId}",
                request.RequestId, request.VendaId);
            return Result.Success();
        }

        //Carregar venda
        var venda = await _vendaRepository.ObterPorIdAsync(request.VendaId, ct);
        if (venda == null)
        {
            _logger.LogWarning(
                "Venda {VendaId} não encontrada para cancelamento",
                request.VendaId);
            throw new NotFoundException("Venda", request.VendaId);
        }

        //Cancelar venda (aplica regra de negócio)
        var resultado = venda.Cancelar();
        if (resultado.IsFailure)
        {
            _logger.LogWarning(
                "Falha ao cancelar venda {VendaId}: {Error}",
                request.VendaId, resultado.Error);
            return resultado;
        }

        //Persistir alterações (que salva eventos no outbox)
        //Nota: Eventos serão publicados assincronamente pelo OutboxProcessor
        await _vendaRepository.AtualizarAsync(venda, ct);

        _logger.LogInformation(
            "Venda {VendaId} (Número: {NumeroVenda}) cancelada com sucesso",
            venda.Id, venda.NumeroVenda);

        //Salvar RequestId no IdempotencyStore
        await _idempotencyStore.SaveAsync(
            request.RequestId,
            nameof(CancelarVendaCommand),
            venda.Id,
            ct);

        return Result.Success();
    }
}
