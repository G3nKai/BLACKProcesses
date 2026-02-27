using System.ComponentModel.DataAnnotations;
using UserService.Domain;

namespace UserService.Contracts;

public sealed record CreateUserAdminRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string FirstName,
    [property: Required] string LastName,
    string? Phone,
    [property: Required] UserRole Role,
    [property: Required, MinLength(8)] string Password);

public sealed record UpdateUserStatusRequest(
    [property: Required] UserStatus Status);

public sealed record PagingQuery(int Page = 0, int Size = 20, UserRole? Role = null);
