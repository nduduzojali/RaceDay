using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <remarks>
    /// Send fullName, email, password, role and optional phoneNumber.
    /// Organisers must also send the organiserCode. Returns 201 on success,
    /// 400 for bad input, 403 for a wrong organiser code, 409 if the email is already used.
    /// </remarks>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        // Only the two roles from the brief are allowed
        var role = dto.Role.Trim().ToLower() switch
        {
            "organiser" => "Organiser",
            "participant" => "Participant",
            _ => null
        };
        if (role == null)
            return BadRequest("Role must be either 'Organiser' or 'Participant'.");

        // Stops a Participant from simply choosing to be an Organiser
        if (role == "Organiser" && dto.OrganiserCode != _config["OrganiserRegistrationCode"])
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid organiser registration code.");

        // Emails must be unique, matching the UNIQUE constraint in the database
        var email = dto.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("An account with this email already exists.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            // Password is hashed with BCrypt and the original is never stored
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            PhoneNumber = dto.PhoneNumber
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new { user.UserID, user.FullName, user.Email, user.Role });
    }

    /// <summary>Logs a user in and returns a JWT token containing their role.</summary>
    /// <remarks>
    /// Send email and password. Returns 200 with a token to use in the Authorize button,
    /// or 401 if the credentials are wrong.
    /// </remarks>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so attackers can't tell which was wrong
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        var token = CreateToken(user, out var expires);
        return Ok(new { token, role = user.Role, expires });
    }

    // Builds a signed token holding the user's id, name and role.
    // This token is how the API remembers who the user is on every later request.
    private string CreateToken(User user, out DateTime expires)
    {
        var jwt = _config.GetSection("Jwt");
        expires = DateTime.UtcNow.AddHours(2);

        var claims = new[]
        {
            new Claim("uid", user.UserID.ToString()),
            new Claim("name", user.FullName),
            new Claim("role", user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}