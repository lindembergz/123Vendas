using _123Vendas.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Venda.Application.Commands;
using Venda.Application.DTOs;
using Venda.Application.Interfaces;
using Venda.Domain.Interfaces;
using Venda.Domain.ValueObjects;

namespace Venda.Application.Handlers;

public class AtualizarVendaHandler : IRequestHandler<AtualizarVendaCommand, Result<VendaDto>>
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IMediator _mediator;
    private readonly ILogger<AtualizarVendaHandler> _logger;

    public AtualizarVendaHandler(
        IVendaRepository vendaRepository,
        IIdempotencyStore idempotencyStore,
        IMediator mediator,
        ILogger<AtualizarVendaHandler> logger)
    {
        _vendaRepository = vendaRepository;
        _idempotencyStore = idempotencyStore;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<VendaDto>> Handle(AtualizarVendaCommand request, CancellationToken ct)
    {
        // 1. Verificar idempotência
        if (await _idempotencyStore.ExistsAsync(request.RequestId, ct))
        {
            var aggregateId = await _idempotencyStore.GetAggregateIdAsync(request.RequestId, ct);
            if (aggregateId.HasValue)
            {
                _logger.LogInformation(
                    "RequestId {RequestId} já processado. Retornando venda existente: {VendaId}",
                    request.RequestId, aggregateId.Value);
                
                var vendaExistente = await _vendaRepository.ObterPorIdAsync(aggregateId.Value, ct);
                if (vendaExistente != null)
                {
                    return Result<VendaDto>.Success(MapearParaDto(vendaExistente));
                }
            }
        }

        // 2. Carregar venda existente
        var venda = await _vendaRepository.ObterPorIdAsync(request.VendaId, ct);
        if (venda == null)
        {
            _logger.LogWarning("Venda {VendaId} não encontrada", request.VendaId);
            return Result<VendaDto>.Failure($"Venda {request.VendaId} não encontrada.");
        }

        // 3. Remover todos os itens existentes
        var produtosParaRemover = venda.Produtos.Select(p => p.ProdutoId).ToList();
        foreach (var produtoId in produtosParaRemover)
        {
            var resultadoRemocao = venda.RemoverItem(produtoId);
            if (resultadoRemocao.IsFailure)
            {
                _logger.LogWarning(
                    "Falha ao remover item {ProdutoId} da venda {VendaId}: {Error}",
                    produtoId, request.VendaId, resultadoRemocao.Error);
                return Result<VendaDto>.Failure(resultadoRemocao.Error!);
            }
        }

        // 4. Adicionar novos itens
        foreach (var itemDto in request.Itens)
        {
            var item = new ItemVenda(
                itemDto.ProdutoId,
                itemDto.Quantidade,
                itemDto.ValorUnitario,
                0m); // Desconto será calculado pelo agregado

            var resultado = venda.AdicionarItem(item);
            if (resultado.IsFailure)
            {
                _logger.LogWarning(
                    "Falha ao adicionar item {ProdutoId} à venda {VendaId}: {Error}",
                    itemDto.ProdutoId, request.VendaId, resultado.Error);
                return Result<VendaDto>.Failure(resultado.Error!);
            }
        }

        // 5. Persistir venda atualizada
        await _vendaRepository.AtualizarAsync(venda, ct);

        _logger.LogInformation(
            "Venda {VendaId} atualizada com sucesso. Número: {NumeroVenda}, Total de itens: {TotalItens}",
            venda.Id, venda.NumeroVenda, venda.Produtos.Count);

        // 6. Publicar eventos de domínio via MediatR
        foreach (var domainEvent in venda.DomainEvents)
        {
            await _mediator.Publish(domainEvent, ct);
        }

        venda.ClearDomainEvents();

        // 7. Salvar RequestId no IdempotencyStore
        await _idempotencyStore.SaveAsync(
            request.RequestId,
            nameof(AtualizarVendaCommand),
            venda.Id,
            ct);

        // 8. Mapear para DTO e retornar
        var vendaDto = MapearParaDto(venda);
        return Result<VendaDto>.Success(vendaDto);
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

        // Converte Filial string para Guid (assumindo que é um Guid em formato string)
        Guid filialId = Guid.TryParse(venda.Filial, out var parsedFilialId) 
            ? parsedFilialId 
            : Guid.Empty;

        return new VendaDto(
            venda.Id,
            venda.NumeroVenda,
            venda.Data,
            venda.ClienteId,
            filialId,
            venda.ValorTotal,
            venda.Status.ToString(),
            itensDto
        );
    }
}
