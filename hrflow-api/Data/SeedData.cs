using HrFlow.Api.Models;
using HrFlow.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HrFlow.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, PasswordHasher passwordHasher)
    {
        var roleEmployee = await EnsureRoleAsync(db, "Employee");
        var roleManager = await EnsureRoleAsync(db, "Manager");
        var roleHr = await EnsureRoleAsync(db, "HR");

        var hr = await EnsureUserAsync(db, passwordHasher, "hr@demo.com", "HR Demo");
        var manager = await EnsureUserAsync(db, passwordHasher, "manager@demo.com", "Manager Demo");
        var employee = await EnsureUserAsync(db, passwordHasher, "employee@demo.com", "Employee Demo");

        if (employee.ManagerId != manager.Id)
        {
            employee.ManagerId = manager.Id;
        }

        await EnsureUserRoleAsync(db, hr.Id, roleHr.Id);
        await EnsureUserRoleAsync(db, hr.Id, roleEmployee.Id);
        await EnsureUserRoleAsync(db, manager.Id, roleManager.Id);
        await EnsureUserRoleAsync(db, manager.Id, roleEmployee.Id);
        await EnsureUserRoleAsync(db, employee.Id, roleEmployee.Id);

        var annual = await EnsureLeaveTypeAsync(db, "Annual Leave", requiresAttachment: false);
        await EnsureLeaveTypeAsync(db, "Sick Leave", requiresAttachment: true);

        if (!await db.LeaveRequests.AnyAsync())
        {
            var now = DateTimeOffset.UtcNow;
            var request = new LeaveRequest
            {
                Id = Guid.NewGuid(),
                UserId = employee.Id,
                LeaveTypeId = annual.Id,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(3)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(4)),
                Reason = "Family trip",
                Status = LeaveRequestStatus.Pending,
                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.LeaveRequests.Add(request);
            db.ApprovalSteps.AddRange(
                new ApprovalStep { Id = Guid.NewGuid(), LeaveRequestId = request.Id, Level = 1, ApproverUserId = manager.Id, Status = ApprovalStepStatus.Pending },
                new ApprovalStep { Id = Guid.NewGuid(), LeaveRequestId = request.Id, Level = 2, ApproverUserId = hr.Id, Status = ApprovalStepStatus.Pending }
            );
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = manager.Id,
                Title = "New leave request",
                Body = $"{employee.DisplayName} submitted a leave request."
            });
        }

        if (!await db.OvertimeRequests.AnyAsync())
        {
            var now = DateTimeOffset.UtcNow;
            var startAt = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1).AddHours(18), TimeSpan.Zero);
            var endAt = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1).AddHours(20), TimeSpan.Zero);

            var ot = new OvertimeRequest
            {
                Id = Guid.NewGuid(),
                UserId = employee.Id,
                StartAt = startAt,
                EndAt = endAt,
                Reason = "Month-end report",
                Status = OvertimeRequestStatus.Pending,
                SubmittedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.OvertimeRequests.Add(ot);
            db.OvertimeApprovalSteps.AddRange(
                new OvertimeApprovalStep { Id = Guid.NewGuid(), OvertimeRequestId = ot.Id, Level = 1, ApproverUserId = manager.Id, Status = ApprovalStepStatus.Pending },
                new OvertimeApprovalStep { Id = Guid.NewGuid(), OvertimeRequestId = ot.Id, Level = 2, ApproverUserId = hr.Id, Status = ApprovalStepStatus.Pending }
            );
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = manager.Id,
                Title = "New OT request",
                Body = $"{employee.DisplayName} submitted an OT request."
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task<Role> EnsureRoleAsync(AppDbContext db, string name)
    {
        var existing = await db.Roles.FirstOrDefaultAsync(x => x.Name == name);
        if (existing is not null) return existing;

        var role = new Role { Id = Guid.NewGuid(), Name = name };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    private static async Task<User> EnsureUserAsync(AppDbContext db, PasswordHasher passwordHasher, string email, string displayName)
    {
        var existing = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (existing is not null)
        {
            if (existing.DisplayName != displayName) existing.DisplayName = displayName;
            if (string.IsNullOrWhiteSpace(existing.PasswordHash)) existing.PasswordHash = passwordHasher.HashPassword("Password1!");
            if (!existing.IsActive) existing.IsActive = true;
            await db.SaveChangesAsync();
            return existing;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHasher.HashPassword("Password1!"),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task EnsureUserRoleAsync(AppDbContext db, Guid userId, Guid roleId)
    {
        var exists = await db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId);
        if (exists) return;
        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        await db.SaveChangesAsync();
    }

    private static async Task<LeaveType> EnsureLeaveTypeAsync(AppDbContext db, string name, bool requiresAttachment)
    {
        var existing = await db.LeaveTypes.FirstOrDefaultAsync(x => x.Name == name);
        if (existing is not null)
        {
            if (existing.RequiresAttachment != requiresAttachment) existing.RequiresAttachment = requiresAttachment;
            await db.SaveChangesAsync();
            return existing;
        }

        var type = new LeaveType { Id = Guid.NewGuid(), Name = name, RequiresAttachment = requiresAttachment };
        db.LeaveTypes.Add(type);
        await db.SaveChangesAsync();
        return type;
    }
}
