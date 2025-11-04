using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Venda.Infrastructure.Data;
using Venda.Infrastructure.Entities;
using Venda.Integration.Tests.Infrastructure;

namespace Venda.Integration.Tests.Helpers;

/// <summary>
/// Helper para validação de eventos de domínio nos testes de integração.
/// Fornece métodos para verificar se eventos foram gerados corretamente no banco de dados.
/// </summary>
public static class EventValidationHelper
{
    /// <summary>
    /// Verifica se um evento específico foi registrado no banco de dados.
    /// </summary>
    /// <param name="factory">Factory da aplicação para acessar os serviços</param>
    /// <param name="eventType">Tipo do evento esperado (ex: "CompraCriada", "CompraAlterada")</param>
    /// <param name="vendaId">ID da venda relacionada ao evento</param>
    /// <param name="clienteId">ID do cliente (opcional, para validação adicional)</param>
    /// <returns>O evento encontrado no banco de dados</returns>
    public static async Task<OutboxEvent> VerificarEventoNoBanco(
        CustomWebApplicationFactory factory,
        string eventType,
        Guid vendaId,
        Guid? clienteId = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        var evento = await db.OutboxEvents
            .Where(e => e.EventType.Contains(eventType))
            .Where(e => e.EventData.Contains(vendaId.ToString()))
            .FirstOrDefaultAsync();

        // Validações básicas
        evento.Should().NotBeNull($"deve gerar evento {eventType}");
        evento!.EventType.Should().Contain(eventType, "o tipo do evento deve corresponder");
        evento.EventData.Should().Contain(vendaId.ToString(), "o evento deve conter o ID da venda");
        evento.Status.Should().Be("Pending", "eventos recém-criados devem ter status Pending");
        evento.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1), 
            "o evento deve ter sido criado recentemente");

        // Validação adicional do ClienteId se fornecido
        if (clienteId.HasValue)
        {
            evento.EventData.Should().Contain(clienteId.Value.ToString(), 
                "o evento deve conter o ID do cliente");
        }

        return evento;
    }

    /// <summary>
    /// Verifica se um evento específico com dados de produto foi registrado no banco de dados.
    /// Útil para eventos ItemCancelado que incluem informações do produto.
    /// </summary>
    /// <param name="factory">Factory da aplicação para acessar os serviços</param>
    /// <param name="eventType">Tipo do evento esperado</param>
    /// <param name="vendaId">ID da venda relacionada ao evento</param>
    /// <param name="produtoId">ID do produto relacionado ao evento</param>
    /// <returns>O evento encontrado no banco de dados</returns>
    public static async Task<OutboxEvent> VerificarEventoComProdutoNoBanco(
        CustomWebApplicationFactory factory,
        string eventType,
        Guid vendaId,
        Guid produtoId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        var evento = await db.OutboxEvents
            .Where(e => e.EventType.Contains(eventType))
            .Where(e => e.EventData.Contains(vendaId.ToString()))
            .Where(e => e.EventData.Contains(produtoId.ToString()))
            .FirstOrDefaultAsync();

        // Validações básicas
        evento.Should().NotBeNull($"deve gerar evento {eventType} para o produto");
        evento!.EventType.Should().Contain(eventType, "o tipo do evento deve corresponder");
        evento.EventData.Should().Contain(vendaId.ToString(), "o evento deve conter o ID da venda");
        evento.EventData.Should().Contain(produtoId.ToString(), "o evento deve conter o ID do produto");
        evento.Status.Should().Be("Pending", "eventos recém-criados devem ter status Pending");

        return evento;
    }

    /// <summary>
    /// Verifica se múltiplos eventos de um tipo específico foram registrados.
    /// Útil para validar que todos os eventos esperados foram gerados.
    /// </summary>
    /// <param name="factory">Factory da aplicação para acessar os serviços</param>
    /// <param name="eventType">Tipo do evento esperado</param>
    /// <param name="vendaId">ID da venda relacionada aos eventos</param>
    /// <param name="expectedCount">Quantidade esperada de eventos</param>
    /// <returns>Lista de eventos encontrados</returns>
    public static async Task<List<OutboxEvent>> VerificarMultiplosEventosNoBanco(
        CustomWebApplicationFactory factory,
        string eventType,
        Guid vendaId,
        int expectedCount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendaDbContext>();

        var eventos = await db.OutboxEvents
            .Where(e => e.EventType.Contains(eventType))
            .Where(e => e.EventData.Contains(vendaId.ToString()))
            .ToListAsync();

        eventos.Should().HaveCount(expectedCount, 
            $"deve gerar exatamente {expectedCount} evento(s) do tipo {eventType}");

        foreach (var evento in eventos)
        {
            evento.EventType.Should().Contain(eventType);
            evento.EventData.Should().Contain(vendaId.ToString());
            evento.Status.Should().Be("Pending");
        }

        return eventos;
    }

    /// <summary>
    /// Verifica a estrutura completa de um evento, incluindo todos os campos obrigatórios.
    /// </summary>
    /// <param name="evento">Evento a ser validado</param>
    /// <param name="expectedEventType">Tipo esperado do evento</param>
    /// <param name="vendaId">ID da venda que deve estar no evento</param>
    /// <param name="clienteId">ID do cliente que deve estar no evento (opcional)</param>
    public static void ValidarEstruturaEvento(
        OutboxEvent evento,
        string expectedEventType,
        Guid vendaId,
        Guid? clienteId = null)
    {
        evento.Should().NotBeNull("o evento não deve ser nulo");
        evento.Id.Should().NotBeEmpty("o evento deve ter um ID válido");
        evento.EventType.Should().Contain(expectedEventType, "o tipo do evento deve corresponder");
        evento.EventData.Should().NotBeNullOrEmpty("o evento deve conter dados");
        evento.EventData.Should().Contain(vendaId.ToString(), "o evento deve conter o ID da venda");
        
        if (clienteId.HasValue)
        {
            evento.EventData.Should().Contain(clienteId.Value.ToString(), 
                "o evento deve conter o ID do cliente");
        }

        evento.Status.Should().Be("Pending", "eventos recém-criados devem ter status Pending");
        evento.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        evento.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        evento.ProcessedAt.Should().BeNull("eventos Pending não devem ter data de processamento");
        evento.RetryCount.Should().Be(0, "eventos novos não devem ter tentativas de retry");
        evento.LastError.Should().BeNullOrEmpty("eventos Pending não devem ter erros");
    }
}
