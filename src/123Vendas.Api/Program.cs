using _123Vendas.Api.Configuration;
using _123Vendas.Api.Endpoints;
using _123Vendas.Api.Extensions;
using _123Vendas.Shared.Interfaces;
using CRM.Application.Services;
using Estoque.Application.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Venda.Infrastructure.Data;

try
{
    Log.Information("Iniciando 123Vendas API");

    var builder = WebApplication.CreateBuilder(args);

    //1. Configurar Logging
    builder.AddSerilogConfiguration();

    //2. Configurar Swagger/OpenAPI
    builder.Services.AddSwaggerConfiguration();

    //3. Registrar serviços de aplicação (MediatR, Repositories, etc.)
    builder.Services.AddApplicationServices();

    //4. Configurar Database
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=vendas.db";
    builder.Services.AddDatabaseConfiguration(connectionString);

    //5. Registrar serviços de domínio
    builder.Services.AddScoped<Venda.Domain.Interfaces.IPoliticaDesconto, Venda.Domain.Services.PoliticaDesconto>();

    //6. Configurar Health Checks
    builder.Services.AddAppHealthChecks(connectionString);

    //7. Configurar Options Pattern para serviços externos
    builder.Services.Configure<ServiceSettings>("CRM", builder.Configuration.GetSection("Services:CRM"));
    builder.Services.Configure<ServiceSettings>("Estoque", builder.Configuration.GetSection("Services:Estoque"));

    //8. Configurar serviços externos (CRM e Estoque)
    // MODO: MOCK para desenvolvimento (sempre retorna sucesso)

    //===== MOCK SERVICES (DESENVOLVIMENTO) =====
    builder.Services.AddScoped<IClienteService, CRM.Application.Services.ClienteServiceMock>();
    builder.Services.AddScoped<IProdutoService, Estoque.Application.Services.ProdutoServiceMock>();
    Log.Information("Usando MOCK services para CRM e Estoque");

    var app = builder.Build();

    //Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "123Vendas API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "123Vendas API - Documentação";
            options.DisplayRequestDuration();
        });

        Log.Information("Swagger UI disponível em: http://localhost:5197/swagger");
    }

    app.UseHttpsRedirection();

    //Mapear endpoints
    app.MapVendasEndpoints();
    app.MapAppHealthChecks();

    //Aplicar migrações automaticamente no startup (exceto em ambiente de testes)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VendaDbContext>();
            dbContext.Database.Migrate();
            Log.Information("Migrações aplicadas com sucesso");
        }
    }
    else
    {
        Log.Information("Ambiente de testes detectado - migrações automáticas desabilitadas");
    }

    app.Run();
    
    Log.Information("123Vendas API encerrada com sucesso");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Erro fatal ao iniciar 123Vendas API");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

//Make Program class accessible for integration tests
public partial class Program { }
