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

        // POST /api/v1/vendas - Criar venda
        group.MapPost("/", CriarVenda)
            .WithName("CriarVenda")
            .WithSummary("Cria uma nova venda")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /api/v1/vendas/{id} - Obter venda por ID
        group.MapGet("/{id:guid}", ObterVendaPorId)
            .WithName("ObterVendaPorId")
            .WithSummary("Obtém uma venda por ID")
            .Produces<VendaDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // GET /api/v1/vendas - Listar vendas
        group.MapGet("/", ListarVendas)
            .WithName("ListarVendas")
            .WithSummary("Lista vendas com filtros e paginação")
            .Produces<PagedResult<VendaDto>>(StatusCodes.Status200OK);

        // PUT /api/v1/vendas/{id} - Atualizar venda
        group.MapPut("/{id:guid}", AtualizarVenda)
            .WithName("AtualizarVenda")
            .WithSummary("Atualiza uma venda existente")
            .Produces<VendaDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // DELETE /api/v1/vendas/{id} - Cancelar venda
        group.MapDelete("/{id:guid}", CancelarVenda)
            .WithName("CancelarVenda")
            .WithSummary("Cancela uma venda (soft delete)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // POST /api/v1/vendas/{id}/confirmar - Confirmar venda pendente
        group.MapPost("/{id:guid}/confirmar", ConfirmarVenda)
            .WithName("ConfirmarVenda")
            .WithSummary("Confirma uma venda com status PendenteValidacao")
            .Produces<VendaDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> CriarVenda(
        [FromBody] CriarVendaRequest request,
        [FromServices] IMediator mediator,
        [FromServices] ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var command = new CriarVendaCommand(
                RequestId: Guid.NewGuid(),
                ClienteId: request.ClienteId,
                FilialId: request.FilialId,
                Itens: request.Itens);

            var result = await mediator.Send(command, ct);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao criar venda: {Error}", result.Error);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Erro ao criar venda",
                    Detail = result.Error,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Venda {VendaId} criada com sucesso", result.Value);
            return Results.Created($"/api/v1/vendas/{result.Value}", result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado ao criar venda");
            return Results.Problem(
                title: "Erro interno",
                detail: "Ocorreu um erro ao processar a requisição",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ObterVendaPorId(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        [FromServices] ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var query = new ObterVendaPorIdQuery(id);
            var venda = await mediator.Send(query, ct);

            if (venda == null)
            {
                logger.LogWarning("Venda {VendaId} não encontrada", id);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Venda não encontrada",
                    Detail = $"Venda com ID {id} não foi encontrada",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Results.Ok(venda);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao obter venda {VendaId}", id);
            return Results.Problem(
                title: "Erro interno",
                detail: "Ocorreu um erro ao processar a requisição",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ListarVendas(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] Guid? clienteId,
        [FromQuery] Guid? filialId,
        [FromQuery] string? status,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromServices] IMediator mediator,
        [FromServices] ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            // Valores padrão
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 100); // Máximo 100 itens por página

            var query = new ListarVendasQuery(
                PageNumber: pageNumber,
                PageSize: pageSize,
                ClienteId: clienteId,
                FilialId: filialId,
                Status: status,
                DataInicio: dataInicio,
                DataFim: dataFim);

            var result = await mediator.Send(query, ct);

            logger.LogInformation(
                "Listagem de vendas: Página {PageNumber}, Total: {TotalCount}",
                pageNumber, result.TotalCount);

            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar vendas");
            return Results.Problem(
                title: "Erro interno",
                detail: "Ocorreu um erro ao processar a requisição",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> AtualizarVenda(
        [FromRoute] Guid id,
        [FromBody] AtualizarVendaRequest request,
        [FromServices] IMediator mediator,
        [FromServices] ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var command = new AtualizarVendaCommand(
                RequestId: Guid.NewGuid(),
                VendaId: id,
                Itens: request.Itens);

            var result = await mediator.Send(command, ct);

            if (result.IsFailure)
            {
                if (result.Error?.Contains("não encontrada") == true)
                {
                    logger.LogWarning("Venda {VendaId} não encontrada para atualização", id);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Venda não encontrada",
                        Detail = result.Error,
                        Status = StatusCodes.Status404NotFound
                    });
                }

                logger.LogWarning("Falha ao atualizar venda {VendaId}: {Error}", id, result.Error);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Erro ao atualizar venda",
                    Detail = result.Error,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Venda {VendaId} atualizada com sucesso", id);
            return Results.Ok(result.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado ao atualizar venda {VendaId}", id);
            return Results.Problem(
                title: "Erro interno",
                detail: "Ocorreu um erro ao processar a requisição",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> CancelarVenda(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        [FromServices] ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            var command = new CancelarVendaCommand(
                RequestId: Guid.NewGuid(),
                VendaId: id);

            var result = await mediator.Send(command, ct);

            if (result.IsFailure)
            {
                if (result.Error?.Contains("não encontrada") == true)
                {
                    logger.LogWarning("Venda {VendaId} não encontrada para cancelamento", id);
                    return Results.NotFound(new ProblemDetails
                    {
                        Title = "Venda não encontrada",
                        Detail = result.Error,
                        Status = StatusCodes.Status404NotFound
                    });
                }

                logger.LogWarning("Falha ao cancelar venda {VendaId}: {Error}", id, result.Error);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Erro ao cancelar venda",
                    Detail = result.Error,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Venda {VendaId} cancelada com sucesso", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado ao cancelar venda {VendaId}", id);
            return Results.Problem(
                title: "Erro interno",
                detail: "Ocorreu um erro ao processar a requisição",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> ConfirmarVenda(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        [FromServices] ILogger<Program> logger,
        CancellationToken ct)
    {
        try
        {
            // Buscar a venda
            var query = new ObterVendaPorIdQuery(id);
            var venda = await mediator.Send(query, ct);

            if (venda == null)
            {
                logger.LogWarning("Venda {VendaId} não encontrada para confirmação", id);
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Venda não encontrada",
                    Detail = $"Venda com ID {id} não foi encontrada",
                    Status = StatusCodes.Status404NotFound
                });
            }

            if (venda.Status != "PendenteValidacao")
            {
                logger.LogWarning("Venda {VendaId} não está pendente de validação. Status atual: {Status}", id, venda.Status);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Status inválido",
                    Detail = $"Apenas vendas com status 'PendenteValidacao' podem ser confirmadas. Status atual: {venda.Status}",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Criar comando de confirmação (reutilizando a estrutura de atualização)
            var command = new ConfirmarVendaCommand(
                RequestId: Guid.NewGuid(),
                VendaId: id);

            var result = await mediator.Send(command, ct);

            if (result.IsFailure)
            {
                logger.LogWarning("Falha ao confirmar venda {VendaId}: {Error}", id, result.Error);
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Erro ao confirmar venda",
                    Detail = result.Error,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Venda {VendaId} confirmada com sucesso", id);
            
            // Buscar venda atualizada
            var vendaAtualizada = await mediator.Send(query, ct);
            return Results.Ok(vendaAtualizada);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado ao confirmar venda {VendaId}", id);
            return Results.Problem(
                title: "Erro interno",
                detail: "Ocorreu um erro ao processar a requisição",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

// Request DTOs
public record CriarVendaRequest(
    Guid ClienteId,
    Guid FilialId,
    List<ItemVendaDto> Itens);

public record AtualizarVendaRequest(
    List<ItemVendaDto> Itens);
