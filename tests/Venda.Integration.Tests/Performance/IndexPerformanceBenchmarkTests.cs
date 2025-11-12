using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Venda.Domain.Enums;
using Venda.Infrastructure.Data;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Performance;

/// <summary>
/// Testes de benchmark para validar melhorias de performance com índices otimizados.
/// Migration: AddOptimizedIndexesWithDescendingOrder
/// 
/// Índices adicionados:
/// - IX_Vendas_ClienteId_Data (ClienteId ASC, Data DESC)
/// - IX_Vendas_FilialId_Data (FilialId ASC, Data DESC)
/// - IX_Vendas_Status_Data (Status ASC, Data DESC)
/// - IX_Vendas_Covering_List (Data DESC, Status, ClienteId, FilialId)
/// - IX_Vendas_Data (Data DESC)
/// </summary>
public class IndexPerformanceBenchmarkTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public IndexPerformanceBenchmarkTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private VendaDbContext GetDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<VendaDbContext>();
    }

    /// <summary>
    /// Benchmark: Query com filtro por ClienteId e ordenação por Data DESC
    /// Índice usado: IX_Vendas_ClienteId_Data
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarVendasPorCliente_ComIndiceOtimizado()
    {
        // Arrange
        using var context = GetDbContext();
        var clienteId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var vendas = await context.Vendas
            .Where(v => v.ClienteId == clienteId)
            .OrderByDescending(v => v.Data)
            .Take(20)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Query com índice ClienteId_Data deve executar em menos de 100ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarVendasPorCliente: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Benchmark: Query com filtro por FilialId e ordenação por Data DESC
    /// Índice usado: IX_Vendas_FilialId_Data
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarVendasPorFilial_ComIndiceOtimizado()
    {
        // Arrange
        using var context = GetDbContext();
        var filialId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var vendas = await context.Vendas
            .Where(v => v.FilialId == filialId)
            .OrderByDescending(v => v.Data)
            .Take(20)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "Query com índice FilialId_Data deve executar em menos de 100ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarVendasPorFilial: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Benchmark: Query com filtro por Status e ordenação por Data DESC
    /// Índice usado: IX_Vendas_Status_Data
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarVendasPorStatus_ComIndiceOtimizado()
    {
        // Arrange
        using var context = GetDbContext();
        var status = StatusVenda.Ativa;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var vendas = await context.Vendas
            .Where(v => v.Status == status)
            .OrderByDescending(v => v.Data)
            .Take(20)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "Query com índice Status_Data deve executar em menos de 100ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarVendasPorStatus: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Benchmark: Query de listagem geral com múltiplos filtros
    /// Índice usado: IX_Vendas_Covering_List (covering index)
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarVendasGeral_ComIndiceCovering()
    {
        // Arrange
        using var context = GetDbContext();
        var dataInicio = DateTime.UtcNow.AddDays(-30);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var vendas = await context.Vendas
            .Where(v => v.Data >= dataInicio)
            .OrderByDescending(v => v.Data)
            .Take(50)
            .Select(v => new
            {
                v.Id,
                v.Data,
                v.Status,
                v.ClienteId,
                v.FilialId,
                v.NumeroVenda
            })
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(150,
            "Query com covering index deve executar em menos de 150ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarVendasGeral (Covering): {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Benchmark: Query com múltiplos filtros combinados
    /// Índices usados: Múltiplos índices compostos
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarVendasComFiltrosCombinados_ComIndicesOtimizados()
    {
        // Arrange
        using var context = GetDbContext();
        var filialId = Guid.NewGuid();
        var status = StatusVenda.Ativa;
        var dataInicio = DateTime.UtcNow.AddDays(-7);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var vendas = await context.Vendas
            .Where(v => v.FilialId == filialId 
                     && v.Status == status 
                     && v.Data >= dataInicio)
            .OrderByDescending(v => v.Data)
            .Take(20)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
            "Query com múltiplos filtros deve executar em menos de 100ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarVendasComFiltrosCombinados: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Benchmark: Query em ItensVenda por ProdutoId
    /// Índice usado: IX_ItensVenda_ProdutoId
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarItensPorProduto_ComIndiceOtimizado()
    {
        // Arrange
        using var context = GetDbContext();
        var produtoId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act - Query itens através da venda (owned entity precisa do owner)
        var vendas = await context.Vendas
            .AsNoTracking()
            .Where(v => v.Produtos.Any(p => p.ProdutoId == produtoId))
            .Include(v => v.Produtos)
            .Take(50)
            .ToListAsync();
        
        var itens = vendas.SelectMany(v => v.Produtos)
            .Where(i => i.ProdutoId == produtoId)
            .ToList();

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(150,
            "Query em ItensVenda com índice ProdutoId deve executar em menos de 150ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarItensPorProduto: {stopwatch.ElapsedMilliseconds}ms");
    }
}
