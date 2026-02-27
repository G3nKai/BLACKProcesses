using UserService.Domain.Enums;

namespace UserService.Contracts.Responses;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    UserRole Role,
    UserStatus Status,
    DateTimeOffset CreatedAt);
