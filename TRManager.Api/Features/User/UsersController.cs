using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TRManager.Api.Features.User.Dtos;

namespace TRManager.Api.Features.User
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;
        public UsersController(IUserService users) => _users = users;

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetAll() => Ok(await _users.GetAllAsync());

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<UserDto?>> GetById(Guid id) => Ok(await _users.GetByIdAsync(id));

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateUserRequest req)
        { 
            await _users.UpdateAsync(id, req);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _users.DeleteSoftAsync(id);
            return NoContent();
        }
    }
}
