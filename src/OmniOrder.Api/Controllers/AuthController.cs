using Microsoft.AspNetCore.Mvc;
using OmniOrder.Application.DTOs;
using OmniOrder.Application.Interfaces;

namespace OmniOrder.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>
    /// Quick-login endpoint: generates a JWT token for testing.
    /// Supply username and role (Admin or Customer).
    /// </summary>
    [HttpPost("demo-token")]
    public IActionResult GetDemoToken([FromBody] DemoTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new ProblemDetails { Title = "Username is required", Status = 400 });

        if (request.Role != "Admin" && request.Role != "Customer")
            return BadRequest(new ProblemDetails { Title = "Role must be 'Admin' or 'Customer'", Status = 400 });

        var token = _tokenService.GenerateDemoToken(request);
        return Ok(token);
    }
}
