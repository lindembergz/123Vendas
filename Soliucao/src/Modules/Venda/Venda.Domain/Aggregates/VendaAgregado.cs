using _123Vendas.Shared.Common;
using _123Vendas.Shared.Events;
using _123Vendas.Shared.Interfaces;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Domain.Services;
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
    
    // Construtor com injeção de dependência
    public VendaAgregado(IPoliticaDesconto politicaDesconto)
    {
        _politicaDesconto = politicaDesconto ?? throw new ArgumentNullException(nameof(politicaDesconto));
    }
    
    //Construtor privado para EF Core
    private VendaAgregado() : this(new PoliticaDesconto()) 
    {
        //EF Core usa este construtor
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
            NumeroVenda = 0 //Será definido pelo repositório após persistência
        };
        
        //Evento será adicionado após definir NumeroVenda no repositório
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
        
        //Valida usando a política de desconto centralizada
        if (!_politicaDesconto.PermiteVenda(quantidadeTotal))
            return Result.Failure("Não é permitido vender mais de 20 unidades do mesmo produto.");
        
        //Calcula desconto usando a política centralizada
        var desconto = _politicaDesconto.Calcular(quantidadeTotal);
        
        //Consolida itens do mesmo produto em uma única linha
        var itemExistente = _produtos.FirstOrDefault(i => i.ProdutoId == item.ProdutoId);
        if (itemExistente != null)
        {
            //Remove o item existente e adiciona com quantidade e desconto atualizados
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
            //Novo produto: adiciona com desconto calculado
            var itemComDesconto = new ItemVenda(
                item.ProdutoId,
                item.Quantidade,
                item.ValorUnitario,
                desconto
            );
            _produtos.Add(itemComDesconto);
        }
        
        //Adiciona evento de alteração
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
    

    
    private void RecalcularTodosOsDescontos()
    {
        //Recalcula descontos para todos os produtos
        //Nota: Cada produto deve ter apenas UMA linha na lista
        for (int i = 0; i < _produtos.Count; i++)
        {
            var item = _produtos[i];
            var quantidadeTotal = item.Quantidade;
            var desconto = _politicaDesconto.Calcular(quantidadeTotal);
            
            if (_produtos[i].Desconto != desconto)
            {
                _produtos[i] = new ItemVenda(
                    item.ProdutoId,
                    item.Quantidade,
                    item.ValorUnitario,
                    desconto
                );
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
