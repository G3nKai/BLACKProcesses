using System.ComponentModel.DataAnnotations;
using UserService.Domain.Enums;

namespace UserService.Contracts.Requests;

public sealed record CreateUserAdminRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string FirstName,
    [property: Required] string LastName,
    string? Phone,
    [property: Required] UserRole Role,
    [property: Required, MinLength(8)] string Password);
