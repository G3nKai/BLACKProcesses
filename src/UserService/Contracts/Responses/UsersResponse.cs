using UserService.Contracts.Common;

namespace UserService.Contracts.Responses;

public sealed record UsersResponse(IReadOnlyCollection<UserResponse> Content, PageInfo Page);
