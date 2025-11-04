using _123Vendas.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Venda.Application.Commands;
using Venda.Application.DTOs;
using Venda.Application.Interfaces;
using Venda.Application.Services;
using Venda.Domain.Interfaces;

namespace Venda.Application.Handlers;

/// <summary>
/// Handler responsável por orquestrar a atualização de uma venda.
/// Implementa SRP: coordena operações sem conter lógica de negócio.
/// </summary>
public class AtualizarVendaHandler : IRequestHandler<AtualizarVendaCommand, Result<VendaDto>>
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IMediator _mediator;
    private readonly VendaUpdater _vendaUpdater;
    private readonly ILogger<AtualizarVendaHandler> _logger;

    public AtualizarVendaHandler(
        IVendaRepository vendaRepository,
        IIdempotencyStore idempotencyStore,
        IMediator mediator,
        VendaUpdater vendaUpdater,
        ILogger<AtualizarVendaHandler> logger)
    {
        _vendaRepository = vendaRepository;
        _idempotencyStore = idempotencyStore;
        _mediator = mediator;
        _vendaUpdater = vendaUpdater;
        _logger = logger;
    }

    public async Task<Result<VendaDto>> Handle(AtualizarVendaCommand request, CancellationToken ct)
    {
        try
        {
            //Verificar idempotência
            var idempotencyResult = await VerificarIdempotencia(request.RequestId, ct);
            if (idempotencyResult != null)
                return idempotencyResult;

            //Carregar venda existente
            var venda = await _vendaRepository.ObterPorIdAsync(request.VendaId, ct);
            if (venda == null)
            {
                _logger.LogWarning("Venda {VendaId} não encontrada", request.VendaId);
                return Result<VendaDto>.Failure($"Venda {request.VendaId} não encontrada.");
            }

            //Atualizar itens (delegado ao VendaUpdater)
            var updateResult = _vendaUpdater.AtualizarItens(venda, request.Itens);
            if (updateResult.IsFailure)
                return Result<VendaDto>.Failure(updateResult.Error!);

            //Persistir venda atualizada
            await _vendaRepository.AtualizarAsync(venda, ct);

            _logger.LogInformation(
                "Venda {VendaId} atualizada com sucesso. Número: {NumeroVenda}, Total de itens: {TotalItens}",
                venda.Id, venda.NumeroVenda, venda.Produtos.Count);

            //Publicar eventos de domínio
            await PublicarEventosDeDominio(venda, ct);

            //Salvar idempotência
            await _idempotencyStore.SaveAsync(
                request.RequestId,
                nameof(AtualizarVendaCommand),
                venda.Id,
                ct);

            //Retornar DTO
            var vendaDto = MapearParaDto(venda);
            return Result<VendaDto>.Success(vendaDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao atualizar venda {VendaId}", request.VendaId);
            return Result<VendaDto>.Failure("Erro interno ao atualizar venda.");
        }
    }

    private async Task<Result<VendaDto>?> VerificarIdempotencia(Guid requestId, CancellationToken ct)
    {
        if (!await _idempotencyStore.ExistsAsync(requestId, ct))
            return null;

        var aggregateId = await _idempotencyStore.GetAggregateIdAsync(requestId, ct);
        if (!aggregateId.HasValue)
            return null;

        _logger.LogInformation(
            "RequestId {RequestId} já processado. Retornando venda existente: {VendaId}",
            requestId, aggregateId.Value);

        var vendaExistente = await _vendaRepository.ObterPorIdAsync(aggregateId.Value, ct);
        if (vendaExistente == null)
            return null;

        return Result<VendaDto>.Success(MapearParaDto(vendaExistente));
    }

    private async Task PublicarEventosDeDominio(Domain.Aggregates.VendaAgregado venda, CancellationToken ct)
    {
        foreach (var domainEvent in venda.DomainEvents)
        {
            await _mediator.Publish(domainEvent, ct);
        }

        venda.ClearDomainEvents();
    }

    private static VendaDto MapearParaDto(Domain.Aggregates.VendaAgregado venda)
    {
        var itensDto = venda.Produtos.Select(p => new ItemVendaDto(
            p.ProdutoId,
            p.Quantidade,
            p.ValorUnitario,
            p.Desconto,
            p.Total
        )).ToList();

        return new VendaDto(
            venda.Id,
            venda.NumeroVenda,
            venda.Data,
            venda.ClienteId,
            venda.FilialId,
            venda.ValorTotal,
            venda.Status.ToString(),
            itensDto
        );
    }
}
