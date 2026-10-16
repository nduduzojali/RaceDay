using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize] // logged-in users only
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Records a participant's finish time and position (Organiser who owns the event only).</summary>
    /// <remarks>Send finishTime and position. Returns 201, 400, 403 (not an Organiser, or not your event), 404 (enrolment not found) or 409 (result already recorded).</remarks>
    [HttpPost("api/enrolments/{id}/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Capture(int id, ResultDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment == null) return NotFound("Enrolment not found.");

        // Results can only be captured for events the Organiser created
        if (enrolment.Category!.Event!.OrganiserID != User.GetUserId()) return Forbid();

        // One result per enrolment (the one-to-one relationship from the ERD)
        if (await _db.Results.AnyAsync(r => r.EnrolmentID == id))
            return Conflict("A result has already been recorded for this enrolment.");

        var result = new Result
        {
            EnrolmentID = id,
            FinishTime = dto.FinishTime.Trim(),
            Position = dto.Position
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            result.ResultID,
            result.EnrolmentID,
            result.FinishTime,
            result.Position
        });
    }

    /// <summary>Returns the result for an enrolment (the participant who owns it, or the Organiser of that event).</summary>
    /// <remarks>No body needed. Returns 200, 403 (not your result) or 404 (enrolment or result not found).</remarks>
    [HttpGet("api/enrolments/{id}/results")]
    public async Task<IActionResult> GetForEnrolment(int id)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment == null) return NotFound("Enrolment not found.");

        var userId = User.GetUserId();

        // Participants may only see their own results, Organisers only results for their own events
        var allowed = User.IsInRole("Organiser")
            ? enrolment.Category!.Event!.OrganiserID == userId
            : enrolment.UserID == userId;
        if (!allowed) return Forbid();

        var result = await _db.Results.FirstOrDefaultAsync(r => r.EnrolmentID == id);
        if (result == null) return NotFound("No result has been recorded yet.");

        return Ok(new { result.ResultID, result.EnrolmentID, result.FinishTime, result.Position });
    }
}