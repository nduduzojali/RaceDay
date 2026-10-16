using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

// Tests for event management and role-based access
// (brief: "Event management" and "Different roles are rejected")
public class EventTests : TestBase
{
    public EventTests(RaceDayFactory factory) : base(factory) { }

    // ---------- Unauthenticated requests ----------

    [Fact]
    public async Task ViewEvents_WithoutToken_Returns401()
    {
        var client = Factory.CreateClient(); // no token attached

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutToken_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Organiser can manage events ----------

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var organiser = await LoggedInClient("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Renamed Event",
            description = "Updated",
            eventDate = DateTime.Today.AddDays(40),
            location = "Pretoria",
            distance = 21,
            eventType = "Cycle"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_ThenItIsGone()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        var afterwards = await organiser.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await LoggedInClient("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Swim Gala",
            description = "Not allowed",
            eventDate = DateTime.Today.AddDays(30),
            location = "Cape Town",
            distance = 5,
            eventType = "Swim" // only Run, Walk or Cycle are allowed
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Both roles can view events ----------

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task BothRoles_CanViewEvents_Returns200(string role)
    {
        var client = await LoggedInClient(role);

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ---------- Role rejection ----------

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await LoggedInClient("Participant");

        var response = await participant.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotDeleteEvent_Returns403()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);
        var participant = await LoggedInClient("Participant");

        var response = await participant.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(owner);
        var otherOrganiser = await LoggedInClient("Organiser");

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(owner);
        var otherOrganiser = await LoggedInClient("Organiser");

        var response = await otherOrganiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
