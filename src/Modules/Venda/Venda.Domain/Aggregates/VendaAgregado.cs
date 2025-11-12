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

public class VendaAgregado : IAggregateRoot
{
    private const int MAX_ITENS_VENDA = 20;
    private const decimal MAX_VALOR_UNITARIO = 999999.99m;
    
    private readonly IPoliticaDesconto _politicaDesconto;
    private readonly List<IItemVendaSpecification> _specifications;
    
    public Guid Id { get; private set; } = Guid.NewGuid();
    public int NumeroVenda { get; private set; }
    public DateTime Data { get; private set; } = DateTime.UtcNow;
    public Guid ClienteId { get; private set; }
    public Guid FilialId { get; private set; }
    public StatusVenda Status { get; private set; } = StatusVenda.Ativa;
    
    private readonly List<ItemVenda> _produtos = new();
    public IReadOnlyList<ItemVenda> Produtos => _produtos.AsReadOnly();
    
    public decimal ValorTotal => _produtos.Sum(i => i.Total);
    
    // Domain Events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Construtor com injeção de dependência.
    /// </summary>
    /// <param name="politicaDesconto">Política de desconto para cálculo de descontos</param>
    public VendaAgregado(IPoliticaDesconto politicaDesconto)
    {
        _politicaDesconto = politicaDesconto ?? throw new ArgumentNullException(nameof(politicaDesconto));
        _specifications = new List<IItemVendaSpecification>
        {
            new VendaAtivaSpecification(),
            new ItemVendaDadosValidosSpecification(),
            new QuantidadeDentroDosLimitesSpecification(_politicaDesconto)
        };
    }
    
    /// <summary>
    /// Construtor privado para EF Core.
    /// </summary>
    private VendaAgregado() : this(new PoliticaDesconto()) 
    {
        //EF Core usa este construtor
    }
    
    /// <summary>
    /// Cria uma nova venda.
    /// </summary>
    /// <param name="clienteId">ID do cliente</param>
    /// <param name="filialId">ID da filial</param>
    /// <param name="politicaDesconto">Política de desconto</param>
    /// <returns>Nova instância de VendaAgregado</returns>
    /// <exception cref="DomainException">Se clienteId ou filialId forem inválidos</exception>
    public static VendaAgregado Criar(Guid clienteId, Guid filialId, IPoliticaDesconto politicaDesconto)
    {
        if (clienteId == Guid.Empty)
            throw new DomainException("ClienteId é obrigatório.", "VENDA_CLIENTE_INVALIDO");
        
        if (filialId == Guid.Empty)
            throw new DomainException("FilialId é obrigatório.", "VENDA_FILIAL_INVALIDA");
        
        var venda = new VendaAgregado(politicaDesconto)
        {
            ClienteId = clienteId,
            FilialId = filialId,
            NumeroVenda = 0 //Será definido pelo repositório após persistência
        };
        
        //Evento será adicionado após definir NumeroVenda no repositório
        return venda;
    }
    
    /// <summary>
    /// Define o número da venda após persistência.
    /// </summary>
    /// <param name="numeroVenda">Número sequencial da venda</param>
    public void DefinirNumeroVenda(int numeroVenda)
    {
        NumeroVenda = numeroVenda;
        AddDomainEvent(new CompraCriada(Id, NumeroVenda, ClienteId));
    }
    
    /// <summary>
    /// Adiciona um item à venda, consolidando itens do mesmo produto.
    /// Aplica validações via specifications e calcula desconto automaticamente.
    /// </summary>
    /// <param name="item">Item a ser adicionado</param>
    /// <returns>Result.Success se adicionado com sucesso, Result.Failure caso contrário</returns>
    public Result AdicionarItem(ItemVenda item)
    {
        // Executa todas as specifications
        foreach (var specification in _specifications)
        {
            var result = specification.IsSatisfiedBy(item, this);
            if (result.IsFailure)
                return result;
        }
        
        // Consolida ou adiciona item
        return ConsolidarOuAdicionarItem(item);
    }
    
