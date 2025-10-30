using _123Vendas.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Venda.Application.Commands;
using Venda.Application.Interfaces;
using Venda.Domain.Aggregates;
using Venda.Domain.Interfaces;
using Venda.Domain.ValueObjects;

namespace Venda.Application.Handlers;

public class CriarVendaHandler : IRequestHandler<CriarVendaCommand, Result<Guid>>
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IClienteService _clienteService;
    private readonly IMediator _mediator;
    private readonly ILogger<CriarVendaHandler> _logger;

    public CriarVendaHandler(
        IVendaRepository vendaRepository,
        IIdempotencyStore idempotencyStore,
        IClienteService clienteService,
        IMediator mediator,
        ILogger<CriarVendaHandler> logger)
    {
        _vendaRepository = vendaRepository;
        _idempotencyStore = idempotencyStore;
        _clienteService = clienteService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CriarVendaCommand request, CancellationToken ct)
    {
        // 1. Verificar idempotência
        if (await _idempotencyStore.ExistsAsync(request.RequestId, ct))
        {
            var aggregateId = await _idempotencyStore.GetAggregateIdAsync(request.RequestId, ct);
            if (aggregateId.HasValue)
            {
                _logger.LogInformation(
                    "RequestId {RequestId} já processado. Retornando VendaId existente: {VendaId}",
                    request.RequestId, aggregateId.Value);
                return Result<Guid>.Success(aggregateId.Value);
            }
        }

        // 2. Validar cliente via IClienteService com fallback
        bool clienteValido = false;
        try
        {
            clienteValido = await _clienteService.ClienteExisteAsync(request.ClienteId, ct);
            
            if (!clienteValido)
            {
                _logger.LogWarning(
                    "Cliente {ClienteId} não encontrado no CRM. Criando venda com status PendenteValidacao",
                    request.ClienteId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Erro ao validar cliente {ClienteId} no CRM. Aplicando fallback: criando venda com status PendenteValidacao",
                request.ClienteId);
            clienteValido = false;
        }

        // 3. Criar VendaAgregado
        // Nota: FilialId é convertido para string pois o agregado atual usa string Filial
        var venda = VendaAgregado.Criar(request.ClienteId, request.FilialId.ToString());

        // Se cliente não foi validado, marcar como pendente
        if (!clienteValido)
        {
            venda.MarcarComoPendenteValidacao();
        }

        // 4. Adicionar itens
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
                    "Falha ao adicionar item {ProdutoId} à venda: {Error}",
                    itemDto.ProdutoId, resultado.Error);
                return Result<Guid>.Failure(resultado.Error!);
            }
        }

        // 5. Persistir venda (que salva eventos no outbox)
        await _vendaRepository.AdicionarAsync(venda, ct);

        _logger.LogInformation(
            "Venda {VendaId} criada com sucesso. Número: {NumeroVenda}, Cliente: {ClienteId}, Status: {Status}",
            venda.Id, venda.NumeroVenda, venda.ClienteId, venda.Status);

        // 6. Publicar eventos de domínio via MediatR
        foreach (var domainEvent in venda.DomainEvents)
        {
            await _mediator.Publish(domainEvent, ct);
        }

        venda.ClearDomainEvents();

        // 7. Salvar RequestId no IdempotencyStore
        await _idempotencyStore.SaveAsync(
            request.RequestId,
            nameof(CriarVendaCommand),
            venda.Id,
            ct);

        return Result<Guid>.Success(venda.Id);
    }
}
