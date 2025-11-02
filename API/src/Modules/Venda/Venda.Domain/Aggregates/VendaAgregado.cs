using _123Vendas.Shared.Common;
using _123Vendas.Shared.Events;
using _123Vendas.Shared.Interfaces;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Aggregates;

public class VendaAgregado : IAggregateRoot
{
    private readonly IPoliticaDesconto _politicaDesconto;
    
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
    
    // Construtor privado para EF Core
    private VendaAgregado() 
    {
        _politicaDesconto = null!; // EF Core irá definir via reflection
    }
    
    // Construtor com injeção de dependência
    public VendaAgregado(IPoliticaDesconto politicaDesconto)
    {
        _politicaDesconto = politicaDesconto ?? throw new ArgumentNullException(nameof(politicaDesconto));
    }
    
    public static VendaAgregado Criar(Guid clienteId, Guid filialId, IPoliticaDesconto politicaDesconto)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("ClienteId é obrigatório.", nameof(clienteId));
        
        if (filialId == Guid.Empty)
            throw new ArgumentException("FilialId é obrigatório.", nameof(filialId));
        
        var venda = new VendaAgregado(politicaDesconto)
        {
            ClienteId = clienteId,
            FilialId = filialId,
            NumeroVenda = 0 // Será definido pelo repositório após persistência
        };
        
        // Evento será adicionado após definir NumeroVenda no repositório
        return venda;
    }
    
    public void DefinirNumeroVenda(int numeroVenda)
    {
        NumeroVenda = numeroVenda;
        AddDomainEvent(new CompraCriada(Id, NumeroVenda, ClienteId));
    }
    
    public Result AdicionarItem(ItemVenda item)
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível adicionar itens a uma venda cancelada.");
        
        if (item.ProdutoId == Guid.Empty)
            return Result.Failure("ProdutoId é obrigatório.");
        
        if (item.Quantidade <= 0)
            return Result.Failure("Quantidade deve ser maior que zero.");
        
        if (item.ValorUnitario <= 0 || item.ValorUnitario > 999999.99m)
            return Result.Failure("Valor unitário deve ser maior que zero e menor que 999999.99.");
        
        var quantidadeExistente = ObterQuantidadeTotalPorProduto(item.ProdutoId);
        var quantidadeTotal = quantidadeExistente + item.Quantidade;
        
        // Valida usando a política de desconto centralizada
        if (!_politicaDesconto.PermiteVenda(quantidadeTotal))
            return Result.Failure("Não é permitido vender mais de 20 unidades do mesmo produto.");
        
        // Calcula desconto usando a política centralizada
        var desconto = _politicaDesconto.Calcular(quantidadeTotal);
        
        // Consolida itens do mesmo produto em uma única linha
        var itemExistente = _produtos.FirstOrDefault(i => i.ProdutoId == item.ProdutoId);
        if (itemExistente != null)
        {
            _produtos.Remove(itemExistente);
            var itemConsolidado = itemExistente with
            {
                Quantidade = quantidadeTotal,
                Desconto = desconto
            };
            _produtos.Add(itemConsolidado);
        }
        else
        {
            var itemComDesconto = item.WithDesconto(desconto);
            _produtos.Add(itemComDesconto);
        }
        
        // Recalcula todos os descontos para garantir consistência
        RecalcularTodosOsDescontos();
        
        // Adiciona evento de alteração
        AddDomainEvent(new CompraAlterada(Id, new[] { item.ProdutoId }));
        
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
    
    public Result RemoverItem(Guid produtoId)
    {
        if (Status == StatusVenda.Cancelada)
            return Result.Failure("Não é possível remover itens de uma venda cancelada.");
        
        var item = _produtos.FirstOrDefault(i => i.ProdutoId == produtoId);
        if (item == null)
            return Result.Failure($"Produto {produtoId} não encontrado na venda.");
        
        _produtos.Remove(item);
        
        // Recalcula todos os descontos para garantir consistência
        RecalcularTodosOsDescontos();
        
        AddDomainEvent(new ItemCancelado(Id, produtoId));
        
        return Result.Success();
    }
    
    public void MarcarComoPendenteValidacao()
    {
        Status = StatusVenda.PendenteValidacao;
    }
    
    private void RecalcularTodosOsDescontos()
    {
        var grupos = _produtos.GroupBy(i => i.ProdutoId);
        
        foreach (var grupo in grupos)
        {
            var quantidadeTotal = grupo.Sum(i => i.Quantidade);
            var desconto = _politicaDesconto.Calcular(quantidadeTotal);
            
            foreach (var item in grupo)
            {
                var index = _produtos.IndexOf(item);
                if (index >= 0 && _produtos[index].Desconto != desconto)
                {
                    _produtos[index] = item with { Desconto = desconto };
                }
            }
        }
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
