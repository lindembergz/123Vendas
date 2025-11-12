using _123Vendas.Shared.Common;
using _123Vendas.Shared.Events;
using _123Vendas.Shared.Exceptions;
using _123Vendas.Shared.Interfaces;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.Services;
using Venda.Domain.Specifications;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Aggregates;

/// <summary>
/// Agregado raiz que representa uma venda.
/// Responsável por garantir os invariantes de negócio e emitir eventos de domínio.
/// </summary>
public class VendaAgregado : IAggregateRoot
{
    private const int MAX_ITENS_VENDA = 20;
    private const decimal MAX_VALOR_UNITARIO = 999_999.99m;

    private readonly IPoliticaDesconto _politicaDesconto;
    private readonly List<IItemVendaSpecification> _specifications;
    private readonly List<ItemVenda> _produtos = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public int NumeroVenda { get; private set; }
    public DateTime Data { get; private set; } = DateTime.UtcNow;
    public Guid ClienteId { get; private set; }
    public Guid FilialId { get; private set; }
    public StatusVenda Status { get; private set; } = StatusVenda.Ativa;

    public IReadOnlyList<ItemVenda> Produtos => _produtos.AsReadOnly();
    public decimal ValorTotal => _produtos.Sum(i => i.Total);
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    #region Construtores

    /// <summary>
    /// Construtor com injeção de dependência.
    /// </summary>
    public VendaAgregado(IPoliticaDesconto politicaDesconto)
    {
        _politicaDesconto = politicaDesconto ?? throw new ArgumentNullException(nameof(politicaDesconto));

        _specifications = new()
        {
            new VendaAtivaSpecification(),
            new ItemVendaDadosValidosSpecification(),
            new QuantidadeDentroDosLimitesSpecification(_politicaDesconto)
        };
    }

    /// <summary>
    /// Construtor privado para uso do EF Core.
    /// </summary>
    private VendaAgregado() : this(new PoliticaDesconto()) { }

    #endregion

    #region Fábricas

    /// <summary>
    /// Cria uma nova venda garantindo invariantes mínimos.
    /// </summary>
    public static VendaAgregado Criar(Guid clienteId, Guid filialId, IPoliticaDesconto politicaDesconto)
    {
        if (clienteId == Guid.Empty)
            throw new DomainException("ClienteId é obrigatório.", "VENDA_CLIENTE_INVALIDO");

        if (filialId == Guid.Empty)
            throw new DomainException("FilialId é obrigatório.", "VENDA_FILIAL_INVALIDA");

        return new VendaAgregado(politicaDesconto)
        {
            ClienteId = clienteId,
            FilialId = filialId
        };
    }

    #endregion

    #region Comportamentos principais

    public void DefinirNumeroVenda(int numeroVenda)
    {
        NumeroVenda = numeroVenda;
        AddDomainEvent(new CompraCriada(Id, NumeroVenda, ClienteId));
    }

    public Result AdicionarItem(ItemVenda item)
    {
        foreach (var spec in _specifications)
        {
            var validation = spec.IsSatisfiedBy(item, this);
            if (validation.IsFailure)
                return validation;
        }

        return ConsolidarOuAdicionarItem(item);
    }

    private Result ConsolidarOuAdicionarItem(ItemVenda item)
    {
        var quantidadeTotal = ObterQuantidadeTotalPorProduto(item.ProdutoId) + item.Quantidade;
        var desconto = _politicaDesconto.Calcular(quantidadeTotal);

        var itemExistente = _produtos.FirstOrDefault(i => i.ProdutoId == item.ProdutoId);
        var itemAtualizado = new ItemVenda(item.ProdutoId, quantidadeTotal, item.ValorUnitario, desconto);

        if (itemExistente != null)
            _produtos.Remove(itemExistente);

        _produtos.Add(itemAtualizado);

        AddDomainEvent(new CompraAlterada(Id, new[] { item.ProdutoId }));
        return Result.Success();
    }

    public Result RemoverItem(Guid produtoId, int quantidade)
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível remover itens de uma venda cancelada.");

        if (quantidade <= 0)
            return Result.Failure("Quantidade deve ser maior que zero.");

