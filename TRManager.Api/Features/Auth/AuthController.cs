using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TRManager.Api.Features.Auth.Dtos;

namespace TRManager.Api.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        => Ok(await _auth.RegisterAsync(req, HttpContext.Connection.RemoteIpAddress?.ToString()));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        => Ok(await _auth.LoginAsync(req, HttpContext.Connection.RemoteIpAddress?.ToString()));

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] string refreshToken)
        => Ok(await _auth.RefreshAsync(refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString()));

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] string refreshToken)
    {
        await _auth.LogoutAsync(refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString());
        return NoContent();
    }

    // tiện ích xem claim
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(claims);
    }
}
