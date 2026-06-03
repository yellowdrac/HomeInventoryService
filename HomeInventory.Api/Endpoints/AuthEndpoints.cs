using HomeInventory.Api.Extensions;
using HomeInventory.Application.Authentication.Commands.Login;
using HomeInventory.Application.Authentication.Commands.Register;
using HomeInventory.Application.Authentication.Commands.RefreshToken;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult())
            .AllowAnonymous();

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult())
            .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
            (await sender.Send(command, ct)).ToHttpResult())
            .AllowAnonymous();

        return app;
    }
}
