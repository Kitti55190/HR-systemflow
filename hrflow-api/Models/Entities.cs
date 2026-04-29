namespace HrFlow.Api.Models;

public enum LeaveRequestStatus
{
    Draft = 0,
    Submitted = 1,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Returned = 5,
    Cancelled = 6
}

public enum ApprovalStepStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Returned = 3
}

public enum OvertimeRequestStatus
{
    Draft = 0,
    Pending = 2,
    Approved = 3,
    Rejected = 4,
    Returned = 5,
    Cancelled = 6
}

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public sealed class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public sealed class LeaveType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public bool RequiresAttachment { get; set; }
}

public sealed class LeaveRequest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = "";

    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }

    public ICollection<ApprovalStep> ApprovalSteps { get; set; } = new List<ApprovalStep>();
}

public sealed class ApprovalStep
{
    public Guid Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest LeaveRequest { get; set; } = null!;

    public int Level { get; set; }
    public Guid ApproverUserId { get; set; }
    public User ApproverUser { get; set; } = null!;

    public ApprovalStepStatus Status { get; set; } = ApprovalStepStatus.Pending;
    public DateTimeOffset? ActionAt { get; set; }
    public string? Comment { get; set; }
}

public sealed class OvertimeRequest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string Reason { get; set; } = "";

    public OvertimeRequestStatus Status { get; set; } = OvertimeRequestStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }

    public ICollection<OvertimeApprovalStep> ApprovalSteps { get; set; } = new List<OvertimeApprovalStep>();
}

public sealed class OvertimeApprovalStep
{
    public Guid Id { get; set; }
    public Guid OvertimeRequestId { get; set; }
    public OvertimeRequest OvertimeRequest { get; set; } = null!;

    public int Level { get; set; }
    public Guid ApproverUserId { get; set; }
    public User ApproverUser { get; set; } = null!;

    public ApprovalStepStatus Status { get; set; } = ApprovalStepStatus.Pending;
    public DateTimeOffset? ActionAt { get; set; }
    public string? Comment { get; set; }
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
