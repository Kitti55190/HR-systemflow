using HrFlow.Api.Data;
using HrFlow.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HrFlow.Api.Controllers;

[ApiController]
[Route("api/leave-types")]
public sealed class LeaveTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public LeaveTypesController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<LeaveTypeDto>>> Get()
    {
        var items = await _db.LeaveTypes.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LeaveTypeDto(x.Id, x.Name, x.RequiresAttachment))
            .ToListAsync();

        return items;
    }
}
