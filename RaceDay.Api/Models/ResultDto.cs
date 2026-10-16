using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

// Data an Organiser sends to record a participant's result
public class ResultDto
{
    [Required, MaxLength(20)] public string FinishTime { get; set; } = string.Empty; // e.g. 01:45:32
    [Range(1, 1000000)] public int? Position { get; set; }
}