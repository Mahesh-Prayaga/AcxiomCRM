using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/leads")]
    [Authorize]
    public class LeadsApiController : ControllerBase
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsApiController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] string? searchName, [FromQuery] string? company, [FromQuery] string? status, [FromQuery] string? assignedTo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var leads = await _crmService.GetLeadsAsync(user.Id, isElevated, searchName, company, status, assignedTo);

            var dtos = leads.Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedTo = l.AssignedTo,
                AssignedToName = l.AssignedToUser?.FullName
            }).ToList();

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] LeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lead = new Lead
            {
                LeadCode = dto.LeadCode,
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = string.IsNullOrWhiteSpace(dto.Source) ? "Website" : dto.Source,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "New" : dto.Status,
                Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Medium" : dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                AssignedTo = string.IsNullOrWhiteSpace(dto.AssignedTo) ? user.Id : dto.AssignedTo
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateLeadAsync(lead, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            dto.LeadId = result.Lead!.LeadId;
            dto.LeadCode = result.Lead.LeadCode;
            dto.CreatedDate = result.Lead.CreatedDate;

            return Created($"/api/leads/{dto.LeadId}", dto);
        }
    }
}
