using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Data an Organiser sends to create or update an event
public class EventDto
{
    [Required, MaxLength(150)] public string EventName { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = string.Empty;
    [Range(0.1, 1000)] public decimal Distance { get; set; }      // in km
    [Required] public string EventType { get; set; } = "Run";     // Run, Walk or Cycle
}