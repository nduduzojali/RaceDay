using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize] // logged-in users only
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Adds a category (e.g. 10km, 21km, Under 20) to an event (Organiser who owns the event only).</summary>
    /// <remarks>Send categoryName, maxParticipants and entryFee. Returns 201, 400, 403 (not an Organiser, or not your event) or 404 (event not found).</remarks>
    [HttpPost("api/events/{id}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int id, CategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound("Event not found.");

        // An Organiser can only add categories to their own events
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var category = new Category
        {
            EventID = id,
            CategoryName = dto.CategoryName.Trim(),
            MaxParticipants = dto.MaxParticipants,
            EntryFee = dto.EntryFee
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            category.CategoryID,
            category.EventID,
            category.CategoryName,
            category.MaxParticipants,
            category.EntryFee
        });
    }

    /// <summary>Lists all categories of an event (any logged-in user).</summary>
    /// <remarks>No body needed. Returns 200 with the categories, or 404 if the event does not exist.</remarks>
    [HttpGet("api/events/{id}/categories")]
    public async Task<IActionResult> GetForEvent(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == id))
            return NotFound("Event not found.");

        var categories = await _db.Categories
            .Where(c => c.EventID == id)
            .Select(c => new { c.CategoryID, c.EventID, c.CategoryName, c.MaxParticipants, c.EntryFee })
            .ToListAsync();

        return Ok(categories);
    }
}