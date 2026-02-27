using UserService.Domain;

namespace UserService.Contracts;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    UserRole Role,
    UserStatus Status,
    DateTimeOffset CreatedAt);

public sealed record PageInfo(int Page, int Size, int TotalElements, int TotalPages);
public sealed record UsersResponse(IReadOnlyCollection<UserResponse> Content, PageInfo Page);

public static class MappingExtensions
{
    public static UserResponse ToResponse(this UserProfile user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Phone, user.Role, user.Status, user.CreatedAt);
}
