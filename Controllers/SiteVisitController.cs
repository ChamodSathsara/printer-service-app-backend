using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrinterServiceAPI.DTOs;
using PrinterServiceAPI.Services;

namespace PrinterServiceAPI.Controllers;

[ApiController]
[Route("api/visits")]
[Authorize]
public class SiteVisitController(ISiteVisitService visitService, IReportService reportService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsManager =>
        User.IsInRole("Manager");

    // POST /api/visits
    [HttpPost]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> Create([FromBody] CreateSiteVisitRequest request)
    {
        var result = await visitService.CreateAsync(CurrentUserId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/visits/my
    [HttpGet("my")]
    [Authorize(Roles = "Technician")]
    public async Task<IActionResult> GetMyVisits([FromQuery] SiteVisitListRequest filter)
    {
        var result = await visitService.GetMyVisitsAsync(CurrentUserId, filter);
        return Ok(result);
    }

    // GET /api/visits          (manager: all; technician: own)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SiteVisitListRequest filter)
    {
        if (IsManager)
        {
            var result = await visitService.GetAllVisitsAsync(filter);
            return Ok(result);
        }
        else
        {
            var result = await visitService.GetMyVisitsAsync(CurrentUserId, filter);
            return Ok(result);
        }
    }

    // GET /api/visits/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await visitService.GetByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // GET /api/visits/export/excel
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] SiteVisitListRequest filter)
    {
        // Technicians can only export their own
        if (!IsManager)
            filter = filter with { TechnicianCode = User.FindFirstValue("techCode") };

        var bytes    = await reportService.ExportExcelAsync(filter);
        var fileName = $"SiteVisitReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // GET /api/visits/export/pdf
    // Temporarily in ExportPdf controller action — remove after debugging
    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] SiteVisitListRequest filter)
    {
        try
        {
            if (!IsManager)
                filter = filter with { TechnicianCode = User.FindFirstValue("techCode") };
            var bytes = await reportService.ExportPdfAsync(filter);
            var fileName = $"SiteVisitReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }
}
