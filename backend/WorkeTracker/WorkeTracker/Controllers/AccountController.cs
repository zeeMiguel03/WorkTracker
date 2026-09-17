using Application.DTOs.Account;
using Application.DTOs.Common;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountService _service;
    public AccountController(IAccountService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResultDTO<ListAccountDTO>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 8,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var accounts = await _service.ListAccountsByUserAsync(page, pageSize, search, ct);

        return Ok(accounts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ListAccountDTO>> GetById(int id, CancellationToken ct)
    {
        var account = await _service.ListAccountByIdAsync(id, ct);

        return Ok(account);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountDTO dto, CancellationToken ct)
    {
        var account = await _service.CreateAccountAsync(dto, ct);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountDTO dto, CancellationToken ct)
    {
        dto.IdAccount = id;

        await _service.UpdateAccountAsync(dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAccountAsync(id, ct);

        return NoContent();
    }
}
