using HrFlow.Api.Models;

namespace HrFlow.Api.Dtos;

public sealed record CreateOvertimeRequestDto(string StartAt, string EndAt, string Reason);

public sealed record OvertimeRequestDto(
    Guid Id,
    string StartAt,
    string EndAt,
    string Reason,
    OvertimeRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt
);

public sealed record InboxOvertimeRequestDto(
    Guid Id,
    string RequestorName,
    string RequestorEmail,
    string StartAt,
    string EndAt,
    OvertimeRequestStatus Status,
    int CurrentLevel
);
