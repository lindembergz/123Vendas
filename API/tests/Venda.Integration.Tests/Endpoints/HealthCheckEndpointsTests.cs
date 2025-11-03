using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Venda.Integration.Tests.Endpoints;

/// <summary>
/// Testes de integração para os endpoints de Health Check da API.
/// Valida os endpoints /health, /ready e /live para monitoramento da aplicação.
/// </summary>
public class HealthCheckEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthCheckEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GET_Health_DeveRetornar200OK()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GET_Health_DeveRetornarJsonComStatusEChecks()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        healthReport.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().NotBeNullOrEmpty();

        healthReport.TryGetProperty("checks", out var checks).Should().BeTrue();
        checks.ValueKind.Should().Be(JsonValueKind.Array);

        healthReport.TryGetProperty("totalDuration", out var totalDuration).Should().BeTrue();
        totalDuration.GetDouble().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GET_Health_DeveIncluirTodosOsChecks()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        healthReport.TryGetProperty("checks", out var checks).Should().BeTrue();
        var checksList = checks.EnumerateArray().ToList();

        // Verificar que os checks esperados estão presentes
        var checkNames = checksList
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        checkNames.Should().Contain("sqlite");
        checkNames.Should().Contain("self");
        checkNames.Should().Contain("outbox");
    }

    [Fact]
    public async Task GET_Ready_DeveRetornar200OK()
    {
        // Act
        var response = await _client.GetAsync("/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GET_Ready_DeveRetornarApenasStatusSelf()
    {
        // Act
        var response = await _client.GetAsync("/ready");
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        healthReport.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task GET_Live_DeveRetornar200OK()
    {
        // Act
        var response = await _client.GetAsync("/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GET_Live_DeveRetornarStatusComDbESelf()
    {
        // Act
        var response = await _client.GetAsync("/live");
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        healthReport.TryGetProperty("status", out var status).Should().BeTrue();
        var statusValue = status.GetString();
        statusValue.Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
    }

    [Fact]
    public async Task GET_Health_ChecksSqlite_DeveIncluirInformacoes()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        healthReport.TryGetProperty("checks", out var checks).Should().BeTrue();
        var sqliteCheck = checks.EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "sqlite");

        sqliteCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        sqliteCheck.TryGetProperty("status", out var status).Should().BeTrue();
        sqliteCheck.TryGetProperty("duration", out var duration).Should().BeTrue();
        duration.GetDouble().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GET_Health_ChecksOutbox_DeveIncluirDados()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        healthReport.TryGetProperty("checks", out var checks).Should().BeTrue();
        var outboxCheck = checks.EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "outbox");

        outboxCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        outboxCheck.TryGetProperty("status", out var status).Should().BeTrue();
        outboxCheck.TryGetProperty("data", out var data).Should().BeTrue();

        // Verificar que os dados do outbox estão presentes
        data.TryGetProperty("pendentes", out _).Should().BeTrue();
        data.TryGetProperty("falhados", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GET_Health_ChecksSelf_DeveSempreRetornarHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthReport = JsonSerializer.Deserialize<JsonElement>(content);

        // Assert
        healthReport.TryGetProperty("checks", out var checks).Should().BeTrue();
        var selfCheck = checks.EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("name").GetString() == "self");

        selfCheck.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        selfCheck.TryGetProperty("status", out var status).Should().BeTrue();
        status.GetString().Should().Be("Healthy");
        selfCheck.TryGetProperty("description", out var description).Should().BeTrue();
        description.GetString().Should().Be("API está respondendo");
    }
}
