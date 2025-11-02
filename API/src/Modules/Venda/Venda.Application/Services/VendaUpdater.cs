using _123Vendas.Shared.Common;
using Microsoft.Extensions.Logging;
using Venda.Application.DTOs;
using Venda.Domain.Aggregates;
using Venda.Domain.ValueObjects;

namespace Venda.Application.Services;

/// <summary>
/// Serviço responsável pela lógica de atualização de itens de uma venda.
/// Implementa SRP: única responsabilidade de coordenar mudanças nos itens.
/// </summary>
public class VendaUpdater
{
    private readonly ILogger<VendaUpdater> _logger;

    public VendaUpdater(ILogger<VendaUpdater> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Atualiza os itens de uma venda de forma inteligente:
    /// - Remove itens que não estão mais na lista
    /// - Adiciona novos itens
    /// - Ajusta quantidades de itens existentes
    /// </summary>
    public Result AtualizarItens(VendaAgregado venda, IReadOnlyList<ItemVendaDto> itensDto)
    {
        var produtosNovos = itensDto.Select(i => i.ProdutoId).ToHashSet();
        var produtosExistentes = venda.Produtos.Select(p => p.ProdutoId).ToHashSet();

        // 1. Remover itens que não estão mais na lista
        var result = RemoverItensAusentes(venda, produtosExistentes, produtosNovos);
        if (result.IsFailure)
            return result;

        // 2. Atualizar ou adicionar itens
        foreach (var itemDto in itensDto)
        {
            result = AtualizarOuAdicionarItem(venda, itemDto);
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

    private Result RemoverItensAusentes(
        VendaAgregado venda,
        HashSet<Guid> produtosExistentes,
        HashSet<Guid> produtosNovos)
    {
        var produtosParaRemover = produtosExistentes.Except(produtosNovos).ToList();

        foreach (var produtoId in produtosParaRemover)
        {
            var result = venda.RemoverItem(produtoId);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Falha ao remover item {ProdutoId} da venda {VendaId}: {Error}",
                    produtoId, venda.Id, result.Error);
                return result;
            }

            _logger.LogInformation(
                "Item {ProdutoId} removido da venda {VendaId}",
                produtoId, venda.Id);
        }

        return Result.Success();
    }

    private Result AtualizarOuAdicionarItem(VendaAgregado venda, ItemVendaDto itemDto)
    {
        var itemExistente = venda.Produtos.FirstOrDefault(p => p.ProdutoId == itemDto.ProdutoId);

        if (itemExistente == null)
        {
            // Item novo: adicionar
            return AdicionarNovoItem(venda, itemDto);
        }

        // Item existente: ajustar quantidade
        return AjustarQuantidadeItem(venda, itemDto, itemExistente);
    }

    private Result AdicionarNovoItem(VendaAgregado venda, ItemVendaDto itemDto)
    {
        var item = new ItemVenda(
            itemDto.ProdutoId,
            itemDto.Quantidade,
            itemDto.ValorUnitario,
            0m); // Desconto será calculado pelo agregado

        var result = venda.AdicionarItem(item);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Novo item {ProdutoId} adicionado à venda {VendaId} com {Quantidade} unidades",
                itemDto.ProdutoId, venda.Id, itemDto.Quantidade);
        }
        else
        {
            _logger.LogWarning(
                "Falha ao adicionar item {ProdutoId} à venda {VendaId}: {Error}",
                itemDto.ProdutoId, venda.Id, result.Error);
        }

        return result;
    }

    private Result AjustarQuantidadeItem(
        VendaAgregado venda,
        ItemVendaDto itemDto,
        ItemVenda itemExistente)
    {
        var diferenca = itemDto.Quantidade - itemExistente.Quantidade;

        if (diferenca == 0)
        {
            // Quantidade não mudou: nada a fazer
            return Result.Success();
        }

        if (diferenca > 0)
        {
            // Aumentar quantidade: adicionar mais unidades
            return AdicionarUnidades(venda, itemDto, diferenca);
        }

        // Diminuir quantidade: remover unidades
        return RemoverUnidades(venda, itemDto, Math.Abs(diferenca));
    }

    private Result AdicionarUnidades(VendaAgregado venda, ItemVendaDto itemDto, int quantidadeAdicionar)
    {
        var item = new ItemVenda(
            itemDto.ProdutoId,
            quantidadeAdicionar,
            itemDto.ValorUnitario,
            0m);

        var result = venda.AdicionarItem(item);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Adicionadas {Quantidade} unidades do item {ProdutoId} à venda {VendaId}",
                quantidadeAdicionar, itemDto.ProdutoId, venda.Id);
        }
        else
        {
            _logger.LogWarning(
                "Falha ao adicionar {Quantidade} unidades do item {ProdutoId} à venda {VendaId}: {Error}",
                quantidadeAdicionar, itemDto.ProdutoId, venda.Id, result.Error);
        }

        return result;
    }

    private Result RemoverUnidades(VendaAgregado venda, ItemVendaDto itemDto, int quantidadeRemover)
    {
        var result = venda.RemoverItem(itemDto.ProdutoId, quantidadeRemover);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Removidas {Quantidade} unidades do item {ProdutoId} da venda {VendaId}",
                quantidadeRemover, itemDto.ProdutoId, venda.Id);
        }
        else
        {
            _logger.LogWarning(
                "Falha ao remover {Quantidade} unidades do item {ProdutoId} da venda {VendaId}: {Error}",
                quantidadeRemover, itemDto.ProdutoId, venda.Id, result.Error);
        }

        return result;
    }
}
