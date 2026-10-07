using System;
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
    [Route("api/opportunities")]
    [Authorize]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesApiController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? searchName, [FromQuery] int? customerId, [FromQuery] string? stage, [FromQuery] string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var opps = await _crmService.GetOpportunitiesAsync(user.Id, isElevated, searchName, customerId, stage, status);

            var dtos = opps.Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                Notes = o.Notes,
                CreatedDate = o.CreatedDate,
                AssignedTo = o.AssignedTo,
                AssignedToName = o.AssignedToUser?.FullName
            }).ToList();

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] OpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Business validations
            if (dto.Amount <= 0)
            {
                return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(new { message = "Probability must be between 0 and 100." });
            }

            var isActive = dto.Stage != "Won" && dto.Stage != "Lost";
            if (isActive && dto.ExpectedCloseDate.Date < DateTime.Today)
            {
                return BadRequest(new { message = "Expected Close Date cannot be in the past for an active opportunity." });
            }

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = string.IsNullOrWhiteSpace(dto.Stage) ? "Qualification" : dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Open" : dto.Status,
                Notes = dto.Notes,
                AssignedTo = string.IsNullOrWhiteSpace(dto.AssignedTo) ? user.Id : dto.AssignedTo
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateOpportunityAsync(opp, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            dto.OpportunityId = result.Opportunity!.OpportunityId;
            dto.CreatedDate = result.Opportunity.CreatedDate;

            return Created($"/api/opportunities/{dto.OpportunityId}", dto);
        }
    }
}
