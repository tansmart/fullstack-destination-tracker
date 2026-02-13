using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
/* Controller to handle user authentication */
public class AuthController(UserManager<ApplicationUser> userManager, IConfiguration config, AppDbContext context) : ControllerBase
{
    /** User manager for handling user operations */
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    /** Configuration for accessing app settings */
    private readonly IConfiguration _config = config;

    /** Database context for accessing the database */
    private readonly AppDbContext _context = context;

    [HttpPost("register")]
    /* Endpoint to register a new user */
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return await CreateJwtResponse(user);
    }

    [HttpPost("login")]
    /* Endpoint to authenticate a user and issue a JWT */
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized();
        return await CreateJwtResponse(user);
    }

    [HttpPost("refresh")]
    /* Endpoint to refresh JWT using a refresh token */
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
    {
        // Clean up expired refresh tokens
        _context.RefreshTokens.RemoveRange(
            _context.RefreshTokens.Where(r => r.ExpiresAt < DateTime.UtcNow)
        );
        await _context.SaveChangesAsync();

        // Validate the provided refresh token
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null) return Unauthorized();

        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        var newAccessToken = CreateAndWriteAccessToken(user);
        var newRefreshToken = await CreateAndStoreRefreshToken(user);

        return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
    }

    [HttpPost("revoke")]
    /* Endpoint to revoke a refresh token */
    public async Task<IActionResult> Revoke([FromBody] RefreshRequestDto dto)
    {
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);
        if (storedToken == null) return NotFound();

        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Token revoked" });
    }

    /* Helper method to create JWT access token */
    private string CreateAndWriteAccessToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim("email", user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /* Helper method to create and store a refresh token */
    private async Task<string> CreateAndStoreRefreshToken(ApplicationUser user)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return token;
    }

    /* Helper method to create JWT response with access and refresh tokens */
    private async Task<IActionResult> CreateJwtResponse(ApplicationUser user)
    {
        var accessToken = CreateAndWriteAccessToken(user);
        var refreshToken = await CreateAndStoreRefreshToken(user);

        return Ok(new { accessToken, refreshToken });
    }
}

/* Data Transfer Object for user registration */
public record RegisterDto(string Email, string Password);
/* Data Transfer Object for user login */
public record LoginDto(string Email, string Password);
/* Data Transfer Object for refresh token request */
public record RefreshRequestDto(string RefreshToken);
