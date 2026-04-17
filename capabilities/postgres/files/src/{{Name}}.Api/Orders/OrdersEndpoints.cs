using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using {{Name}}.Application.Orders.Commands;
using {{Name}}.Application.Orders.Contracts;
using {{Name}}.Application.Orders.Queries;

namespace {{Name}}.Api.Orders;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        // NOTE: the sample is AllowAnonymous so the integration test can hit it
        // without a token. Real endpoints should stay under the default
        // [Authorize] fallback policy — remove the `.AllowAnonymous()` calls
        // when you replace this sample with real endpoints.
        var group = app.MapGroup("/v1/orders").WithTags("Orders");

        group.MapPost("/", async ([FromBody] PlaceOrder cmd, IMediator mediator) =>
        {
            var dto = await mediator.Send(cmd);
            return Results.Created($"/v1/orders/{dto.Id}", dto);
        })
        .AllowAnonymous()
        .Produces<OrderDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .WithOpenApi(op =>
        {
            op.Summary = "Place an order";
            return op;
        });

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var dto = await mediator.Send(new GetOrderById(id));
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .AllowAnonymous()
        .Produces<OrderDto>()
        .Produces(StatusCodes.Status404NotFound)
        .WithOpenApi(op =>
        {
            op.Summary = "Get order by id";
            return op;
        });

        return app;
    }
}
