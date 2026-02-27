using UserService.Domain;

namespace UserService.Contracts.Responses;

public static class MappingExtensions
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Phone, user.Role, user.Status, user.CreatedAt);
}
