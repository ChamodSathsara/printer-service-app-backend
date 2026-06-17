using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrinterServiceAPI.DTOs;
using PrinterServiceAPI.Services;
using System.Security.Claims;

namespace PrinterServiceAPI.Controllers;

// ════════════════════════════════════════════
// Dashboard Controller
// ════════════════════════════════════════════
[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Manager")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    // GET /api/dashboard/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await dashboardService.GetStatsAsync();
        return Ok(result);
    }
}



// ════════════════════════════════════════════
// Technician Dashboard Controller
// ════════════════════════════════════════════
[ApiController]
[Route("api/tech/dashboard")]
[Authorize(Roles = "Technician")]
public class TechnicianDashboardController(IDashboardService dashboardService) : ControllerBase
{
    // GET /api/tech/dashboard/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var techCode = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("techCode"); // adjust claim key to match your JWT

        if (string.IsNullOrEmpty(techCode))
            return Unauthorized();

        var result = await dashboardService.GetTechnicianStatsAsync(techCode);
        return Ok(result);
    }
}




// ════════════════════════════════════════════
// Technicians Controller
// ════════════════════════════════════════════
[ApiController]
[Route("api/technicians")]
[Authorize]
public class TechniciansController(ITechnicianService technicianService) : ControllerBase
{
    // GET /api/technicians   (Manager only)
    [HttpGet]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetAll()
    {
        var result = await technicianService.GetAllAsync();
        return Ok(result);
    }

    // GET /api/technicians/{techCode}
    [HttpGet("{techCode}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> GetProfile(string techCode)
    {
        var result = await technicianService.GetProfileAsync(techCode);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // POST /api/technicians   (Manager only)
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateTechnicianRequest request)
    {
        var result = await technicianService.CreateAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/technicians/{techCode}
    [HttpPut("{techCode}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Update(string techCode, [FromBody] UpdateTechnicianRequest request)
    {
        var result = await technicianService.UpdateAsync(techCode, request);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // DELETE /api/technicians/{techCode}   (soft delete)
    [HttpDelete("{techCode}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(string techCode)
    {
        var result = await technicianService.DeleteAsync(techCode);
        return result.Success ? Ok(result) : NotFound(result);
    }
}


// ════════════════════════════════════════════
// Machines Controller
// ════════════════════════════════════════════
[ApiController]
[Route("api/machines")]
[Authorize]
public class MachinesController(IMachineService machineService) : ControllerBase
{
    // GET /api/machines/{refNumber}
    [HttpGet("{refNumber}")]
    public async Task<IActionResult> GetByRef(string refNumber)
    {
        var result = await machineService.GetByRefAsync(refNumber);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // GET /api/machines/search?q=xxx
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query is required.");

        var result = await machineService.SearchAsync(q);
        return Ok(result);
    }

    // POST /api/machines   (Manager only)
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create([FromBody] CreateMachineRequest request)
    {
        var result = await machineService.CreateAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}


// ════════════════════════════════════════════
// Solution Categories Controller
// ════════════════════════════════════════════
[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(PrinterServiceAPI.Data.AppDbContext db) : ControllerBase
{
    // GET /api/categories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cats = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                db.SolutionCategories
                  .Where(c => c.IsActive)
                  .OrderBy(c => c.SortOrder)
                  .Select(c => new SolutionCategoryDto(c.CategoryId, c.CategoryName, c.SortOrder))
            );

        return Ok(new ApiResponse<IEnumerable<SolutionCategoryDto>>(true, "Success.", cats));
    }
}
