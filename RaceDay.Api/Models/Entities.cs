using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

[Table("User")]
public class User
{
    [Key] public int UserID { get; set; }
    [Required, MaxLength(100)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Role { get; set; } = "Participant"; // Organiser or Participant
    [MaxLength(20)] public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("Event")]
public class Event
{
    [Key] public int EventID { get; set; }
    public int OrganiserID { get; set; }
    [ForeignKey(nameof(OrganiserID))] public User? Organiser { get; set; }
    [Required, MaxLength(150)] public string EventName { get; set; } = string.Empty;
    [Column(TypeName = "date")] public DateTime EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    [Column(TypeName = "decimal(6,2)")] public decimal Distance { get; set; } // in km
    [Required, MaxLength(20)] public string EventType { get; set; } = "Run"; // Run, Walk or Cycle
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Category> Categories { get; set; } = new();
}

[Table("Category")]
public class Category
{
    [Key] public int CategoryID { get; set; }
    public int EventID { get; set; }
    [ForeignKey(nameof(EventID))] public Event? Event { get; set; }
    [Required, MaxLength(100)] public string CategoryName { get; set; } = string.Empty;
    public int MaxParticipants { get; set; } = 100;
    [Column(TypeName = "decimal(10,2)")] public decimal EntryFee { get; set; }
}

[Table("Enrolment")]
public class Enrolment
{
    [Key] public int EnrolmentID { get; set; }
    public int UserID { get; set; }
    [ForeignKey(nameof(UserID))] public User? User { get; set; }
    public int CategoryID { get; set; }
    [ForeignKey(nameof(CategoryID))] public Category? Category { get; set; }
    [Column(TypeName = "date")] public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(20)] public string Status { get; set; } = "Confirmed";
}

[Table("Result")]
public class Result
{
    [Key] public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    [ForeignKey(nameof(EnrolmentID))] public Enrolment? Enrolment { get; set; }
    [Required, MaxLength(20)] public string FinishTime { get; set; } = string.Empty; // e.g. 01:45:32
    public int? Position { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

[Table("Payment")]
public class Payment
{
    [Key] public int PaymentID { get; set; }
    public int EnrolmentID { get; set; }
    [ForeignKey(nameof(EnrolmentID))] public Enrolment? Enrolment { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Amount { get; set; }
    [Column(TypeName = "date")] public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    [MaxLength(50)] public string? PaymentMethod { get; set; }
    [Required, MaxLength(20)] public string PaymentStatus { get; set; } = "Paid";
}