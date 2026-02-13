using System.Security.Claims;
using System.Text;
using System.Text.Json;
using backend;
using backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

/** Add services to the container. */
builder.Services.AddControllers();
/** Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/swashbuckle */
builder.Services.AddEndpointsApiExplorer();
/** Add OpenAPI support */
builder.Services.AddOpenApi();

/** Configure SQLite database */
var dbPath = Path.Combine(AppContext.BaseDirectory, "travelwishlist.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}")
);

/** Configure CORS to allow requests from the frontend */
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // Use origins from config
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

/** Configure JSON options for consistent serialization */
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});
/** Configure Identity for user management */
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
/** Configure JWT authentication */
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
/* Configure JWT Bearer options */
.AddJwtBearer(options =>
{
    var jwtSection = builder.Configuration.GetRequiredSection("Jwt");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
});

/** Build the app */
var app = builder.Build();

/** Ensure database is created */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

/** Configure the HTTP request pipeline. */
if (app.Environment.IsDevelopment())
{
    /** Enable OpenAPI middleware */
    app.MapOpenApi();
}

/** Enable CORS */
app.UseCors();

/** Redirect HTTP to HTTPS */
app.UseHttpsRedirection();
/** Enable authentication and authorization */
app.UseAuthentication();
app.UseAuthorization();
/** Map controller routes */
app.MapControllers();
/** Run the application */
app.Run();
