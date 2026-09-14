using Application.DTOs.Source;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/sources")]
public sealed class SourceController : ControllerBase
{
    private readonly ISourceService _service;
    public SourceController(ISourceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<GetSourceDTO>>> List(CancellationToken ct)
    {
        var sources = await _service.ListSourcesByUserAsync(ct);

        sources.ForEach(AddImageUrl);

        return Ok(sources);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetSourceDTO>> GetById(int id, CancellationToken ct)
    {
        var source = await _service.ListSourceByIdAsync(id, ct);

        AddImageUrl(source);

        return Ok(source);
    }

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(int id, CancellationToken ct)
    {
        var stream = await _service.GetSourceImageAsync(id, ct);

        return stream is null ? NotFound() : File(stream, "image/webp");
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateSourceDTO dto, CancellationToken ct)
    {
        var source = await _service.CreateSourceAsync(dto, ct);

        AddImageUrl(source);

        return CreatedAtAction(nameof(GetById), new { id = source.Id }, source);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateSourceDTO dto, CancellationToken ct)
    {
        dto.Id = id;

        await _service.UpdateSourceAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteSourceAsync(id, ct);

        return NoContent();
    }

    private void AddImageUrl(GetSourceDTO source)
    {
        if (!string.IsNullOrWhiteSpace(source.ImageUrl))
        {
            source.ImageUrl = Url.ActionLink(nameof(GetImage), values: new { id = source.Id });
        }
    }
}
