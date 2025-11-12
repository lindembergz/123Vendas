using _123Vendas.Shared.Common;
using Microsoft.Extensions.Logging;
using Venda.Application.DTOs;
using Venda.Domain.Aggregates;
using Venda.Domain.ValueObjects;

namespace Venda.Application.Services;

/// <summary>
/// Serviço responsável pela orquestração de atualização de itens de uma venda.
/// Implementa SRP: única responsabilidade de coordenar e logar operações de atualização.
/// A lógica de negócio está no agregado de domínio.
/// </summary>
public class VendaUpdater
{
    private readonly ILogger<VendaUpdater> _logger;

    public VendaUpdater(ILogger<VendaUpdater> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Atualiza os itens de uma venda delegando a lógica de negócio para o agregado.
    /// </summary>
    public Result AtualizarItens(VendaAgregado venda, IReadOnlyList<ItemVendaDto> itensDto)
    {
        _logger.LogInformation(
            "Iniciando atualização de itens da venda {VendaId}. Itens recebidos: {QuantidadeItens}",
            venda.Id, itensDto.Count);
        
        // Converte DTOs para value objects de domínio
        var itens = itensDto.Select(dto => new ItemVenda(
            dto.ProdutoId,
            dto.Quantidade,
            dto.ValorUnitario,
            0m // Desconto será calculado pelo agregado
        )).ToList();
        
        // Delega a lógica de negócio para o agregado
        var result = venda.AtualizarItens(itens);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Atualização de itens da venda {VendaId} concluída com sucesso. Total de itens: {QuantidadeItens}",
                venda.Id, venda.Produtos.Count);
        }
        else
        {
            _logger.LogWarning(
                "Falha ao atualizar itens da venda {VendaId}: {Error}",
                venda.Id, result.Error);
        }
        
        return result;
    }
}
