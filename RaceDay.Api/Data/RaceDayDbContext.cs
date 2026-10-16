using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // No two accounts can share an email (matches UNIQUE in the SQL script)
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        // A participant cannot enrol in the same category twice
        modelBuilder.Entity<Enrolment>().HasIndex(e => new { e.UserID, e.CategoryID }).IsUnique();

        // One-to-one: each enrolment has at most one result and one payment
        modelBuilder.Entity<Result>().HasIndex(r => r.EnrolmentID).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(p => p.EnrolmentID).IsUnique();

        // SQL Server rejects multiple cascade paths from User, so these two are restricted
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser).WithMany().HasForeignKey(e => e.OrganiserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}