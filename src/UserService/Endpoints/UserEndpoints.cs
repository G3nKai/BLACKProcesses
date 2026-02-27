using Microsoft.AspNetCore.Authorization;
using UserService.Contracts;
using UserService.Services;

namespace UserService.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/users")
            .WithTags("User Service")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "ADMIN,EMPLOYEE" });

        group.MapGet("/", async ([AsParameters] PagingQuery query, IUserManagementService users, CancellationToken ct) =>
            Results.Ok(await users.GetUsersAsync(query, ct)))
            .WithSummary("Получить список всех пользователей");

        group.MapPost("/", async (CreateUserAdminRequest request, IUserManagementService users, CancellationToken ct) =>
            {
                var created = await users.CreateUserAsync(request, ct);
                return Results.Created($"/admin/users/{created.Id}", created);
            })
            .WithSummary("Создать пользователя вручную");

        group.MapPatch("/{userId:guid}/status", async (Guid userId, UpdateUserStatusRequest request, IUserManagementService users, CancellationToken ct) =>
            Results.Ok(await users.UpdateStatusAsync(userId, request, ct)))
            .WithSummary("Заблокировать или разблокировать пользователя");

        return app;
    }
}
