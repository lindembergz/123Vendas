using _123Vendas.Shared.Common;
using _123Vendas.Shared.Interfaces;
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
    private readonly IPoliticaDesconto _politicaDesconto;

    public CriarVendaHandler(
        IVendaRepository vendaRepository,
        IIdempotencyStore idempotencyStore,
        IClienteService clienteService,
        IMediator mediator,
        ILogger<CriarVendaHandler> logger,
        IPoliticaDesconto politicaDesconto)
    {
        _vendaRepository = vendaRepository;
        _idempotencyStore = idempotencyStore;
        _clienteService = clienteService;
        _mediator = mediator;
        _logger = logger;
        _politicaDesconto = politicaDesconto;
    }

    public async Task<Result<Guid>> Handle(CriarVendaCommand request, CancellationToken ct)
    {
        //Verificar idempotência
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

        //Validar cliente via IClienteService
        try
        {
            var clienteValido = await _clienteService.ClienteExisteAsync(request.ClienteId, ct);
            
            if (!clienteValido)
            {
                _logger.LogWarning("Cliente {ClienteId} não encontrado no CRM", request.ClienteId);
                return Result<Guid>.Failure($"Cliente {request.ClienteId} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar cliente {ClienteId} no CRM", request.ClienteId);
            return Result<Guid>.Failure("Serviço de validação de cliente indisponível. Tente novamente mais tarde.");
        }

        //Criar VendaAgregado com injeção da política de desconto
        var venda = VendaAgregado.Criar(request.ClienteId, request.FilialId, _politicaDesconto);

        //Adicionar itens
        foreach (var itemDto in request.Itens)
        {
            var item = new ItemVenda(
                itemDto.ProdutoId,
                itemDto.Quantidade,
                itemDto.ValorUnitario,
                0m); //Desconto será calculado pelo agregado

            var resultado = venda.AdicionarItem(item);
            if (resultado.IsFailure)
            {
                _logger.LogWarning(
                    "Falha ao adicionar item {ProdutoId} à venda: {Error}",
                    itemDto.ProdutoId, resultado.Error);
                return Result<Guid>.Failure(resultado.Error!);
            }
        }

        //Persistir venda (que salva eventos no outbox)
        await _vendaRepository.AdicionarAsync(venda, ct);

        _logger.LogInformation(
            "Venda {VendaId} criada com sucesso. Número: {NumeroVenda}, Cliente: {ClienteId}, Status: {Status}",
            venda.Id, venda.NumeroVenda, venda.ClienteId, venda.Status);

        //Publicar eventos de domínio via MediatR
        foreach (var domainEvent in venda.DomainEvents)
        {
            await _mediator.Publish(domainEvent, ct);
        }

        venda.ClearDomainEvents();

        //Salvar RequestId no IdempotencyStore
        await _idempotencyStore.SaveAsync(
            request.RequestId,
            nameof(CriarVendaCommand),
            venda.Id,
            ct);

        return Result<Guid>.Success(venda.Id);
    }
}
