using Application.DTOs.Common;
using Application.DTOs.Task;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class TaskController : ControllerBase
{
    private readonly ITasksService _service;
    public TaskController(ITasksService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResultDTO<GetTaskDTO>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 8,
        [FromQuery] int? taskStatusId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var tasks = await _service.ListTasksByUserAsync(page, pageSize, taskStatusId, search, ct);

        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetTaskDTO>> GetById(int id, CancellationToken ct)
    {
        var task = await _service.ListTaskByIdAsync(id, ct);

        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskDTO dto, CancellationToken ct)
    {
        var task = await _service.CreateTaskAsync(dto, ct);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDTO dto, CancellationToken ct)
    {
        dto.Id = id;

        await _service.UpdateTaskAsync(dto, ct);

        return NoContent();
    }

    [HttpPatch("{id:int}/status/{taskStatusId:int}")]
    public async Task<IActionResult> UpdateStatus(int id, int taskStatusId, CancellationToken ct)
    {
        await _service.UpdateTaskStatusAsync(id, taskStatusId, ct);

        return NoContent();
    }

    [HttpPatch("{id:int}/sort-order/{sortOrder:int}")]
    public async Task<IActionResult> UpdateSortOrder(int id, int sortOrder, CancellationToken ct)
    {
        await _service.UpdateTaskSortOrderAsync(id, sortOrder, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        await _service.CompleteTaskAsync(id, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/reopen")]
    public async Task<IActionResult> Reopen(int id, CancellationToken ct)
    {
        await _service.ReopenTaskAsync(id, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteTaskAsync(id, ct);

        return NoContent();
    }
}
