using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

// Tests for user registration and login (brief: "User registration and login")
public class AuthTests : TestBase
{
    public AuthTests(RaceDayFactory factory) : base(factory) { }

    // ---------- Registration: expected success ----------

    [Fact]
    public async Task Register_NewParticipant_Returns201()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Participant",
            email = NewEmail(),
            password = "Test1234",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithCorrectCode_Returns201()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Organiser",
            email = NewEmail(),
            password = "Test1234",
            role = "Organiser",
            organiserCode = OrganiserCode
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = Factory.CreateClient();
        var email = NewEmail();

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Hash Check",
            email,
            password = "Test1234",
            role = "Participant"
        });

        // Look directly in the database to see what was stored
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = db.Users.Single(u => u.Email == email);

        Assert.NotEqual("Test1234", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Test1234", user.PasswordHash));
    }

    // ---------- Registration: expected failures ----------

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = Factory.CreateClient();
        var body = new { fullName = "Dup User", email = NewEmail(), password = "Test1234", role = "Participant" };

        await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithWrongCode_Returns403()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Fake Organiser",
            email = NewEmail(),
            password = "Test1234",
            role = "Organiser",
            organiserCode = "WRONG-CODE"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Admin Wannabe",
            email = NewEmail(),
            password = "Test1234",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Short Pass",
            email = NewEmail(),
            password = "123",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndRole()
    {
        var client = Factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login User",
            email,
            password = "Test1234",
            role = "Participant"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test1234" });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Participant", body.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = Factory.CreateClient();
        var email = NewEmail();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login User",
            email,
            password = "Test1234",
            role = "Participant"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = NewEmail(),
            password = "Test1234"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
