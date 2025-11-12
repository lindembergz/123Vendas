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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var totalItens = request.Itens.Count;
        var valorTotal = request.Itens.Sum(i => i.Quantidade * i.ValorUnitario);

        // Log estruturado: Início da transação crítica
        _logger.LogInformation(
            "[TRANSACAO_CRITICA] Iniciando criação de venda. RequestId: {RequestId}, ClienteId: {ClienteId}, FilialId: {FilialId}, TotalItens: {TotalItens}, ValorTotal: {ValorTotal:C}",
            request.RequestId,
            request.ClienteId,
            request.FilialId,
            totalItens,
            valorTotal);

        //Verificar idempotência
        if (await _idempotencyStore.ExistsAsync(request.RequestId, ct))
        {
            var aggregateId = await _idempotencyStore.GetAggregateIdAsync(request.RequestId, ct);
            if (aggregateId.HasValue)
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "[IDEMPOTENCIA] RequestId {RequestId} já processado. VendaId: {VendaId}, Duracao: {DuracaoMs}ms",
                    request.RequestId,
                    aggregateId.Value,
                    stopwatch.ElapsedMilliseconds);
                return Result<Guid>.Success(aggregateId.Value);
            }
        }

        //Validar cliente via IClienteService
        try
        {
            var clienteValido = await _clienteService.ClienteExisteAsync(request.ClienteId, ct);
            
            if (!clienteValido)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[VALIDACAO_FALHOU] Cliente não encontrado. ClienteId: {ClienteId}, Duracao: {DuracaoMs}ms",
                    request.ClienteId,
                    stopwatch.ElapsedMilliseconds);
                return Result<Guid>.Failure($"Cliente {request.ClienteId} não encontrado.");
            }

            _logger.LogInformation(
                "[VALIDACAO_OK] Cliente validado com sucesso. ClienteId: {ClienteId}",
                request.ClienteId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[ERRO_INTEGRACAO] Erro ao validar cliente no CRM. ClienteId: {ClienteId}, Duracao: {DuracaoMs}ms",
                request.ClienteId,
                stopwatch.ElapsedMilliseconds);
            return Result<Guid>.Failure("Serviço de validação de cliente indisponível. Tente novamente mais tarde.");
        }

        //Criar VendaAgregado com injeção da política de desconto
        var venda = VendaAgregado.Criar(request.ClienteId, request.FilialId, _politicaDesconto);

        //Adicionar itens
        var itensAdicionados = 0;
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
                stopwatch.Stop();
                _logger.LogWarning(
                    "[ITEM_REJEITADO] Falha ao adicionar item. ProdutoId: {ProdutoId}, Quantidade: {Quantidade}, Erro: {Error}, Duracao: {DuracaoMs}ms",
                    itemDto.ProdutoId,
                    itemDto.Quantidade,
                    resultado.Error,
                    stopwatch.ElapsedMilliseconds);
                return Result<Guid>.Failure(resultado.Error!);
            }
            itensAdicionados++;
        }

        //Persistir venda (que salva eventos no outbox)
        //Nota: Eventos serão publicados assincronamente pelo OutboxProcessor
        await _vendaRepository.AdicionarAsync(venda, ct);

        //Salvar RequestId no IdempotencyStore
        await _idempotencyStore.SaveAsync(
            request.RequestId,
            nameof(CriarVendaCommand),
            venda.Id,
            ct);

        stopwatch.Stop();

        // Log estruturado: Sucesso da transação crítica com métricas
        _logger.LogInformation(
            "[TRANSACAO_SUCESSO] Venda criada com sucesso. VendaId: {VendaId}, NumeroVenda: {NumeroVenda}, ClienteId: {ClienteId}, FilialId: {FilialId}, Status: {Status}, ItensAdicionados: {ItensAdicionados}, ValorTotal: {ValorTotal:C}, Duracao: {DuracaoMs}ms",
            venda.Id,
            venda.NumeroVenda,
            venda.ClienteId,
            venda.FilialId,
            venda.Status,
            itensAdicionados,
            venda.ValorTotal,
            stopwatch.ElapsedMilliseconds);

        return Result<Guid>.Success(venda.Id);
    }
}
