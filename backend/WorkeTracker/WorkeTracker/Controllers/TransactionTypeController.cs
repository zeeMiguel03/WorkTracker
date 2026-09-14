using Application.DTOs.Transaction;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/transaction-types")]
public sealed class TransactionTypeController : ControllerBase
{
    private readonly ITransactionTypeService _service;
    public TransactionTypeController(ITransactionTypeService service)
    {
        _service = service;
    } 

    [HttpGet]
    public async Task<ActionResult<List<GetTransactionTypeDTO>>> List(CancellationToken ct)
    {
        var result = await _service.ListTransactionTypesByUserAsync(ct);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetTransactionTypeDTO>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.ListTransactionTypeByIdAsync(id, ct);

        return Ok(result);
    } 

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionTypeDTO dto, CancellationToken ct)
    {
        var transactionType = await _service.CreateTransactionTypeAsync(dto, ct);

        return CreatedAtAction(nameof(GetById), new { id = transactionType.Id }, transactionType);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionTypeDTO dto, CancellationToken ct)
    {
        dto.idTransactionType = id;

        await _service.UpdateTransactionTypeAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteTransactionTypeAsync(id, ct);

        return NoContent();
    }
}
