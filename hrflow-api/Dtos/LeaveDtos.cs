using HrFlow.Api.Models;

namespace HrFlow.Api.Dtos;

public sealed record LeaveTypeDto(Guid Id, string Name, bool RequiresAttachment);

public sealed record CreateLeaveRequestDto(Guid LeaveTypeId, string StartDate, string EndDate, string Reason);

public sealed record LeaveRequestDto(
    Guid Id,
    Guid LeaveTypeId,
    string LeaveTypeName,
    string StartDate,
    string EndDate,
    string Reason,
    LeaveRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt
);

public sealed record InboxLeaveRequestDto(
    Guid Id,
    string RequestorName,
    string RequestorEmail,
    string LeaveTypeName,
    string StartDate,
    string EndDate,
    LeaveRequestStatus Status,
    int CurrentLevel
);

public sealed record ApprovalActionDto(string Action, string? Comment);
