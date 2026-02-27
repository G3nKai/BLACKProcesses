using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Contracts.Requests;
using UserService.Contracts.Responses;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("admin/users")]
[Tags("User Service")]
[Authorize(Roles = "ADMIN,EMPLOYEE")]
public sealed class UserController(IUserManagementService userManagementService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UsersResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UsersResponse>> GetUsers([FromQuery] PagingQuery query, CancellationToken cancellationToken)
    {
        var response = await userManagementService.GetUsersAsync(query, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserAdminRequest request, CancellationToken cancellationToken)
    {
        var response = await userManagementService.CreateUserAsync(request, cancellationToken);
        return Created($"/admin/users/{response.Id}", response);
    }

    [HttpPatch("{userId:guid}/status")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserResponse>> UpdateStatus(Guid userId, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await userManagementService.UpdateStatusAsync(userId, request, cancellationToken);
        return Ok(response);
    }
}
