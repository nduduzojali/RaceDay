using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

// Tests for event enrolments and results
// (brief: "Event enrolments - verifying that Participants can enrol and that the enrolment is correctly recorded")
public class EnrolmentTests : TestBase
{
    public EnrolmentTests(RaceDayFactory factory) : base(factory) { }

    // Helper: enrols a participant in a category and returns the response
    private static Task<HttpResponseMessage> Enrol(HttpClient participant, int categoryId)
        => participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);

    // ---------- Enrolling: expected success ----------

    [Fact]
    public async Task Participant_CanEnrol_Returns201()
    {
        var organiser = await LoggedInClient("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var participant = await LoggedInClient("Participant");

        var response = await Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Enrolment_IsRecorded_WithCorrectEventAndCategory()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var participant = await LoggedInClient("Participant");

        await Enrol(participant, categoryId);
        var mine = await participant.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/me");

        // The enrolment links this participant to the chosen category and its event
        var enrolment = Assert.Single(mine!);
        Assert.Equal(categoryId, enrolment.CategoryID);
        Assert.Equal(eventId, enrolment.EventID);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);
        var categoryId = await CreateCategory(organiser, eventId);
        var participant = await LoggedInClient("Participant");
        await Enrol(participant, categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(list!);
    }

    // ---------- Enrolling: expected failures ----------

    [Fact]
    public async Task Participant_EnrollingTwiceInSameCategory_Returns409()
    {
        var organiser = await LoggedInClient("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var participant = await LoggedInClient("Participant");

        await Enrol(participant, categoryId);
        var second = await Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_NonExistentCategory_Returns404()
    {
        var participant = await LoggedInClient("Participant");

        var response = await Enrol(participant, 99999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutToken_Returns401()
    {
        var client = Factory.CreateClient();

        var response = await Enrol(client, 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await LoggedInClient("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));

        var response = await Enrol(organiser, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var organiser = await LoggedInClient("Organiser");
        var eventId = await CreateEvent(organiser);
        var participant = await LoggedInClient("Participant");

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Results ----------

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanViewIt()
    {
        var organiser = await LoggedInClient("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var participant = await LoggedInClient("Participant");
        var enrolment = await (await Enrol(participant, categoryId)).Content.ReadFromJsonAsync<EnrolmentResponse>();

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentID}/results", new
        {
            finishTime = "00:50:00",
            position = 3
        });
        var view = await participant.GetAsync($"/api/enrolments/{enrolment.EnrolmentID}/results");

        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);
        Assert.Equal(HttpStatusCode.OK, view.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var organiser = await LoggedInClient("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var participant = await LoggedInClient("Participant");
        var enrolment = await (await Enrol(participant, categoryId)).Content.ReadFromJsonAsync<EnrolmentResponse>();

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentID}/results", new
        {
            finishTime = "00:40:00",
            position = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotSeeAnotherParticipantsResult_Returns403()
    {
        var organiser = await LoggedInClient("Organiser");
        var categoryId = await CreateCategory(organiser, await CreateEvent(organiser));
        var runner = await LoggedInClient("Participant");
        var enrolment = await (await Enrol(runner, categoryId)).Content.ReadFromJsonAsync<EnrolmentResponse>();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment!.EnrolmentID}/results", new
        {
            finishTime = "00:55:00",
            position = 5
        });
        var otherParticipant = await LoggedInClient("Participant");

        var response = await otherParticipant.GetAsync($"/api/enrolments/{enrolment.EnrolmentID}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
