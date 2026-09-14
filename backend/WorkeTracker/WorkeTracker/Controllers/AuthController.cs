using API.Services;
using Application.DTOs.Auth;
using Application.DTOs.User;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRefreshTokenCookieService _cookieService;

        public AuthController(IAuthService authService, IRefreshTokenCookieService cookieService)
        {
            _authService = authService;
            _cookieService = cookieService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<AuthenticatedUserDTO>> Register([FromForm] CreateUserDTO dto, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(dto, cancellationToken);

            SetRefreshToken(result);
            AddProfileImageUrl(result);

            return CreatedAtAction(
                nameof(UserController.GetCurrent),
                "User",
                routeValues: null,
                value: result);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthenticatedUserDTO>> Login(LoginDTO dto, CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(dto, cancellationToken);

            SetRefreshToken(result);
            AddProfileImageUrl(result);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthenticatedUserDTO>> Refresh(CancellationToken cancellationToken)
        {
            var result = await _authService.RefreshAsync(_cookieService.Get() ?? string.Empty, cancellationToken);

            SetRefreshToken(result);
            AddProfileImageUrl(result);

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(_cookieService.Get(), cancellationToken);

            _cookieService.Delete();

            return NoContent();
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
        {
            await _authService.LogoutAllAsync(cancellationToken);

            _cookieService.Delete();

            return NoContent();
        }

        private void SetRefreshToken(AuthenticatedUserDTO result)
        {
            _cookieService.Set(result.RefreshToken, result.RefreshTokenExpiresAt);
        }

        private void AddProfileImageUrl(AuthenticatedUserDTO result)
        {
            if (!string.IsNullOrWhiteSpace(result.User.ProfileImageUrl))
            {
                result.User.ProfileImageUrl = Url.ActionLink(nameof(UserController.GetImage), "User");
            }
        }
    }
}
