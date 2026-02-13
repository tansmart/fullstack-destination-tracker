using Xunit;
using backend.Controllers;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace backend.Tests;

public class AuthControllerTests
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        _dbContext = new AppDbContext(options);

        var store = new UserStore<ApplicationUser>(_dbContext);
        _userManager = new UserManager<ApplicationUser>(
            store,
            null!,
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            null!
        );

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "a-string-secret-at-least-256-bits-long",
                ["Jwt:Issuer"] = "travelwishlist",
                ["Jwt:Audience"] = "travelwishlist"
            })
            .Build();
    }

    [Fact]
    public async Task Register_ShouldCreateUserAndReturnJwt()
    {
        // Arrange
        var controller = new AuthController(_userManager, _config, _dbContext);
        var dto = new RegisterDto("test@example.com", "Password123!");

        // Act
        var result = await controller.Register(dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(_dbContext.Users);
    }
}
