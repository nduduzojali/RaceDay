using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Data an Organiser sends to add a category (e.g. "10km" or "Under 20") to an event
public class CategoryDto
{
    [Required, MaxLength(100)] public string CategoryName { get; set; } = string.Empty;
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 100;
    [Range(0, 100000)] public decimal EntryFee { get; set; }
}