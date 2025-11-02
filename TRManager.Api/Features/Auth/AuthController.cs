using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TRManager.Api.Features.Auth.Dtos;
using System.Linq;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        => Ok(await _auth.RegisterAsync(req));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        => Ok(await _auth.LoginAsync(req, HttpContext.Connection.RemoteIpAddress?.ToString()));

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] string refreshToken)
        => Ok(await _auth.RefreshAsync(refreshToken));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await _auth.LogoutAsync(refreshToken);
        return NoContent();
    }
        [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(claims);
    }

}

