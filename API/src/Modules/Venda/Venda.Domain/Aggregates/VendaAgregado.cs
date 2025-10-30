using _123Vendas.Shared.Common;
using _123Vendas.Shared.Events;
using Venda.Domain.Enums;
using Venda.Domain.ValueObjects;

namespace Venda.Domain.Aggregates;

public class VendaAgregado
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public int NumeroVenda { get; private set; }
    public DateTime Data { get; private set; } = DateTime.UtcNow;
    public Guid ClienteId { get; private set; }
    public string Filial { get; private set; } = string.Empty;
    public StatusVenda Status { get; private set; } = StatusVenda.Ativa;
    
    private readonly List<ItemVenda> _produtos = new();
    public IReadOnlyList<ItemVenda> Produtos => _produtos.AsReadOnly();
    
    public decimal ValorTotal => _produtos.Sum(i => i.Total);
    
    // Domain Events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    public static VendaAgregado Criar(Guid clienteId, string filial)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("ClienteId é obrigatório.", nameof(clienteId));
        
        if (string.IsNullOrWhiteSpace(filial))
            throw new ArgumentException("Filial é obrigatória.", nameof(filial));
        
        var venda = new VendaAgregado
        {
            ClienteId = clienteId,
            Filial = filial,
            NumeroVenda = 0 // Será definido pelo repositório
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
        
        if (item.ValorUnitario <= 0)
            return Result.Failure("Valor unitário deve ser maior que zero.");
        
        // Conta quantos itens do mesmo produto já existem
        var quantidadeExistente = _produtos
            .Where(i => i.ProdutoId == item.ProdutoId)
            .Sum(i => i.Quantidade);
        
        var quantidadeTotal = quantidadeExistente + item.Quantidade;
        
        // Valida regra: não permitir mais de 20 itens iguais
        if (quantidadeTotal > 20)
            return Result.Failure("Não é permitido vender mais de 20 unidades do mesmo produto.");
        
        // Calcula desconto baseado na quantidade total
        var desconto = CalcularDesconto(quantidadeTotal);
        var itemComDesconto = item.WithDesconto(desconto);
        
        _produtos.Add(itemComDesconto);
        
        // Recalcula descontos de todos os itens do mesmo produto
        RecalcularDescontosProduto(item.ProdutoId);
        
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
        RecalcularDescontosProduto(produtoId);
        AddDomainEvent(new ItemCancelado(Id, produtoId));
        
        return Result.Success();
    }
    
    public void MarcarComoPendenteValidacao()
    {
        Status = StatusVenda.PendenteValidacao;
    }
    
    private decimal CalcularDesconto(int quantidade)
    {
        return quantidade switch
        {
            < 4 => 0m,
            >= 4 and < 10 => 0.10m,
            >= 10 and <= 20 => 0.20m,
            _ => 0m
        };
    }
    
    private void RecalcularDescontosProduto(Guid produtoId)
    {
        var itensProduto = _produtos.Where(i => i.ProdutoId == produtoId).ToList();
        var quantidadeTotal = itensProduto.Sum(i => i.Quantidade);
        var desconto = CalcularDesconto(quantidadeTotal);
        
        for (int i = 0; i < _produtos.Count; i++)
        {
            if (_produtos[i].ProdutoId == produtoId && _produtos[i].Desconto != desconto)
            {
                _produtos[i] = _produtos[i].WithDesconto(desconto);
            }
        }
    }
    
    private void AddDomainEvent(IDomainEvent @event)
    {
        _domainEvents.Add(@event);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    private VendaAgregado() { } // EF Core
}
