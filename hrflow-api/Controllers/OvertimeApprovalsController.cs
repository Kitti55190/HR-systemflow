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
[Route("api/overtime-approvals")]
public sealed class OvertimeApprovalsController : ControllerBase
{
    private readonly AppDbContext _db;

    public OvertimeApprovalsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("inbox")]
    public async Task<ActionResult<IReadOnlyCollection<InboxOvertimeRequestDto>>> Inbox()
    {
        var approverId = User.GetUserId();

        var raw = await _db.OvertimeRequests.AsNoTracking()
            .Where(r => r.Status == OvertimeRequestStatus.Pending)
            .Where(r => r.ApprovalSteps.Any(s => s.ApproverUserId == approverId && s.Status == ApprovalStepStatus.Pending))
            .Select(r => new
            {
                Request = r,
                CurrentLevel = r.ApprovalSteps.Where(s => s.Status == ApprovalStepStatus.Pending).Min(s => s.Level),
                ApproverStepLevel = r.ApprovalSteps.Where(s => s.ApproverUserId == approverId && s.Status == ApprovalStepStatus.Pending).Select(s => s.Level).FirstOrDefault()
            })
            .Where(x => x.ApproverStepLevel == x.CurrentLevel)
            .OrderBy(x => x.Request.SubmittedAt)
            .Select(x => new
            {
                x.Request.Id,
                RequestorName = x.Request.User.DisplayName,
                RequestorEmail = x.Request.User.Email,
                x.Request.StartAt,
                x.Request.EndAt,
                x.Request.Status,
                x.CurrentLevel
            })
            .ToListAsync();

        return raw.Select(x => new InboxOvertimeRequestDto(
            x.Id,
            x.RequestorName,
            x.RequestorEmail,
            x.StartAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            x.EndAt.ToLocalTime().ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            x.Status,
            x.CurrentLevel
        )).ToList();
    }

    [Authorize]
    [HttpPost("overtime-requests/{id:guid}/action")]
    public async Task<ActionResult> Action(Guid id, [FromBody] ApprovalActionDto dto)
    {
        var approverId = User.GetUserId();
        var action = dto.Action.Trim().ToLowerInvariant();
        if (action is not ("approve" or "reject" or "return"))
        {
            return BadRequest("Action must be approve, reject, or return.");
        }

        var request = await _db.OvertimeRequests
            .Include(x => x.User)
            .Include(x => x.ApprovalSteps)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != OvertimeRequestStatus.Pending)
        {
            return BadRequest("Request is not pending.");
        }

        var currentLevel = request.ApprovalSteps.Where(s => s.Status == ApprovalStepStatus.Pending).Min(s => s.Level);
        var step = request.ApprovalSteps.FirstOrDefault(s => s.ApproverUserId == approverId && s.Level == currentLevel && s.Status == ApprovalStepStatus.Pending);
        if (step is null)
        {
            return Forbid();
        }

        step.ActionAt = DateTimeOffset.UtcNow;
        step.Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim();

        if (action == "approve")
        {
            step.Status = ApprovalStepStatus.Approved;

            var nextStep = request.ApprovalSteps
                .Where(s => s.Status == ApprovalStepStatus.Pending)
                .OrderBy(s => s.Level)
                .FirstOrDefault();

            if (nextStep is null)
            {
                request.Status = OvertimeRequestStatus.Approved;
                _db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    Title = "OT request approved",
                    Body = "Your OT request was approved."
                });
            }
            else
            {
                _db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = nextStep.ApproverUserId,
                    Title = "OT request pending your approval",
                    Body = $"{request.User.DisplayName} submitted an OT request."
                });
            }
        }
        else if (action == "reject")
        {
            step.Status = ApprovalStepStatus.Rejected;
            request.Status = OvertimeRequestStatus.Rejected;

            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = "OT request rejected",
                Body = "Your OT request was rejected."
            });
        }
        else
        {
            step.Status = ApprovalStepStatus.Returned;
            request.Status = OvertimeRequestStatus.Returned;

            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = "OT request returned",
                Body = "Your OT request was returned for changes."
            });
        }

        request.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
