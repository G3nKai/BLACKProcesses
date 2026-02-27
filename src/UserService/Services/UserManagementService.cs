using Microsoft.EntityFrameworkCore;
using UserService.Contracts;
using UserService.Data;
using UserService.Domain;

namespace UserService.Services;

public interface IUserManagementService
{
    Task<UsersResponse> GetUsersAsync(PagingQuery query, CancellationToken cancellationToken);
    Task<UserResponse> CreateUserAsync(CreateUserAdminRequest request, CancellationToken cancellationToken);
    Task<UserResponse> UpdateStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken);
}

public sealed class UserManagementService(
    UserDbContext dbContext,
    IAuthServiceClient authServiceClient,
    ICoreServiceClient coreServiceClient,
    ILogger<UserManagementService> logger) : IUserManagementService
{
    public async Task<UsersResponse> GetUsersAsync(PagingQuery query, CancellationToken cancellationToken)
    {
        var filtered = dbContext.Users.AsNoTracking();

        if (query.Role.HasValue)
        {
            filtered = filtered.Where(u => u.Role == query.Role.Value);
        }

        var total = await filtered.CountAsync(cancellationToken);
        var users = await filtered
            .OrderByDescending(u => u.CreatedAt)
            .Skip(query.Page * query.Size)
            .Take(query.Size)
            .ToListAsync(cancellationToken);

        var totalPages = query.Size == 0 ? 0 : (int)Math.Ceiling(total / (double)query.Size);
        return new UsersResponse(users.Select(u => u.ToResponse()).ToArray(), new PageInfo(query.Page, query.Size, total, totalPages));
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserAdminRequest request, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Users.AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var user = new UserProfile
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = request.Role,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await authServiceClient.RegisterCredentialsAsync(new RegisterCredentialsRequest(user.Id, user.Email, request.Password, user.Role.ToString().ToUpperInvariant()), cancellationToken);
        }
        catch
        {
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }

        logger.LogInformation("Admin created user {UserId} with role {Role}", user.Id, user.Role);
        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (request.Status is not (UserStatus.Active or UserStatus.Blocked))
        {
            throw new InvalidOperationException("Only ACTIVE or BLOCKED are supported by this endpoint.");
        }

        user.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);

        await coreServiceClient.SyncUserStatusAsync(new UpdateAccountStateRequest(user.Id, user.Status.ToString().ToUpperInvariant()), cancellationToken);
        return user.ToResponse();
    }
}
