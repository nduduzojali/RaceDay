using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize] // logged-in users only; Organiser-only actions add their own role check below
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    // Only these three types are allowed, as the brief says (run, walk or cycle)
    private static string? NormaliseType(string type) => type.Trim().ToLower() switch
    {
        "run" => "Run",
        "walk" => "Walk",
        "cycle" => "Cycle",
        _ => null
    };

    /// <summary>Creates a new event (Organiser only).</summary>
    /// <remarks>
    /// Send eventName, description, eventDate, location, distance and eventType (Run, Walk or Cycle).
    /// Returns 201 with the event, 400 for bad input, 403 if the caller is not an Organiser.
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(EventDto dto)
    {
        var type = NormaliseType(dto.EventType);
        if (type == null) return BadRequest("EventType must be Run, Walk or Cycle.");

        var ev = new Event
        {
            OrganiserID = User.GetUserId(), // the creator becomes the owner, taken from the token
            EventName = dto.EventName.Trim(),
            Description = dto.Description,
            EventDate = dto.EventDate.Date,
            Location = dto.Location.Trim(),
            Distance = dto.Distance,
            EventType = type
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, ToResponse(ev));
    }

    /// <summary>Lists all upcoming events (any logged-in user).</summary>
    /// <remarks>No body needed. Returns 200 with the events ordered by date.</remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events
            .Where(e => e.EventDate >= DateTime.Today)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToResponse));
    }

    /// <summary>Returns the details of one event, including its categories (any logged-in user).</summary>
    /// <remarks>Returns 200 with the event or 404 if it does not exist.</remarks>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null) return NotFound("Event not found.");

        return Ok(new
        {
            ev.EventID,
            ev.OrganiserID,
            ev.EventName,
            ev.Description,
            ev.EventDate,
            ev.Location,
            ev.Distance,
            ev.EventType,
            Categories = ev.Categories.Select(c => new { c.CategoryID, c.CategoryName, c.MaxParticipants, c.EntryFee })
        });
    }

    /// <summary>Updates an event (Organiser who owns it only).</summary>
    /// <remarks>Send the same fields as when creating. Returns 200, 400, 403 (not an Organiser, or not your event) or 404.</remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var type = NormaliseType(dto.EventType);
        if (type == null) return BadRequest("EventType must be Run, Walk or Cycle.");

        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");

        // An Organiser may only change events they created themselves
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        ev.EventName = dto.EventName.Trim();
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate.Date;
        ev.Location = dto.Location.Trim();
        ev.Distance = dto.Distance;
        ev.EventType = type;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(ev));
    }

    /// <summary>Deletes an event and its categories and enrolments (Organiser who owns it only).</summary>
    /// <remarks>No body needed. Returns 200, 403 (not an Organiser, or not your event) or 404.</remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return Ok("Event deleted.");
    }

    // Shapes an event for the response so we never send the whole entity graph
    private static object ToResponse(Event e) => new
    {
        e.EventID,
        e.OrganiserID,
        e.EventName,
        e.Description,
        e.EventDate,
        e.Location,
        e.Distance,
        e.EventType
    };
}