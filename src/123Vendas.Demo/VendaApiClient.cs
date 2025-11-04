using System.Net.Http.Json;
using System.Text.Json;

namespace _123Vendas.Demo;

public class VendaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public VendaApiClient(string baseUrl = "http://localhost:5197")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<Guid?> CriarVendaAsync(CriarVendaRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/vendas", request);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Erro ao criar venda (Status {(int)response.StatusCode}):");
                
                // Tentar extrair a mensagem de erro do ProblemDetails
                try
                {
                    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
                    if (problemDetails?.Detail != null)
                    {
                        Console.WriteLine($"   {problemDetails.Detail}\n");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"   {errorContent}\n");
                    }
                }
                catch
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"   {errorContent}\n");
                }
                
                Console.ResetColor();
                return null;
            }
            
            // A API retorna apenas o Guid da venda criada como string JSON
            var vendaIdString = await response.Content.ReadAsStringAsync();
            // Remove aspas do JSON
            vendaIdString = vendaIdString.Trim('"');
            
            if (Guid.TryParse(vendaIdString, out var vendaId))
            {
                return vendaId;
            }
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro ao parsear ID da venda: {vendaIdString}");
            Console.ResetColor();
            return null;
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro de conexão ao criar venda: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    public async Task<VendaResponse?> AtualizarVendaAsync(Guid vendaId, AtualizarVendaRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/vendas/{vendaId}", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Erro ao atualizar venda (Status {(int)response.StatusCode}):");
                
                // Tentar extrair a mensagem de erro do ProblemDetails
                try
                {
                    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
                    if (problemDetails?.Detail != null)
                    {
                        Console.WriteLine($"   {problemDetails.Detail}");
                    }
                    else
                    {
                        Console.WriteLine($"   {errorContent}");
                    }
                }
                catch
                {
                    Console.WriteLine($"   {errorContent}");
                }
                
                Console.ResetColor();
                return null;
            }
            
            return await response.Content.ReadFromJsonAsync<VendaResponse>();
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro de conexão ao atualizar venda: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    public async Task<VendaResponse?> ObterVendaAsync(Guid vendaId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/vendas/{vendaId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VendaResponse>();
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro ao obter venda: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    public async Task<PagedResultResponse<VendaResponse>?> ListarVendasAsync(int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/vendas?pageNumber={pageNumber}&pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PagedResultResponse<VendaResponse>>();
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro ao listar vendas: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    public async Task<bool> CancelarVendaAsync(Guid vendaId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/v1/vendas/{vendaId}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Erro ao cancelar venda (Status {(int)response.StatusCode}):");
                
                // Tentar extrair a mensagem de erro do ProblemDetails
                try
                {
                    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
                    if (problemDetails?.Detail != null)
                    {
                        Console.WriteLine($"   {problemDetails.Detail}");
                    }
                    else
                    {
                        Console.WriteLine($"   {errorContent}");
                    }
                }
                catch
                {
                    Console.WriteLine($"   {errorContent}");
                }
                
                Console.ResetColor();
                return false;
            }
            
            return true;
        }
        catch (HttpRequestException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro de conexão ao cancelar venda: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    public async Task<bool> VerificarApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// DTOs para comunicação com a API - 100% compatíveis com VendasEndpoints.cs
public record CriarVendaRequest(
    Guid ClienteId,
    Guid FilialId,
    List<ItemVendaDto> Itens);

public record AtualizarVendaRequest(
    List<ItemVendaDto> Itens);

public record ItemVendaDto(
    Guid ProdutoId,
    int Quantidade,
    decimal ValorUnitario,
    decimal Desconto,
    decimal Total);

public record VendaResponse(
    Guid Id,
    int Numero,
    DateTime Data,
    Guid ClienteId,
    Guid FilialId,
    decimal ValorTotal,
    string Status,
    List<ItemVendaDto> Itens);

public record PagedResultResponse<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
};

// DTO para capturar erros da API (ProblemDetails)
public record ProblemDetailsResponse(
    string? Title,
    string? Detail,
    int? Status);
