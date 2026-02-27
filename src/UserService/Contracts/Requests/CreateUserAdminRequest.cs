using System.ComponentModel.DataAnnotations;
using UserService.Domain.Enums;

namespace UserService.Contracts.Requests;

public sealed record CreateUserAdminRequest(
    [param: Required, EmailAddress] string Email,
    [param: Required] string FirstName,
    [param: Required] string LastName,
    string? Phone,
    [param: Required] UserRole Role,
    [param: Required, MinLength(8)] string Password);