    /// <summary>
    /// Consolida ou adiciona um item à venda.
    /// Se o produto já existe, consolida as quantidades; caso contrário, adiciona novo item.
    /// </summary>
    /// <param name="item">Item a ser consolidado ou adicionado</param>
    /// <returns>Result.Success</returns>
    private Result ConsolidarOuAdicionarItem(ItemVenda item)
    {
        var quantidadeExistente = ObterQuantidadeTotalPorProduto(item.ProdutoId);
        var quantidadeTotal = quantidadeExistente + item.Quantidade;
        
        // Calcula desconto usando a política centralizada
        var desconto = _politicaDesconto.Calcular(quantidadeTotal);
        
        // Consolida itens do mesmo produto em uma única linha
        var itemExistente = _produtos.FirstOrDefault(i => i.ProdutoId == item.ProdutoId);
        if (itemExistente != null)
        {
            // Remove o item existente e adiciona com quantidade e desconto atualizados
            _produtos.Remove(itemExistente);
            var itemConsolidado = new ItemVenda(
                item.ProdutoId,
                quantidadeTotal,
                item.ValorUnitario,
                desconto
            );
            _produtos.Add(itemConsolidado);
        }
        else
        {
            // Novo produto: adiciona com desconto calculado
            var itemComDesconto = new ItemVenda(
                item.ProdutoId,
                item.Quantidade,
                item.ValorUnitario,
                desconto
            );
            _produtos.Add(itemComDesconto);
        }
        
        // Adiciona evento de alteração
        AddDomainEvent(new CompraAlterada(Id, new[] { item.ProdutoId }));
        
        return Result.Success();
    }
    
    /// <summary>
    /// Cancela a venda.
    /// </summary>
    /// <returns>Result.Success se cancelada, Result.Failure se já estava cancelada</returns>
    public Result Cancelar()
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Venda já está cancelada.");
        
        Status = StatusVenda.Cancelada;
        AddDomainEvent(new CompraCancelada(Id, "Cancelado pelo usuário"));
        
