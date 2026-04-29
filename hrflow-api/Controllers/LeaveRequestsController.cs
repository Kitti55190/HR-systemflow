using HrFlow.Api.Data;
using HrFlow.Api.Dtos;
using HrFlow.Api.Models;
using HrFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HrFlow.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
public sealed class LeaveRequestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeaveRequestsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyCollection<LeaveRequestDto>>> Mine()
    {
        var userId = User.GetUserId();
        var raw = await _db.LeaveRequests.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.LeaveTypeId,
                LeaveTypeName = x.LeaveType.Name,
                x.StartDate,
                x.EndDate,
                x.Reason,
                x.Status,
                x.CreatedAt,
                x.SubmittedAt
            })
            .ToListAsync();

        return raw.Select(x => new LeaveRequestDto(
            x.Id,
            x.LeaveTypeId,
            x.LeaveTypeName,
            x.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            x.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            x.Reason,
            x.Status,
            x.CreatedAt,
            x.SubmittedAt
        )).ToList();
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Create([FromBody] CreateLeaveRequestDto dto)
    {
        var userId = User.GetUserId();

        if (!DateOnly.TryParseExact(dto.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate))
        {
            return BadRequest("StartDate must be yyyy-MM-dd.");
        }

        if (!DateOnly.TryParseExact(dto.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            return BadRequest("EndDate must be yyyy-MM-dd.");
        }

        if (endDate < startDate)
        {
            return BadRequest("EndDate must be >= StartDate.");
        }

        var leaveType = await _db.LeaveTypes.FirstOrDefaultAsync(x => x.Id == dto.LeaveTypeId);
        if (leaveType is null)
        {
            return BadRequest("Invalid LeaveTypeId.");
        }

        var overlaps = await _db.LeaveRequests.AnyAsync(x =>
            x.UserId == userId
            && x.Status != LeaveRequestStatus.Cancelled
            && x.Status != LeaveRequestStatus.Rejected
            && startDate <= x.EndDate
            && endDate >= x.StartDate
        );
        if (overlaps)
        {
            return BadRequest("Date range overlaps with an existing request.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LeaveTypeId = leaveType.Id,
            StartDate = startDate,
            EndDate = endDate,
            Reason = dto.Reason?.Trim() ?? "",
            Status = LeaveRequestStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.LeaveRequests.Add(entity);
        await _db.SaveChangesAsync();

        return new LeaveRequestDto(
            entity.Id,
            entity.LeaveTypeId,
            leaveType.Name,
            entity.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            entity.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            entity.Reason,
            entity.Status,
            entity.CreatedAt,
            entity.SubmittedAt
        );
    }

    [Authorize]
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult> Submit(Guid id)
    {
        var userId = User.GetUserId();
        var request = await _db.LeaveRequests
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status is not (LeaveRequestStatus.Draft or LeaveRequestStatus.Returned))
        {
            return BadRequest("Request cannot be submitted in the current status.");
        }

        var requestor = await _db.Users.FirstAsync(x => x.Id == request.UserId);

        var hrIds = await _db.UserRoles
            .Join(_db.Roles.Where(r => r.Name == "HR"), ur => ur.RoleId, r => r.Id, (ur, r) => ur.UserId)
            .Distinct()
            .ToListAsync();

        if (hrIds.Count == 0)
        {
            return StatusCode(500, "No HR user configured.");
        }

        var approvers = new List<Guid>();
        if (requestor.ManagerId.HasValue)
        {
            approvers.Add(requestor.ManagerId.Value);
        }

        var hrApprover = hrIds[0];
        if (!approvers.Contains(hrApprover))
        {
            approvers.Add(hrApprover);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        await _db.ApprovalSteps
            .Where(x => x.LeaveRequestId == request.Id)
            .ExecuteDeleteAsync();

        var steps = approvers.Select((approverUserId, index) => new ApprovalStep
        {
            Id = Guid.NewGuid(),
            LeaveRequestId = request.Id,
            Level = index + 1,
            ApproverUserId = approverUserId,
            Status = ApprovalStepStatus.Pending
        }).ToList();

        _db.ApprovalSteps.AddRange(steps);

        request.Status = LeaveRequestStatus.Pending;
        request.SubmittedAt = DateTimeOffset.UtcNow;
        request.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = approvers[0],
            Title = "New leave request",
            Body = $"{requestor.DisplayName} submitted a leave request."
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok();
    }
}
