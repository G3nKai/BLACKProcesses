using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserService.Contracts.Common;
using UserService.Contracts.Requests;
using UserService.Contracts.Responses;
using UserService.Data;
using UserService.Domain;
using UserService.Domain.Enums;

namespace UserService.Services;

public interface IUserManagementService
{
    Task<UsersResponse> GetUsersAsync(PagingQuery query, CancellationToken cancellationToken);
    Task<UserResponse> CreateUserAsync(CreateUserAdminRequest request, CancellationToken cancellationToken);
    Task<UserResponse> UpdateStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken);
}

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserDbContext _dbContext;
    private readonly IAuthServiceClient _authServiceClient;
    private readonly ICoreServiceClient _coreServiceClient;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserManagementService(
        UserDbContext dbContext,
        IAuthServiceClient authServiceClient,
        ICoreServiceClient coreServiceClient,
        ILogger<UserManagementService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _authServiceClient = authServiceClient;
        _coreServiceClient = coreServiceClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<UsersResponse> GetUsersAsync(PagingQuery query, CancellationToken cancellationToken)
    {
        await EnsureCurrentUserIsAdminAsync(cancellationToken);

        var filteredUsers = BuildUsersQuery(query);
        var totalElements = await filteredUsers.CountAsync(cancellationToken);
        var users = await GetUsersPageAsync(filteredUsers, query, cancellationToken);
        var page = BuildPageInfo(query, totalElements);

        return new UsersResponse(users.Select(x => x.ToResponse()).ToArray(), page);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserAdminRequest request, CancellationToken cancellationToken)
    {
        await EnsureEmailUniqueAsync(request.Email, cancellationToken);

        var user = BuildUser(request);
        await PersistCreatedUserAsync(user, cancellationToken);

        try
        {
            await RegisterInAuthServiceAsync(user, request.Password, cancellationToken);
        }
        catch
        {
            await RollbackCreatedUserAsync(user, cancellationToken);
            throw;
        }

        _logger.LogInformation("Admin created user {UserId} with role {Role}", user.Id, user.Role);
        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        ValidateStatusUpdate(request.Status);

        var user = await FindUserOrThrowAsync(userId, cancellationToken);
        user.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncStatusWithCoreAsync(user, cancellationToken);

        return user.ToResponse();
    }

    private async Task EnsureCurrentUserIsAdminAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userRole = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => (UserRole?)u.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (!userRole.HasValue)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        if (userRole.Value != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only admins can access this resource");
        }
    }

    private IQueryable<User> BuildUsersQuery(PagingQuery query)
    {
        var users = _dbContext.Users.AsNoTracking();

        if (query.Role.HasValue)
        {
            users = users.Where(x => x.Role == query.Role.Value);
        }

        return users;
    }

    private static async Task<List<User>> GetUsersPageAsync(IQueryable<User> users, PagingQuery query, CancellationToken cancellationToken)
    {
        return await users
            .OrderByDescending(x => x.CreatedAt)
            .Skip(query.Page * query.Size)
            .Take(query.Size)
            .ToListAsync(cancellationToken);
    }

    private static PageInfo BuildPageInfo(PagingQuery query, int totalElements)
    {
        var totalPages = query.Size == 0 ? 0 : (int)Math.Ceiling(totalElements / (double)query.Size);
        return new PageInfo(query.Page, query.Size, totalElements, totalPages);
    }

    private async Task EnsureEmailUniqueAsync(string email, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }
    }

    private static User BuildUser(CreateUserAdminRequest request)
    {
        return new User
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
    }

    private async Task PersistCreatedUserAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RegisterInAuthServiceAsync(User user, string password, CancellationToken cancellationToken)
    {
        var request = new RegisterCredentialsRequest(user.Id, user.Email, password, user.Role.ToString().ToUpperInvariant());
        await _authServiceClient.RegisterCredentialsAsync(request, cancellationToken);
    }

    private async Task RollbackCreatedUserAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> FindUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");
    }

    private static void ValidateStatusUpdate(UserStatus status)
    {
        if (status is not (UserStatus.Active or UserStatus.Blocked))
        {
            throw new InvalidOperationException("Only ACTIVE or BLOCKED are supported by this endpoint.");
        }
    }

    private async Task SyncStatusWithCoreAsync(User user, CancellationToken cancellationToken)
    {
        var request = new UpdateAccountStateRequest(user.Id, user.Status.ToString().ToUpperInvariant());
        await _coreServiceClient.SyncUserStatusAsync(request, cancellationToken);
    }
}
