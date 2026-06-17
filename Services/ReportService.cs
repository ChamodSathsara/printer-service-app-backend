using ClosedXML.Excel;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.EntityFrameworkCore;
using PrinterServiceAPI.Data;
using PrinterServiceAPI.DTOs;

namespace PrinterServiceAPI.Services;

public interface IReportService
{
    Task<byte[]> ExportExcelAsync(SiteVisitListRequest filter);
    Task<byte[]> ExportPdfAsync(SiteVisitListRequest filter);
}

public class ReportService(AppDbContext db) : IReportService
{
    // ── Shared Data Query ────────────────────────────────────────
    private async Task<List<ReportRow>> GetRowsAsync(SiteVisitListRequest f)
    {
        var query = db.SiteVisits.Include(v => v.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.TechnicianCode))
            query = query.Where(v => v.TechnicianCode == f.TechnicianCode);

        if (f.FromDate.HasValue)   query = query.Where(v => v.VisitDate >= f.FromDate.Value);
        if (f.ToDate.HasValue)     query = query.Where(v => v.VisitDate <= f.ToDate.Value);
        if (f.CategoryId.HasValue) query = query.Where(v => v.CategoryId == f.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLower();
            query = query.Where(v =>
                v.MachineRefNumber.ToLower().Contains(s) ||
                v.TechnicianName.ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.VisitTime)
            .Select(v => new ReportRow(
                v.VisitId,
                v.VisitDate.ToString("yyyy-MM-dd"),
                v.VisitTime.ToString("HH:mm"),
                v.TechnicianCode,
                v.TechnicianName,
                v.MachineRefNumber,
                v.Category.CategoryName,
                v.Note ?? "",
                v.MeterReadingValue.HasValue ? v.MeterReadingValue.Value.ToString("F2") : "",
                v.Latitude.HasValue  ? v.Latitude.Value.ToString()  : "",
                v.Longitude.HasValue ? v.Longitude.Value.ToString() : "",
                v.LocationAddress ?? ""
            ))
            .ToListAsync();
    }

    // ── Excel Export ─────────────────────────────────────────────
    public async Task<byte[]> ExportExcelAsync(SiteVisitListRequest filter)
    {
        var rows = await GetRowsAsync(filter);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Site Visits");

        var headers = new[]
        {
            "Visit ID", "Date", "Time", "Tech Code", "Technician",
            "Machine Ref#", "Category", "Notes", "Meter Reading",
            "Latitude", "Longitude", "Location"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int r = 0; r < rows.Count; r++)
        {
            var row   = rows[r];
            var exRow = r + 2;

            ws.Cell(exRow, 1).Value  = row.VisitId;
            ws.Cell(exRow, 2).Value  = row.VisitDate;
            ws.Cell(exRow, 3).Value  = row.VisitTime;
            ws.Cell(exRow, 4).Value  = row.TechnicianCode;
            ws.Cell(exRow, 5).Value  = row.TechnicianName;
            ws.Cell(exRow, 6).Value  = row.MachineRefNumber;
            ws.Cell(exRow, 7).Value  = row.CategoryName;
            ws.Cell(exRow, 8).Value  = row.Note;
            ws.Cell(exRow, 9).Value  = row.MeterReading;
            ws.Cell(exRow, 10).Value = row.Latitude;
            ws.Cell(exRow, 11).Value = row.Longitude;
            ws.Cell(exRow, 12).Value = row.LocationAddress;

            if (r % 2 == 1)
                ws.Row(exRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── PDF Export ───────────────────────────────────────────────
    public async Task<byte[]> ExportPdfAsync(SiteVisitListRequest filter)
    {
        var rows = await GetRowsAsync(filter);

        using var ms = new MemoryStream();

        // Don't use `using` on writer/pdf/doc — close doc manually first,
        // THEN read the stream. Otherwise stream is disposed before ToArray().
        var writer = new PdfWriter(ms);
        var pdf = new PdfDocument(writer);
        var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

        doc.SetMargins(20, 20, 20, 20);

        // Title
        doc.Add(new Paragraph("Site Visit Report")
            .SetFontSize(16)
            .SetBold()
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(4));

        var subtitle = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        if (filter.FromDate.HasValue || filter.ToDate.HasValue)
            subtitle += $"  |  Period: {filter.FromDate?.ToString("yyyy-MM-dd") ?? "—"} to {filter.ToDate?.ToString("yyyy-MM-dd") ?? "—"}";

        doc.Add(new Paragraph(subtitle)
            .SetFontSize(9)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(12));

        // Table
        float[] widths = [2f, 2.5f, 2f, 3f, 3f, 2f, 3.5f, 4f, 2f, 2f, 2f, 3f];
        var table = new Table(UnitValue.CreatePercentArray(widths)).UseAllAvailableWidth();

        string[] headers =
        [
            "ID", "Date", "Time", "Tech Code", "Technician",
        "Machine Ref#", "Category", "Notes", "Meter", "Lat", "Lng", "Location"
        ];

        var headerBg = new DeviceRgb(30, 64, 175);

        foreach (var h in headers)
        {
            table.AddHeaderCell(new Cell()
                .Add(new Paragraph(h).SetBold().SetFontSize(7).SetFontColor(ColorConstants.WHITE))
                .SetBackgroundColor(headerBg)
                .SetPadding(4));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var bg = i % 2 == 1 ? new DeviceRgb(241, 245, 249) : null;

            void AddCell(string val)
            {
                var c = new Cell().Add(new Paragraph(val).SetFontSize(7)).SetPadding(3);
                if (bg is not null) c.SetBackgroundColor(bg);
                table.AddCell(c);
            }

            AddCell(row.VisitId.ToString());
            AddCell(row.VisitDate);
            AddCell(row.VisitTime);
            AddCell(row.TechnicianCode);
            AddCell(row.TechnicianName);
            AddCell(row.MachineRefNumber);
            AddCell(row.CategoryName);
            AddCell(row.Note.Length > 60 ? row.Note[..60] + "…" : row.Note);
            AddCell(row.MeterReading);
            AddCell(row.Latitude);
            AddCell(row.Longitude);
            AddCell(row.LocationAddress.Length > 40 ? row.LocationAddress[..40] + "…" : row.LocationAddress);
        }

        doc.Add(table);
        doc.Add(new Paragraph($"Total Records: {rows.Count}")
            .SetFontSize(8).SetBold().SetMarginTop(8));

        // Close doc FIRST — this flushes all PDF bytes into the MemoryStream.
        // Only AFTER this is ms safe to read.
        doc.Close();

        return ms.ToArray();
    }

    private record ReportRow(
        int    VisitId,
        string VisitDate,
        string VisitTime,
        string TechnicianCode,
        string TechnicianName,
        string MachineRefNumber,
        string CategoryName,
        string Note,
        string MeterReading,
        string Latitude,
        string Longitude,
        string LocationAddress
    );
}
