using Application.DTOs.TaskStatus;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/task-statuses")]
public sealed class TaskStatusController : ControllerBase
{
    private readonly ITasksStatusService _service;
    public TaskStatusController(ITasksStatusService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<GetTaskStatusDTO>>> List(CancellationToken ct)
    {
        var result = await _service.ListTaskStatusesByUserAsync(ct);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetTaskStatusDTO>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.ListTaskStatusByIdAsync(id, ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskStatusDTO dto, CancellationToken ct)
    {
        var taskStatus = await _service.CreateTaskStatusAsync(dto, ct);

        return CreatedAtAction(nameof(GetById), new { id = taskStatus.Id }, taskStatus);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskStatusDTO dto, CancellationToken ct)
    {
        dto.Id = id;
        await _service.UpdateTaskStatusAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteTaskStatusAsync(id, ct);

        return NoContent();
    }
}
