using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Register the database context so controllers can use it (EF Core + SQL Server)
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger: shows every endpoint in the browser and lets us paste a login token to test protected ones
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for managing road running, walking and cycling events."
    });

    // Pull in the /// summary comments from our controllers so each endpoint has a description
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    // Adds the "Authorize" button to Swagger for pasting the JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token returned by /api/auth/login (without the word Bearer)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Authentication: every request carries a signed JWT, which keeps the user's identity and role
// across requests without the server storing a session
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep our claim names exactly as we wrote them ("role", "uid")
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            RoleClaimType = "role", // tells [Authorize(Roles = "...")] which claim holds the role
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger is always on so the lecturer can test endpoints in the browser
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Order matters: authenticate (who are you?) before authorize (are you allowed?)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Lets the test project start the whole API in memory later
public partial class Program { }