        return Result.Success();
    }
    
    /// <summary>
    /// Remove uma quantidade específica de um item da venda.
    /// </summary>
    /// <param name="produtoId">ID do produto</param>
    /// <param name="quantidade">Quantidade a remover</param>
    /// <returns>Result.Success se removido, Result.Failure caso contrário</returns>
    public Result RemoverItem(Guid produtoId, int quantidade)
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível remover itens de uma venda cancelada.");
        
        if (quantidade <= 0)
            return Result.Failure("Quantidade a remover deve ser maior que zero.");
        
        var item = _produtos.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (item == null)
            return Result.Failure($"Produto {produtoId} não encontrado na venda.");
        
        if (quantidade > item.Quantidade)
            return Result.Failure($"Quantidade a remover ({quantidade}) é maior que a quantidade disponível ({item.Quantidade}).");
        
        var novaQuantidade = item.Quantidade - quantidade;
        
        if (novaQuantidade == 0)
        {
            //Remove o item completamente
            _produtos.Remove(item);
            AddDomainEvent(new ItemCancelado(Id, produtoId));
        }
        else
        {
            //Atualiza a quantidade e recalcula o desconto
            var novoDesconto = _politicaDesconto.Calcular(novaQuantidade);
            _produtos.Remove(item);
            _produtos.Add(new ItemVenda(
                item.ProdutoId,
                novaQuantidade,
                item.ValorUnitario,
                novoDesconto
            ));
            AddDomainEvent(new CompraAlterada(Id, new[] { produtoId }));
        }
        
        return Result.Success();
    }
    
    /// <summary>
    /// Remove completamente um item da venda (todas as quantidades).
    /// </summary>
    /// <param name="produtoId">ID do produto</param>
    /// <returns>Result.Success se removido, Result.Failure caso contrário</returns>
    public Result RemoverItem(Guid produtoId)
    {
        //Remove o item completamente (todas as quantidades)
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível remover itens de uma venda cancelada.");
        
        var item = _produtos.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (item == null)
            return Result.Failure($"Produto {produtoId} não encontrado na venda.");
        
        _produtos.Remove(item);
        AddDomainEvent(new ItemCancelado(Id, produtoId));
        
        return Result.Success();
    }
    

    
    /// <summary>
    /// Atualiza os itens da venda de forma inteligente:
    /// - Consolida itens duplicados (mesmo ProdutoId) somando suas quantidades
    /// - Remove itens que não estão mais na lista
    /// - Adiciona novos itens
    /// - Ajusta quantidades de itens existentes
    /// </summary>
    public Result AtualizarItens(IReadOnlyList<ItemVenda> novosItens)
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível atualizar itens de uma venda cancelada.");
        
        // Consolida itens duplicados antes de processar
        var itensConsolidados = ConsolidarItensDuplicados(novosItens);
        
        var produtosNovos = itensConsolidados.Select(i => i.ProdutoId).ToHashSet();
        var produtosExistentes = _produtos.Select(p => p.ProdutoId).ToHashSet();
        
        // Remove itens que não estão mais na lista
        var produtosParaRemover = produtosExistentes.Except(produtosNovos).ToList();
        foreach (var produtoId in produtosParaRemover)
        {
            var result = RemoverItem(produtoId);
            if (result.IsFailure)
                return result;
        }
        
        // Atualiza ou adiciona itens
        foreach (var item in itensConsolidados)
        {
            var result = AtualizarOuAdicionarItem(item);
            if (result.IsFailure)
                return result;
        }
        
        return Result.Success();
    }
    
    /// <summary>
    /// Consolida itens com o mesmo ProdutoId, somando suas quantidades.
    /// </summary>
    private List<ItemVenda> ConsolidarItensDuplicados(IReadOnlyList<ItemVenda> itens)
    {
        return itens
            .GroupBy(i => i.ProdutoId)
            .Select(g => new ItemVenda(
                g.Key,
                g.Sum(x => x.Quantidade),
                g.First().ValorUnitario,
                0m // Desconto será recalculado pelo agregado
            ))
            .ToList();
    }
    
    /// <summary>
    /// Atualiza ou adiciona um item na venda.
    /// </summary>
    private Result AtualizarOuAdicionarItem(ItemVenda novoItem)
    {
        var itemExistente = _produtos.FirstOrDefault(p => p.ProdutoId == novoItem.ProdutoId);
        
        if (itemExistente == null)
        {
            // Novo item: adiciona
            return AdicionarItem(novoItem);
        }
        
        // Item existente: ajusta quantidade
        var diferenca = novoItem.Quantidade - itemExistente.Quantidade;
        
        if (diferenca == 0)
        {
            // Quantidade não mudou: nada a fazer
            return Result.Success();
        }
        
        if (diferenca > 0)
        {
            // Aumentar quantidade: adicionar mais unidades
            var itemAdicional = new ItemVenda(
                novoItem.ProdutoId,
                diferenca,
                novoItem.ValorUnitario,
                0m);
            return AdicionarItem(itemAdicional);
        }
        
        // Diminuir quantidade: remover unidades
        return RemoverItem(novoItem.ProdutoId, Math.Abs(diferenca));
    }
    
    /// <summary>
    /// Valida os invariantes do agregado.
    /// </summary>
    /// <returns>Result.Success se válido, Result.Failure caso contrário</returns>
    private Result ValidarInvariantes()
    {
        // Venda ativa não pode ter zero itens (após inicialização)
        if (Status == StatusVenda.Ativa && NumeroVenda > 0 && _produtos.Count == 0)
            return Result.Failure("Venda ativa deve ter pelo menos um item.");
        
        // Valor total não pode ser negativo
        if (ValorTotal < 0)
            return Result.Failure("Valor total não pode ser negativo.");
        
        // Itens não podem ter quantidade zero ou negativa
        if (_produtos.Any(p => p.Quantidade <= 0))
            return Result.Failure("Itens não podem ter quantidade zero ou negativa.");
        
        // Não pode haver produtos duplicados (deve estar consolidado)
        var produtosDuplicados = _produtos.GroupBy(p => p.ProdutoId).Any(g => g.Count() > 1);
        if (produtosDuplicados)
            return Result.Failure("Não pode haver produtos duplicados na venda.");
        
        return Result.Success();
    }
    
    private int ObterQuantidadeTotalPorProduto(Guid produtoId)
        => _produtos.Where(i => i.ProdutoId == produtoId).Sum(i => i.Quantidade);
    
    private void AddDomainEvent(IDomainEvent @event)
    {
        _domainEvents.Add(@event);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
