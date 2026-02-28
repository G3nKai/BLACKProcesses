using UserService.Domain.Enums;

namespace UserService.Services;

public sealed record RegisterCredentialsRequest(Guid UserId, string Email, string Password, UserRole Role);
public sealed record UpdateAccountStateRequest(Guid UserId, UserStatus Status);
