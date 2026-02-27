using UserService.Domain.Enums;

namespace UserService.Contracts.Requests;

public sealed record PagingQuery(int Page = 0, int Size = 20, UserRole? Role = null);
