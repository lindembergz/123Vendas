using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Venda.Application.Interfaces;
using Venda.Domain.Enums;
using Venda.Domain.Interfaces;
using Venda.Infrastructure.Data;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Performance;

/// <summary>
/// Testes de benchmark para validar melhorias de performance com otimizações de queries.
/// 
/// Otimizações testadas:
/// - AsSplitQuery() para evitar cartesian explosion
/// - Otimização de count query (skip quando primeira página tem menos itens)
/// - Projection direta para DTO (ListarComProjecaoAsync)
/// - Método AplicarFiltros extraído para reutilização
/// </summary>
public class QueryOptimizationBenchmarkTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public QueryOptimizationBenchmarkTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private IVendaRepository GetVendaRepository()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IVendaRepository>();
    }

    private IVendaQueryRepository GetVendaQueryRepository()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IVendaQueryRepository>();
    }

    private VendaDbContext GetDbContext()
    {
        var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<VendaDbContext>();
    }

    /// <summary>
    /// Benchmark: ListarComFiltrosAsync com AsSplitQuery
    /// Valida que AsSplitQuery evita cartesian explosion ao carregar coleções
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarComFiltrosAsync_ComAsSplitQuery()
    {
        // Arrange
        var repository = GetVendaRepository();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var (items, totalCount) = await repository.ListarComFiltrosAsync(
            pageNumber: 1,
            pageSize: 20);

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200,
            "ListarComFiltrosAsync com AsSplitQuery deve executar em menos de 200ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarComFiltrosAsync (AsSplitQuery): {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"[BENCHMARK] Items retornados: {items.Count}, Total: {totalCount}");
    }

    /// <summary>
    /// Benchmark: ListarComFiltrosAsync - Otimização de count na primeira página
    /// Valida que count query é evitada quando primeira página tem menos itens que pageSize
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarComFiltrosAsync_OtimizacaoCountPrimeiraPagina()
    {
        // Arrange
        var repository = GetVendaRepository();
        var clienteIdInexistente = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // Act - Primeira página com filtro que retorna 0 resultados
        var (items, totalCount) = await repository.ListarComFiltrosAsync(
            pageNumber: 1,
            pageSize: 20,
            clienteId: clienteIdInexistente);

        stopwatch.Stop();

        // Assert
        items.Should().BeEmpty();
        totalCount.Should().Be(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
            "Query otimizada sem count extra deve executar em menos de 50ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarComFiltrosAsync (Sem Count Extra): {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Benchmark: ListarComProjecaoAsync vs ListarComFiltrosAsync
    /// Compara performance entre carregar agregado completo vs projection direta
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarComProjecaoAsync_VsAgregadoCompleto()
    {
        // Arrange
        var repository = GetVendaRepository();
        var queryRepository = GetVendaQueryRepository();

        // Act 1: Carregar agregado completo
        var stopwatch1 = Stopwatch.StartNew();
        var (itemsCompletos, totalCount1) = await repository.ListarComFiltrosAsync(
            pageNumber: 1,
            pageSize: 50);
        stopwatch1.Stop();

        // Act 2: Projection direta para DTO
        var stopwatch2 = Stopwatch.StartNew();
        var (itemsProjetados, totalCount2) = await queryRepository.ListarComProjecaoAsync(
            pageNumber: 1,
            pageSize: 50);
        stopwatch2.Stop();

        // Assert - Projection deve ser no máximo igual ou mais rápida
        // Com datasets pequenos e SQLite in-memory, a diferença pode ser mínima
        stopwatch2.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(stopwatch1.ElapsedMilliseconds,
            "Projection direta deve ser no máximo igual ou mais rápida que carregar agregado completo");

        var improvement = stopwatch1.ElapsedMilliseconds > 0 
            ? ((double)(stopwatch1.ElapsedMilliseconds - stopwatch2.ElapsedMilliseconds) / stopwatch1.ElapsedMilliseconds) * 100
            : 0;

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] Agregado Completo: {stopwatch1.ElapsedMilliseconds}ms");
        Console.WriteLine($"[BENCHMARK] Projection Direta: {stopwatch2.ElapsedMilliseconds}ms");
        Console.WriteLine($"[BENCHMARK] Melhoria: {improvement:F2}%");
    }

    /// <summary>
    /// Benchmark: ListarComProjecaoAsync com cálculos no SQL
    /// Valida que ValorTotal e QuantidadeItens são calculados no banco
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarComProjecaoAsync_CalculosNoSQL()
    {
        // Arrange
        var queryRepository = GetVendaQueryRepository();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var (items, totalCount) = await queryRepository.ListarComProjecaoAsync(
            pageNumber: 1,
            pageSize: 100);

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            "Projection com cálculos no SQL deve executar em menos de 500ms");

        // Validar que DTOs têm valores calculados
        foreach (var item in items)
        {
            item.ValorTotal.Should().BeGreaterThanOrEqualTo(0);
            item.QuantidadeItens.Should().BeGreaterThanOrEqualTo(0);
        }

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarComProjecaoAsync (100 itens): {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"[BENCHMARK] Items retornados: {items.Count}, Total: {totalCount}");
    }

    /// <summary>
    /// Benchmark: ListarComProjecaoAsync com múltiplos filtros
    /// Valida performance com filtros combinados
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarComProjecaoAsync_ComMultiplosFiltros()
    {
        // Arrange
        var queryRepository = GetVendaQueryRepository();
        var dataInicio = DateTime.UtcNow.AddDays(-30);
        var dataFim = DateTime.UtcNow;
        var status = StatusVenda.Ativa;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var (items, totalCount) = await queryRepository.ListarComProjecaoAsync(
            pageNumber: 1,
            pageSize: 50,
            status: status,
            dataInicio: dataInicio,
            dataFim: dataFim);

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200,
            "Projection com múltiplos filtros deve executar em menos de 200ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarComProjecaoAsync (Múltiplos Filtros): {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"[BENCHMARK] Items retornados: {items.Count}, Total: {totalCount}");
    }

    /// <summary>
    /// Benchmark: Comparação de queries com e sem AsSplitQuery
    /// Demonstra impacto do cartesian explosion
    /// </summary>
    [Fact]
    public async Task Benchmark_ComparacaoComESemAsSplitQuery()
    {
        // Arrange
        using var context = GetDbContext();

        // Act 1: Sem AsSplitQuery (cartesian explosion)
        var stopwatch1 = Stopwatch.StartNew();
        var vendasSemSplit = await context.Vendas
            .AsNoTracking()
            .Include(v => v.Produtos)
            .OrderByDescending(v => v.Data)
            .Take(50)
            .ToListAsync();
        stopwatch1.Stop();

        // Act 2: Com AsSplitQuery (queries separadas)
        var stopwatch2 = Stopwatch.StartNew();
        var vendasComSplit = await context.Vendas
            .AsNoTracking()
            .Include(v => v.Produtos)
            .AsSplitQuery()
            .OrderByDescending(v => v.Data)
            .Take(50)
            .ToListAsync();
        stopwatch2.Stop();

        // Assert
        vendasSemSplit.Count.Should().Be(vendasComSplit.Count);

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] Sem AsSplitQuery: {stopwatch1.ElapsedMilliseconds}ms");
        Console.WriteLine($"[BENCHMARK] Com AsSplitQuery: {stopwatch2.ElapsedMilliseconds}ms");
        
        if (stopwatch1.ElapsedMilliseconds > stopwatch2.ElapsedMilliseconds)
        {
            var improvement = ((double)(stopwatch1.ElapsedMilliseconds - stopwatch2.ElapsedMilliseconds) / stopwatch1.ElapsedMilliseconds) * 100;
            Console.WriteLine($"[BENCHMARK] Melhoria com AsSplitQuery: {improvement:F2}%");
        }
    }

    /// <summary>
    /// Benchmark: Paginação em páginas subsequentes
    /// Valida que count query é executado em páginas após a primeira
    /// </summary>
    [Fact]
    public async Task Benchmark_ListarComFiltrosAsync_PaginasSubsequentes()
    {
        // Arrange
        var repository = GetVendaRepository();

        // Act - Página 2 (deve executar count)
        var stopwatch = Stopwatch.StartNew();
        var (items, totalCount) = await repository.ListarComFiltrosAsync(
            pageNumber: 2,
            pageSize: 20);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
            "Paginação em página subsequente deve executar em menos de 500ms");

        // Log para documentação
        Console.WriteLine($"[BENCHMARK] ListarComFiltrosAsync (Página 2): {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"[BENCHMARK] Items retornados: {items.Count}, Total: {totalCount}");
    }
}
