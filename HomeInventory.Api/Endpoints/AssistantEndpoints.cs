using HomeInventory.Api.Extensions;
using HomeInventory.Application.Assistant.Commands.AskAssistant;
using HomeInventory.Application.Assistant.Common;
using MediatR;

namespace HomeInventory.Api.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assistant").WithTags("Assistant").RequireAuthorization();

        group.MapPost("/chat", async (
            ChatRequest body,
            ISender sender,
            CancellationToken ct) =>
            (await sender.Send(
                new AskAssistantCommand(body.Message, body.History), ct)).ToHttpResult());

        return app;
    }
}
