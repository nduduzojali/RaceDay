using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize] // logged-in users only
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrols the logged-in Participant into a category (Participant only).</summary>
    /// <remarks>No body needed, the participant comes from the token. Returns 201, 403 (not a Participant), 404 (category not found) or 409 (already enrolled, or category full).</remarks>
    [HttpPost("api/categories/{id}/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null) return NotFound("Category not found.");

        var userId = User.GetUserId();

        // The same participant cannot enter the same category twice
        if (await _db.Enrolments.AnyAsync(e => e.UserID == userId && e.CategoryID == id))
            return Conflict("You are already enrolled in this category.");

        // Respect the category's participant limit
        var taken = await _db.Enrolments.CountAsync(e => e.CategoryID == id);
        if (taken >= category.MaxParticipants)
            return Conflict("This category is full.");

        // This row records the link between the participant, the category and (through it) the event
        var enrolment = new Enrolment
        {
            UserID = userId,
            CategoryID = id,
            EnrolmentDate = DateTime.UtcNow.Date,
            Status = "Confirmed"
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            enrolment.EnrolmentID,
            enrolment.UserID,
            enrolment.CategoryID,
            category.CategoryName,
            category.EventID,
            category.Event!.EventName,
            enrolment.EnrolmentDate,
            enrolment.Status
        });
    }

    /// <summary>Returns all enrolments of the logged-in Participant (Participant only).</summary>
    /// <remarks>No body needed. Returns 200 with the participant's own enrolments, or 403 for Organisers.</remarks>
    [HttpGet("api/enrolments/me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.GetUserId();

        var enrolments = await _db.Enrolments
            .Where(e => e.UserID == userId)
            .Select(e => new
            {
                e.EnrolmentID,
                e.CategoryID,
                e.Category!.CategoryName,
                e.Category.EventID,
                e.Category.Event!.EventName,
                e.Category.Event.EventDate,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Returns all enrolments for one event (Organiser who owns the event only).</summary>
    /// <remarks>No body needed. Returns 200, 403 (not an Organiser, or not your event) or 404 (event not found).</remarks>
    [HttpGet("api/events/{id}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetForEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var enrolments = await _db.Enrolments
            .Where(e => e.Category!.EventID == id)
            .Select(e => new
            {
                e.EnrolmentID,
                e.UserID,
                ParticipantName = e.User!.FullName,
                e.CategoryID,
                e.Category!.CategoryName,
                e.EnrolmentDate,
                e.Status
            })
            .ToListAsync();

        return Ok(enrolments);
    }
}