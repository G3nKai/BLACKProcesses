using System.ComponentModel.DataAnnotations;
using UserService.Domain.Enums;

namespace UserService.Contracts.Requests;

public sealed record UpdateUserStatusRequest(
    [property: Required] UserStatus Status);
