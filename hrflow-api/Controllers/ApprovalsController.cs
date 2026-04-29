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
[Route("api/approvals")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ApprovalsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("inbox")]
    public async Task<ActionResult<IReadOnlyCollection<InboxLeaveRequestDto>>> Inbox()
    {
        var approverId = User.GetUserId();

        var raw = await _db.LeaveRequests.AsNoTracking()
            .Where(r => r.Status == LeaveRequestStatus.Pending)
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
                LeaveTypeName = x.Request.LeaveType.Name,
                x.Request.StartDate,
                x.Request.EndDate,
                x.Request.Status,
                x.CurrentLevel
            })
            .ToListAsync();

        return raw.Select(x => new InboxLeaveRequestDto(
            x.Id,
            x.RequestorName,
            x.RequestorEmail,
            x.LeaveTypeName,
            x.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            x.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            x.Status,
            x.CurrentLevel
        )).ToList();
    }

    [Authorize]
    [HttpPost("leave-requests/{id:guid}/action")]
    public async Task<ActionResult> Action(Guid id, [FromBody] ApprovalActionDto dto)
    {
        var approverId = User.GetUserId();
        var action = dto.Action.Trim().ToLowerInvariant();
        if (action is not ("approve" or "reject" or "return"))
        {
            return BadRequest("Action must be approve, reject, or return.");
        }

        var request = await _db.LeaveRequests
            .Include(x => x.User)
            .Include(x => x.LeaveType)
            .Include(x => x.ApprovalSteps)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != LeaveRequestStatus.Pending)
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
                request.Status = LeaveRequestStatus.Approved;
                _db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    Title = "Leave request approved",
                    Body = $"Your {request.LeaveType.Name} request was approved."
                });
            }
            else
            {
                _db.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = nextStep.ApproverUserId,
                    Title = "Leave request pending your approval",
                    Body = $"{request.User.DisplayName} submitted a leave request."
                });
            }
        }
        else if (action == "reject")
        {
            step.Status = ApprovalStepStatus.Rejected;
            request.Status = LeaveRequestStatus.Rejected;

            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = "Leave request rejected",
                Body = $"Your {request.LeaveType.Name} request was rejected."
            });
        }
        else
        {
            step.Status = ApprovalStepStatus.Returned;
            request.Status = LeaveRequestStatus.Returned;

            _db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Title = "Leave request returned",
                Body = $"Your {request.LeaveType.Name} request was returned for changes."
            });
        }

        request.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }
}
