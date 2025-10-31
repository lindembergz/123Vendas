using _123Vendas.Shared.Interfaces;
using CRM.Application.Services;
using Estoque.Application.Services;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configurar HttpClient para ClienteService com Polly
builder.Services.AddHttpClient<IClienteService, ClienteService>(client =>
{
    var baseUrl = builder.Configuration["Services:CRM:BaseUrl"] 
        ?? throw new InvalidOperationException("CRM BaseUrl não configurada");
    var timeoutSeconds = int.Parse(builder.Configuration["Services:CRM:TimeoutSeconds"] ?? "30");
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine(
                $"[Retry CRM] Tentativa {retryAttempt} após {timespan.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Console.WriteLine(
                $"[Circuit Breaker CRM] ABERTO por {duration.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        },
        onReset: () =>
        {
            Console.WriteLine("[Circuit Breaker CRM] RESETADO - Serviço voltou ao normal");
        },
        onHalfOpen: () =>
        {
            Console.WriteLine("[Circuit Breaker CRM] HALF-OPEN - Testando serviço");
        }));

// Configurar HttpClient para ProdutoService com Polly
builder.Services.AddHttpClient<IProdutoService, ProdutoService>(client =>
{
    var baseUrl = builder.Configuration["Services:Estoque:BaseUrl"] 
        ?? throw new InvalidOperationException("Estoque BaseUrl não configurada");
    var timeoutSeconds = int.Parse(builder.Configuration["Services:Estoque:TimeoutSeconds"] ?? "30");
    
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine(
                $"[Retry Estoque] Tentativa {retryAttempt} após {timespan.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Console.WriteLine(
                $"[Circuit Breaker Estoque] ABERTO por {duration.TotalSeconds}s. " +
                $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
        },
        onReset: () =>
        {
            Console.WriteLine("[Circuit Breaker Estoque] RESETADO - Serviço voltou ao normal");
        },
        onHalfOpen: () =>
        {
            Console.WriteLine("[Circuit Breaker Estoque] HALF-OPEN - Testando serviço");
        }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
