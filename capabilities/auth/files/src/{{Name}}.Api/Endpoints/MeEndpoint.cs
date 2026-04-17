using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace {{Name}}.Api.Endpoints;

internal static class MeEndpoint
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            sub = user.FindFirst("sub")?.Value,
            name = user.Identity?.Name,
            claims = user.Claims.Select(c => new { c.Type, c.Value }),
        }))
        .WithTags("Auth")
        .WithOpenApi(op =>
        {
            op.Summary = "Who am I?";
            op.Description = "Returns the authenticated user's claims.";
            return op;
        });

        return app;
    }
}
