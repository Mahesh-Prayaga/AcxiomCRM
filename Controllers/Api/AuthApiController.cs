using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuditService _auditService;

        public AuthApiController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auditService = auditService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input payload", errors = ModelState });
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (user == null || !user.IsActive)
            {
                await _auditService.LogAsync(null, dto.Email, "API Failed Login", "Security", null, null, "User not found or inactive", ipAddress);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _auditService.LogAsync(user.Id, user.Email, "API Failed Login", "Security", null, null, "User locked out", ipAddress);
                return StatusCode(403, new { message = "Account is locked out due to multiple failed login attempts" });
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, dto.Password, isPersistent: true, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _auditService.LogAsync(user.Id, user.Email, "API Login", "Security", null, null, "Successful API authentication", ipAddress);

                return Ok(new
                {
                    message = "Login successful",
                    userId = user.Id,
                    email = user.Email,
                    fullName = user.FullName,
                    roles = roles
                });
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(user.Id, user.Email, "API Lockout", "Security", null, null, "User locked out", ipAddress);
                return StatusCode(403, new { message = "Account locked out due to repeated failed attempts" });
            }

            await _auditService.LogAsync(user.Id, user.Email, "API Failed Login", "Security", null, null, "Invalid password attempt", ipAddress);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _signInManager.SignOutAsync();

            if (user != null)
            {
                await _auditService.LogAsync(user.Id, user.Email, "API Logout", "Security", null, null, "API session terminated", ipAddress);
            }

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
