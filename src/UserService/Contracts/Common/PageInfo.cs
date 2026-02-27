namespace UserService.Contracts.Common;

public sealed record PageInfo(int Page, int Size, int TotalElements, int TotalPages);
