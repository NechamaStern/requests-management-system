using Microsoft.AspNetCore.Mvc;
using RequestsManagement.Bll.Mapping.Dtos;
using RequestsManagement.Bll.Services;

namespace RequestsManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(IUsersService usersService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await usersService.GetActiveUsersAsync(cancellationToken));
    }
}
