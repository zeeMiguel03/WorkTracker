using API.Services;
using Application.DTOs.User;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _service;
    private readonly IRefreshTokenCookieService _cookieService;

    public UserController(IUserService service, IRefreshTokenCookieService cookieService)
    {
        _service = service;
        _cookieService = cookieService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<GetUserDTO>> GetCurrent(CancellationToken ct) {
        var user = await _service.GetUserByIdAsync(ct);

        AddImageUrl(user);

        return Ok(user);
    }

    [HttpGet("me/image")]
    public async Task<IActionResult> GetImage(CancellationToken ct)
    {
        var stream = await _service.GetProfileImageAsync(ct);

        return stream is null ? NotFound() : File(stream, "image/webp");
    }


    [HttpPut("me")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("uploads")]
    public async Task<IActionResult> Update([FromForm] UpdateUserDTO dto, CancellationToken ct)
    {
        await _service.UpdateUserAsync(dto, ct);

        return NoContent();
    }

    [HttpPatch("me/email")]
    public async Task<IActionResult> ChangeEmail([FromBody] ChangeUserEmailDTO dto, CancellationToken ct)
    {
        await _service.ChangeEmailAsync(dto, ct);

        _cookieService.Delete();

        return NoContent();
    }

    [HttpPatch("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeUserPasswordDTO dto, CancellationToken ct)
    {
        await _service.ChangePasswordAsync(dto, ct);

        _cookieService.Delete();

        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        await _service.DeleteUserAsync(ct);

        _cookieService.Delete();

        return NoContent();
    }

    private void AddImageUrl(GetUserDTO? user)
    {
        if (user is not null && !string.IsNullOrWhiteSpace(user.ProfileImageUrl))
        {
            user.ProfileImageUrl = Url.ActionLink(nameof(GetImage));
        }
    }
}
