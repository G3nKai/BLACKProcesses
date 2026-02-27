namespace UserService.Services;

public sealed record RegisterCredentialsRequest(Guid UserId, string Email, string Password, string Role);
public sealed record UpdateAccountStateRequest(Guid UserId, string Status);
