using HrFlow.Api.Data;
using HrFlow.Api.Dtos;
using HrFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AppDbContext db, PasswordHasher passwordHasher, JwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsActive);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized();
        }

        var roles = await _db.UserRoles
            .Where(x => x.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();

        var token = _jwtTokenService.CreateToken(user, roles, DateTimeOffset.UtcNow);
        return new LoginResponse(token, new UserDto(user.Id, user.Email, user.DisplayName, roles));
    }
}
