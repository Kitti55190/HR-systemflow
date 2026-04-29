using HrFlow.Api.Data;
using HrFlow.Api.Dtos;
using HrFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrFlow.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyCollection<NotificationDto>>> Mine()
    {
        var userId = User.GetUserId();
        var items = await _db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new NotificationDto(x.Id, x.Title, x.Body, x.IsRead, x.CreatedAt))
            .ToListAsync();

        return items;
    }

    [Authorize]
    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid id)
    {
        var userId = User.GetUserId();
        var notification = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (notification is null)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return Ok();
    }
}
