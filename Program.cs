using Microsoft.EntityFrameworkCore;
using QueueBookingAPI.Data;
using QueueBookingAPI.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "server=localhost;port=3306;database=QueueBookingDB;user=root;password=800186Kpm;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "QueueBookingAPI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "QueueBookingClient",
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!")),
        RoleClaimType = ClaimTypes.Role
    };
    
    // Map role claim from JWT token to ASP.NET Core role claim
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var claimsIdentity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
            if (claimsIdentity != null)
            {
                // Check for role claim with short name and map it to ClaimTypes.Role
                var roleClaim = claimsIdentity.FindFirst("role");
                if (roleClaim != null && !claimsIdentity.HasClaim(System.Security.Claims.ClaimTypes.Role, roleClaim.Value))
                {
                    claimsIdentity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleClaim.Value));
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Add policy for Admin role (case-insensitive)
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireAssertion(context =>
        {
            var roleClaim = context.User.FindFirst(ClaimTypes.Role);
            if (roleClaim != null)
            {
                return string.Equals(roleClaim.Value, "Admin", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }));
});

// Services
builder.Services.AddScoped<QueueBookingService>();
builder.Services.AddScoped<QueueTrackingService>();
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

