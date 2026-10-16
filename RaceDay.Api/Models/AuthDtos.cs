using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Data a new user must send to register
public class RegisterDto
{
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = "Participant"; // "Organiser" or "Participant"
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public string? OrganiserCode { get; set; } // only required when Role is Organiser
}

// Data a user sends to log in
public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

// Data a user can change on their own profile (role and email cannot be changed)
public class UpdateProfileDto
{
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [MaxLength(20)] public string? PhoneNumber { get; set; }
}