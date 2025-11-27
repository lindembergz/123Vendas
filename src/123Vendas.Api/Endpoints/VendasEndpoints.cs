using _123Vendas.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Venda.Application.Commands;
using Venda.Application.DTOs;
using Venda.Application.Queries;

namespace _123Vendas.Api.Endpoints;

public static class VendasEndpoints
{
    public static IEndpointRouteBuilder MapVendasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/vendas")
            .WithTags("Vendas")
            .WithOpenApi();

        //POST /api/v1/vendas - Criar venda
        group.MapPost("/", CriarVenda)
            .WithName("CriarVenda")
            .WithSummary("Cria uma nova venda")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //GET /api/v1/vendas/{id} - Obter venda por ID
        group.MapGet("/{id:guid}", ObterVendaPorId)
            .WithName("ObterVendaPorId")
            .WithSummary("Obtém uma venda por ID")
            .Produces<VendaDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //GET /api/v1/vendas - Listar vendas
        group.MapGet("/", ListarVendas)
            .WithName("ListarVendas")
            .WithSummary("Lista vendas com filtros e paginação")
            .Produces<PagedResult<VendaDto>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //PUT /api/v1/vendas/{id} - Atualizar venda
        group.MapPut("/{id:guid}", AtualizarVenda)
            .WithName("AtualizarVenda")
            .WithSummary("Atualiza uma venda existente")
            .Produces<VendaDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        //DELETE /api/v1/vendas/{id} - Cancelar venda
        group.MapDelete("/{id:guid}", CancelarVenda)
            .WithName("CancelarVenda")
            .WithSummary("Cancela uma venda (soft delete)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> CriarVenda(
        [AsParameters] CriarVendaParameters parameters)
    {
        var command = new CriarVendaCommand(
            RequestId: Guid.NewGuid(),
            ClienteId: parameters.Request.ClienteId,
            FilialId: parameters.Request.FilialId,
            Itens: parameters.Request.Itens);

        var result = await parameters.Mediator.Send(command, parameters.Ct);

        if (result.IsFailure)
        {
            parameters.Logger.LogWarning("Falha ao criar venda: {Error}", result.Error);
            
            var isValidationError = result.Error?.Contains("obrigatório") == true ||
                                   result.Error?.Contains("deve") == true ||
                                   result.Error?.Contains("inválido") == true;
            
            return Results.BadRequest(new ProblemDetails
            {
                Title = isValidationError ? "Erro de validação" : "Erro ao criar venda",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var location = parameters.LinkGenerator.GetUriByName(
            parameters.HttpContext,
            "ObterVendaPorId",
            new { id = result.Value });

        parameters.Logger.LogInformation("Venda {VendaId} criada com sucesso", result.Value);
        return Results.Created(location, result.Value);
    }

    private static async Task<IResult> ObterVendaPorId(
        [AsParameters] ObterVendaPorIdParameters parameters)
    {
        var query = new ObterVendaPorIdQuery(parameters.Id);
        var venda = await parameters.Mediator.Send(query, parameters.Ct);

        if (venda == null)
        {
            parameters.Logger.LogWarning("Venda {VendaId} não encontrada", parameters.Id);
            return Results.NotFound(new ProblemDetails
            {
                Title = "Venda não encontrada",
                Detail = $"Venda com ID {parameters.Id} não foi encontrada",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Results.Ok(venda);
    }

    private static async Task<IResult> ListarVendas(
        [AsParameters] ListarVendasParameters parameters)
    {
        var pageNumber = parameters.PageNumber <= 0 ? 1 : parameters.PageNumber;
        var pageSize = parameters.PageSize <= 0 ? 10 : Math.Min(parameters.PageSize, 100); 

        var query = new ListarVendasQuery(
            PageNumber: pageNumber,
            PageSize: pageSize,
            ClienteId: parameters.ClienteId,
            FilialId: parameters.FilialId,
            Status: parameters.Status,
            DataInicio: parameters.DataInicio,
            DataFim: parameters.DataFim);

        var result = await parameters.Mediator.Send(query, parameters.Ct);

        parameters.Logger.LogInformation(
            "Listagem de vendas: Página {PageNumber}, Total: {TotalCount}",
            pageNumber, result.TotalCount);

        return Results.Ok(result);
    }

    private static async Task<IResult> AtualizarVenda(
        [AsParameters] AtualizarVendaParameters parameters)
    {
        var command = new AtualizarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: parameters.Id,
            Itens: parameters.Request.Itens);

        var result = await parameters.Mediator.Send(command, parameters.Ct);

        if (result.IsFailure)
        {
            if (result.Error?.Contains("não encontrada") == true)
            {
                parameters.Logger.LogWarning("Venda {VendaId} não encontrada para atualização", parameters.Id);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Venda não encontrada",
                    Detail = result.Error,
                    Status = StatusCodes.Status404NotFound
                });
            }

            parameters.Logger.LogWarning("Falha ao atualizar venda {VendaId}: {Error}", parameters.Id, result.Error);
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Erro ao atualizar venda",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        parameters.Logger.LogInformation("Venda {VendaId} atualizada com sucesso", parameters.Id);
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> CancelarVenda(
        [AsParameters] CancelarVendaParameters parameters)
    {
        var command = new CancelarVendaCommand(
            RequestId: Guid.NewGuid(),
            VendaId: parameters.Id);

        var result = await parameters.Mediator.Send(command, parameters.Ct);

        if (result.IsFailure)
        {
            if (result.Error?.Contains("não encontrada") == true)
            {
                parameters.Logger.LogWarning("Venda {VendaId} não encontrada para cancelamento", parameters.Id);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Venda não encontrada",
                    Detail = result.Error,
                    Status = StatusCodes.Status404NotFound
                });
            }

            parameters.Logger.LogWarning("Falha ao cancelar venda {VendaId}: {Error}", parameters.Id, result.Error);
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Erro ao cancelar venda",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest
            });
        }

        parameters.Logger.LogInformation("Venda {VendaId} cancelada com sucesso", parameters.Id);
        return Results.NoContent();
    }
}

// Parameters Structs
public record struct CriarVendaParameters(
    [FromBody] CriarVendaRequest Request,
    IMediator Mediator,
    ILogger<Program> Logger,
    LinkGenerator LinkGenerator,
    HttpContext HttpContext,
    CancellationToken Ct);

public record struct ObterVendaPorIdParameters(
    [FromRoute] Guid Id,
    IMediator Mediator,
    ILogger<Program> Logger,
    CancellationToken Ct);

public record struct ListarVendasParameters(
    [FromQuery] int PageNumber,
    [FromQuery] int PageSize,
    [FromQuery] Guid? ClienteId,
    [FromQuery] Guid? FilialId,
    [FromQuery] string? Status,
    [FromQuery] DateTime? DataInicio,
    [FromQuery] DateTime? DataFim,
    IMediator Mediator,
    ILogger<Program> Logger,
    CancellationToken Ct);

public record struct AtualizarVendaParameters(
    [FromRoute] Guid Id,
    [FromBody] AtualizarVendaRequest Request,
    IMediator Mediator,
    ILogger<Program> Logger,
    CancellationToken Ct);

public record struct CancelarVendaParameters(
    [FromRoute] Guid Id,
    IMediator Mediator,
    ILogger<Program> Logger,
    CancellationToken Ct);

// Request DTOs
public record CriarVendaRequest(
    Guid ClienteId,
    Guid FilialId,
    List<ItemVendaDto> Itens);

public record AtualizarVendaRequest(
    List<ItemVendaDto> Itens);
