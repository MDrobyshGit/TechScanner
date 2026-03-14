using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using TechScanner.Api.DTOs;
using TechScanner.Core.Entities;
using TechScanner.Core.Interfaces;
using TechScanner.Scanner.Background;

namespace TechScanner.Api.Controllers;

[ApiController]
[Route("api/scans")]
public class ScanController : ControllerBase
{
    private readonly IScanRepository _repository;
    private readonly Channel<ScanJob> _channel;
    private readonly ScanBackgroundService _backgroundService;

    public ScanController(
        IScanRepository repository,
        Channel<ScanJob> channel,
        ScanBackgroundService backgroundService)
    {
        _repository = repository;
        _channel = channel;
        _backgroundService = backgroundService;
    }

    /// <summary>POST /api/scans — Start a new scan</summary>
    [HttpPost]
    public async Task<IActionResult> StartScanAsync([FromBody] StartScanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceInput))
            return BadRequest("SourceInput is required.");

        var scan = new Scan
        {
            SourceType = request.SourceType,
            SourceInput = request.SourceInput
        };

        await _repository.CreateAsync(scan);

        var job = new ScanJob(scan.Id, request.SourceType, request.SourceInput, request.GitToken);
        await _channel.Writer.WriteAsync(job);

        return Accepted(new { scanId = scan.Id });
    }

    /// <summary>GET /api/scans/{id} — Get scan result</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetScanAsync(Guid id)
    {
        var scan = await _repository.GetByIdAsync(id);
        if (scan == null) return NotFound();
        return Ok(ScanResultDto.FromEntity(scan));
    }

    /// <summary>GET /api/scans — Get recent scans history</summary>
    [HttpGet]
    public async Task<IActionResult> GetHistoryAsync([FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 200) limit = 20;
        var scans = await _repository.GetRecentAsync(limit);
        return Ok(scans.Select(ScanSummaryDto.FromEntity));
    }

    /// <summary>DELETE /api/scans/{id} — Delete a scan</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteScanAsync(Guid id)
    {
        var scan = await _repository.GetByIdAsync(id);
        if (scan == null) return NotFound();
        await _repository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>GET /api/scans/{id}/progress — SSE progress stream</summary>
    [HttpGet("{id:guid}/progress")]
    public async Task GetProgressAsync(Guid id, CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        while (!ct.IsCancellationRequested)
        {
            _backgroundService.ProgressMap.TryGetValue(id, out var progress);

            if (progress != null)
            {
                var data = JsonSerializer.Serialize(new { percent = progress.Percent, message = progress.Message });
                var message = $"data: {data}\n\n";
                await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(message), ct);
                await Response.Body.FlushAsync(ct);

                if (progress.Percent >= 100) break;
            }
            else
            {
                // Scan may already be done — check DB
                var scan = await _repository.GetByIdAsync(id);
                if (scan == null) break;
                if (scan.Status == TechScanner.Core.Enums.ScanStatus.Completed ||
                    scan.Status == TechScanner.Core.Enums.ScanStatus.Failed)
                {
                    var data = JsonSerializer.Serialize(new
                    {
                        percent = 100,
                        message = scan.Status == TechScanner.Core.Enums.ScanStatus.Completed
                            ? "Scan complete." : $"Error: {scan.ErrorMessage}"
                    });
                    await Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"data: {data}\n\n"), ct);
                    await Response.Body.FlushAsync(ct);
                    break;
                }
            }

            await Task.Delay(500, ct);
        }
    }
}