        var item = _produtos.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (item is null)
            return Result.Failure($"Produto {produtoId} não encontrado.");

        if (quantidade > item.Quantidade)
            return Result.Failure($"Quantidade ({quantidade}) maior que disponível ({item.Quantidade}).");

        var novaQuantidade = item.Quantidade - quantidade;
        _produtos.Remove(item);

        if (novaQuantidade == 0)
        {
            _produtos.Remove(item);
            AddDomainEvent(new ItemCancelado(Id, produtoId));
        }
        else
        {
            var novoDesconto = _politicaDesconto.Calcular(novaQuantidade);
            _produtos.Remove(item);
            _produtos.Add(new ItemVenda(item.ProdutoId, novaQuantidade, item.ValorUnitario, novoDesconto));
            AddDomainEvent(new CompraAlterada(Id, new[] { produtoId }));
        }

        return Result.Success();
    }

    public Result RemoverItem(Guid produtoId)
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível remover itens de uma venda cancelada.");

        var item = _produtos.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (item == null)
            return Result.Failure($"Produto {produtoId} não encontrado.");

        _produtos.Remove(item);
        AddDomainEvent(new ItemCancelado(Id, produtoId));

        return Result.Success();
    }

    public Result AtualizarItens(IReadOnlyList<ItemVenda> novosItens)
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível atualizar itens de uma venda cancelada.");

        var itensConsolidados = ConsolidarItensDuplicados(novosItens);

        var produtosNovos = itensConsolidados.Select(i => i.ProdutoId).ToHashSet();
        var produtosExistentes = _produtos.Select(p => p.ProdutoId).ToHashSet();

        // Remove itens ausentes
        foreach (var produtoId in produtosExistentes.Except(produtosNovos))
        {
            var result = RemoverItem(produtoId);
            if (result.IsFailure) return result;
        }

        // Atualiza ou adiciona
        foreach (var item in itensConsolidados)
        {
            var result = AtualizarOuAdicionarItem(item);
            if (result.IsFailure) return result;
        }

        return Result.Success();
    }

    public Result Cancelar()
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Venda já está cancelada.");

        Status = StatusVenda.Cancelada;
        AddDomainEvent(new CompraCancelada(Id, "Cancelado pelo usuário"));

        return Result.Success();
    }

    #endregion

    #region Métodos auxiliares

    private Result AtualizarOuAdicionarItem(ItemVenda novoItem)
    {
        var existente = _produtos.FirstOrDefault(p => p.ProdutoId == novoItem.ProdutoId);
        if (existente == null)
            return AdicionarItem(novoItem);

        var diferenca = novoItem.Quantidade - existente.Quantidade;
        if (diferenca == 0) return Result.Success();

        return diferenca > 0
            ? AdicionarItem(new ItemVenda(novoItem.ProdutoId, diferenca, novoItem.ValorUnitario, 0m))
            : RemoverItem(novoItem.ProdutoId, Math.Abs(diferenca));
    }

    private static List<ItemVenda> ConsolidarItensDuplicados(IEnumerable<ItemVenda> itens)
        => itens
            .GroupBy(i => i.ProdutoId)
            .Select(g => new ItemVenda(
                g.Key,
                g.Sum(x => x.Quantidade),
                g.First().ValorUnitario,
                0m))
            .ToList();

    private int ObterQuantidadeTotalPorProduto(Guid produtoId)
        => _produtos.Where(i => i.ProdutoId == produtoId).Sum(i => i.Quantidade);

    private Result ValidarInvariantes()
    {
        if (Status == StatusVenda.Ativa && NumeroVenda > 0 && !_produtos.Any())
            return Result.Failure("Venda ativa deve ter ao menos um item.");

        if (ValorTotal < 0)
            return Result.Failure("Valor total não pode ser negativo.");

        if (_produtos.Any(p => p.Quantidade <= 0))
            return Result.Failure("Itens não podem ter quantidade zero ou negativa.");

        if (_produtos.GroupBy(p => p.ProdutoId).Any(g => g.Count() > 1))
            return Result.Failure("Itens duplicados detectados na venda.");

        return Result.Success();
    }

    private void AddDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();

    #endregion
}
