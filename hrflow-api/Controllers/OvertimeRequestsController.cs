using System.Globalization;
using HrFlow.Api.Data;
using HrFlow.Api.Dtos;
using HrFlow.Api.Models;
using HrFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrFlow.Api.Controllers;

[ApiController]
[Route("api/overtime-requests")]
public sealed class OvertimeRequestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public OvertimeRequestsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyCollection<OvertimeRequestDto>>> Mine()
    {
        var userId = User.GetUserId();
        var raw = await _db.OvertimeRequests.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.StartAt,
                x.EndAt,
                x.Reason,
                x.Status,
                x.CreatedAt,
                x.SubmittedAt
            })
            .ToListAsync();

        return raw.Select(x => new OvertimeRequestDto(
            x.Id,
            x.StartAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            x.EndAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            x.Reason,
            x.Status,
            x.CreatedAt,
            x.SubmittedAt
        )).ToList();
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<OvertimeRequestDto>> Create([FromBody] CreateOvertimeRequestDto dto)
    {
        var userId = User.GetUserId();

        if (!DateTime.TryParseExact(dto.StartAt, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startLocal))
        {
            return BadRequest("StartAt must be yyyy-MM-ddTHH:mm.");
        }
        if (!DateTime.TryParseExact(dto.EndAt, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endLocal))
        {
            return BadRequest("EndAt must be yyyy-MM-ddTHH:mm.");
        }

        startLocal = DateTime.SpecifyKind(startLocal, DateTimeKind.Local);
        endLocal = DateTime.SpecifyKind(endLocal, DateTimeKind.Local);

        var startAt = new DateTimeOffset(startLocal).ToUniversalTime();
        var endAt = new DateTimeOffset(endLocal).ToUniversalTime();

        if (endAt <= startAt)
        {
            return BadRequest("EndAt must be > StartAt.");
        }

        var overlaps = await _db.OvertimeRequests.AnyAsync(x =>
            x.UserId == userId
            && x.Status != OvertimeRequestStatus.Cancelled
            && x.Status != OvertimeRequestStatus.Rejected
            && startAt < x.EndAt
            && endAt > x.StartAt
        );
        if (overlaps)
        {
            return BadRequest("Time range overlaps with an existing OT request.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new OvertimeRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StartAt = startAt,
            EndAt = endAt,
            Reason = dto.Reason?.Trim() ?? "",
            Status = OvertimeRequestStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.OvertimeRequests.Add(entity);
        await _db.SaveChangesAsync();

        return new OvertimeRequestDto(
            entity.Id,
            entity.StartAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            entity.EndAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
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
        var request = await _db.OvertimeRequests.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status is not (OvertimeRequestStatus.Draft or OvertimeRequestStatus.Returned))
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

        await _db.OvertimeApprovalSteps
            .Where(x => x.OvertimeRequestId == request.Id)
            .ExecuteDeleteAsync();

        var steps = approvers.Select((approverUserId, index) => new OvertimeApprovalStep
        {
            Id = Guid.NewGuid(),
            OvertimeRequestId = request.Id,
            Level = index + 1,
            ApproverUserId = approverUserId,
            Status = ApprovalStepStatus.Pending
        }).ToList();

        _db.OvertimeApprovalSteps.AddRange(steps);

        request.Status = OvertimeRequestStatus.Pending;
        request.SubmittedAt = DateTimeOffset.UtcNow;
        request.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = approvers[0],
            Title = "New OT request",
            Body = $"{requestor.DisplayName} submitted an OT request."
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok();
    }
}
