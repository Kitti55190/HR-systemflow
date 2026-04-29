using HrFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrFlow.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<OvertimeRequest> OvertimeRequests => Set<OvertimeRequest>();
    public DbSet<OvertimeApprovalStep> OvertimeApprovalSteps => Set<OvertimeApprovalStep>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var utcDateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTime>(
            v => v.UtcDateTime,
            v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc))
        );
        var utcNullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTime?>(
            v => v.HasValue ? v.Value.UtcDateTime : null,
            v => v.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : null
        );

        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Email).HasMaxLength(320);
            b.Property(x => x.DisplayName).HasMaxLength(200);

            b.HasOne(x => x.Manager)
                .WithMany()
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.HasIndex(x => x.Name).IsUnique();
            b.Property(x => x.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(x => new { x.UserId, x.RoleId });
            b.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            b.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<LeaveType>(b =>
        {
            b.HasIndex(x => x.Name).IsUnique();
            b.Property(x => x.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<LeaveRequest>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.StartDate, x.EndDate });
            b.Property(x => x.Reason).HasMaxLength(1000);
            var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v)
            );
            b.Property(x => x.StartDate).HasConversion(dateOnlyConverter);
            b.Property(x => x.EndDate).HasConversion(dateOnlyConverter);
            b.Property(x => x.CreatedAt).HasConversion(utcDateTimeOffsetConverter);
            b.Property(x => x.UpdatedAt).HasConversion(utcDateTimeOffsetConverter);
            b.Property(x => x.SubmittedAt).HasConversion(utcNullableDateTimeOffsetConverter);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            b.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId);
        });

        modelBuilder.Entity<ApprovalStep>(b =>
        {
            b.HasIndex(x => new { x.LeaveRequestId, x.Level }).IsUnique();
            b.Property(x => x.Comment).HasMaxLength(1000);
            b.Property(x => x.ActionAt).HasConversion(utcNullableDateTimeOffsetConverter);
            b.HasOne(x => x.LeaveRequest).WithMany(x => x.ApprovalSteps).HasForeignKey(x => x.LeaveRequestId);
            b.HasOne(x => x.ApproverUser).WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OvertimeRequest>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.StartAt, x.EndAt });
            b.Property(x => x.Reason).HasMaxLength(1000);
            b.Property(x => x.StartAt).HasConversion(utcDateTimeOffsetConverter);
            b.Property(x => x.EndAt).HasConversion(utcDateTimeOffsetConverter);
            b.Property(x => x.CreatedAt).HasConversion(utcDateTimeOffsetConverter);
            b.Property(x => x.UpdatedAt).HasConversion(utcDateTimeOffsetConverter);
            b.Property(x => x.SubmittedAt).HasConversion(utcNullableDateTimeOffsetConverter);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<OvertimeApprovalStep>(b =>
        {
            b.HasIndex(x => new { x.OvertimeRequestId, x.Level }).IsUnique();
            b.Property(x => x.Comment).HasMaxLength(1000);
            b.Property(x => x.ActionAt).HasConversion(utcNullableDateTimeOffsetConverter);
            b.HasOne(x => x.OvertimeRequest).WithMany(x => x.ApprovalSteps).HasForeignKey(x => x.OvertimeRequestId);
            b.HasOne(x => x.ApproverUser).WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(b =>
        {
            b.HasIndex(x => new { x.UserId, x.CreatedAt });
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.Body).HasMaxLength(2000);
            b.Property(x => x.CreatedAt).HasConversion(utcDateTimeOffsetConverter);
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
    }
}
