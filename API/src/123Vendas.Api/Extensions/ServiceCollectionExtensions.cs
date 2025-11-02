using FluentValidation;
using MediatR;
using Venda.Application.Interfaces;
using Venda.Domain.Interfaces;
using Venda.Infrastructure.Interfaces;
using Venda.Infrastructure.Repositories;
using Venda.Infrastructure.Services;

namespace _123Vendas.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // MediatR - Registrar handlers do assembly de Application
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Venda.Application.Commands.CriarVendaCommand).Assembly);
        });

        // FluentValidation - Registrar validadores
        services.AddValidatorsFromAssembly(typeof(Venda.Application.Commands.CriarVendaCommand).Assembly);

        // Application Services
        services.AddScoped<Venda.Application.Services.VendaUpdater>();

        // Repositories
        services.AddScoped<IVendaRepository, VendaRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IOutboxService, OutboxService>();

        // Background Services
        services.AddHostedService<Venda.Infrastructure.BackgroundServices.OutboxProcessor>();

        // Event Handlers (CRM e Estoque)
        services.AddScoped<CRM.Application.EventHandlers.CrmEventHandler>();
        services.AddScoped<Estoque.Application.EventHandlers.EstoqueEventHandler>();

        return services;
    }
}
