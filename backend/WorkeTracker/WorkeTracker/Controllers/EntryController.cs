using Application.DTOs.Entry;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/entries")]
public sealed class EntryController : ControllerBase
{
    private readonly IEntryService _service;
    public EntryController(IEntryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<GetEntryDTO>>> List(CancellationToken ct)
    {
        var entries = await _service.ListEntriesByUserAsync(ct);

        entries.ForEach(AddImageUrl);

        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetEntryDTO>> GetById(int id, CancellationToken ct)
    {
        var entry = await _service.ListEntryByIdAsync(id, ct);

        AddImageUrl(entry);

        return Ok(entry);
    }

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(int id, CancellationToken ct)
    {
        var stream = await _service.GetEntryImageAsync(id, ct);

        return stream is null ? NotFound() : File(stream, "image/webp");
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateEntryDTO dto, CancellationToken ct)
    {
        var entry = await _service.CreateEntryAsync(dto, ct);

        AddImageUrl(entry);

        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateEntryDTO dto, CancellationToken ct)
    {
        dto.Id = id;

        await _service.UpdateEntryAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteEntryAsync(id, ct);

        return NoContent();
    }

    private void AddImageUrl(GetEntryDTO entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.ImageUrl))
        {
            entry.ImageUrl = Url.ActionLink(nameof(GetImage), values: new { id = entry.Id });
        }
    }
}
