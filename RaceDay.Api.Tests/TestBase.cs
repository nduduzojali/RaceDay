using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

// Starts the real API inside the test run, but swaps SQL Server for an in-memory database.
// That way tests run fast, need no SQL Server, and never touch the real RaceDayApiDB data.
public class RaceDayFactory : WebApplicationFactory<Program>
{
    // Unique name so each test class gets its own empty database
    private readonly string _dbName = "RaceDayTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration made in Program.cs...
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // ...and register an in-memory database instead
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}

// Shared helpers so each test stays short and readable
public abstract class TestBase : IClassFixture<RaceDayFactory>
{
    protected readonly RaceDayFactory Factory;

    // Same organiser code as appsettings.json
    protected const string OrganiserCode = "RACEDAY-ORG-2026";

    protected TestBase(RaceDayFactory factory) => Factory = factory;

    // Shapes of the JSON the API sends back, used to read ids out of responses
    protected record LoginResponse(string Token, string Role);
    protected record EventResponse(int EventID);
    protected record CategoryResponse(int CategoryID);
    protected record EnrolmentResponse(int EnrolmentID, int CategoryID, int EventID);

    // A new unique email each time, so tests never clash with each other
    protected static string NewEmail() => $"user{Guid.NewGuid():N}@test.com";

    // Registers a new user, logs in, and returns a client that sends the JWT with every request
    protected async Task<HttpClient> LoggedInClient(string role)
    {
        var client = Factory.CreateClient();
        var email = NewEmail();

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Test1234",
            role,
            organiserCode = role == "Organiser" ? OrganiserCode : null
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Test1234" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    // Valid event body used by several tests
    protected static object ValidEvent() => new
    {
        eventName = "Test 10km Run",
        description = "Test event",
        eventDate = DateTime.Today.AddDays(30),
        location = "Johannesburg",
        distance = 10,
        eventType = "Run"
    };

    // Creates an event as the given Organiser and returns its id
    protected async Task<int> CreateEvent(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent());
        response.EnsureSuccessStatusCode();
        var ev = await response.Content.ReadFromJsonAsync<EventResponse>();
        return ev!.EventID;
    }

    // Creates a category under an event as the given Organiser and returns its id
    protected async Task<int> CreateCategory(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "10km",
            maxParticipants = 50,
            entryFee = 150
        });
        response.EnsureSuccessStatusCode();
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return category!.CategoryID;
    }
}
