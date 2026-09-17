using Application.DTOs.Common;
using Application.DTOs.PurchaseOrder;
using Application.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/purchase-orders")]
public sealed class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrderController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDTO<ListPurchaseOrderDTO>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PurchaseOrderStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListPurchaseOrdersByUserAsync(
            page,
            pageSize,
            status,
            search,
            ct);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GetPurchaseOrderDTO>> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetPurchaseOrderByIdAsync(id, ct);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDTO dto, CancellationToken ct)
    {
        var result = await _service.CreatePurchaseOrderAsync(dto, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseOrderDTO dto, CancellationToken ct)
    {
        dto.Id = id;

        await _service.UpdatePurchaseOrderAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeletePurchaseOrderAsync(id, ct);

        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangePurchaseOrderStatusDTO dto, CancellationToken ct)
    {
        await _service.ChangePurchaseOrderStatusAsync(id, dto.Status, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/ordered")]
    public async Task<IActionResult> MarkAsOrdered(int id, [FromBody] PurchaseOrderDateDTO dto, CancellationToken ct)
    {
        await _service.MarkPurchaseOrderAsOrderedAsync(id, dto.Date, ct);

        return NoContent();
    }

    [HttpPost("{id:int}/delivered")]
    public async Task<IActionResult> MarkAsDelivered(int id, [FromBody] PurchaseOrderDateDTO dto, CancellationToken ct)
    {
        await _service.MarkPurchaseOrderAsDeliveredAsync(id, dto.Date, ct);

        return NoContent();
    }
}
