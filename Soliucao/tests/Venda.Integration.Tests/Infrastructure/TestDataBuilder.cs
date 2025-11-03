using Bogus;
using Venda.Application.DTOs;

namespace Venda.Integration.Tests.Infrastructure;

/// <summary>
/// Builder para geração de dados fake realistas usando Bogus.
/// Facilita a criação de vendas e itens para testes de integração.
/// </summary>
public class TestDataBuilder
{
    private readonly Faker _faker;

    public TestDataBuilder()
    {
        // Configurar Faker com locale pt_BR para dados brasileiros
        _faker = new Faker("pt_BR");
    }

    /// <summary>
    /// Gera uma requisição de venda válida com itens fake.
    /// </summary>
    /// <param name="quantidadeItens">Número de itens a serem gerados (padrão: 1)</param>
    /// <returns>CriarVendaRequest com dados realistas</returns>
    public CriarVendaRequest GerarVendaValida(int quantidadeItens = 1)
    {
        return new CriarVendaRequest(
            ClienteId: Guid.NewGuid(),
            FilialId: Guid.NewGuid(),
            Itens: GerarItens(quantidadeItens)
        );
    }

    /// <summary>
    /// Gera uma lista de itens fake com valores realistas.
    /// </summary>
    /// <param name="quantidade">Número de itens a serem gerados</param>
    /// <returns>Lista de ItemVendaDto com dados realistas</returns>
    public List<ItemVendaDto> GerarItens(int quantidade)
    {
        return Enumerable.Range(1, quantidade)
            .Select(_ => new ItemVendaDto(
                ProdutoId: Guid.NewGuid(),
                Quantidade: _faker.Random.Int(1, 3), // Quantidade pequena para não acionar descontos
                ValorUnitario: _faker.Finance.Amount(10, 10000),
                Desconto: 0,
                Total: 0 // Será calculado pelo sistema
            ))
            .ToList();
    }

    /// <summary>
    /// Gera um item com quantidade entre 4-9 unidades para acionar desconto de 10%.
    /// </summary>
    /// <returns>ItemVendaDto configurado para desconto de 10%</returns>
    public ItemVendaDto GerarItemComDesconto10()
    {
        var quantidade = _faker.Random.Int(4, 9);
        var valorUnitario = _faker.Finance.Amount(10, 1000);
        
        return new ItemVendaDto(
            ProdutoId: Guid.NewGuid(),
            Quantidade: quantidade,
            ValorUnitario: valorUnitario,
            Desconto: 0, // Será calculado pelo sistema
            Total: 0 // Será calculado pelo sistema
        );
    }

    /// <summary>
    /// Gera um item com quantidade entre 10-20 unidades para acionar desconto de 20%.
    /// </summary>
    /// <returns>ItemVendaDto configurado para desconto de 20%</returns>
    public ItemVendaDto GerarItemComDesconto20()
    {
        var quantidade = _faker.Random.Int(10, 20);
        var valorUnitario = _faker.Finance.Amount(10, 1000);
        
        return new ItemVendaDto(
            ProdutoId: Guid.NewGuid(),
            Quantidade: quantidade,
            ValorUnitario: valorUnitario,
            Desconto: 0, // Será calculado pelo sistema
            Total: 0 // Será calculado pelo sistema
        );
    }
}
