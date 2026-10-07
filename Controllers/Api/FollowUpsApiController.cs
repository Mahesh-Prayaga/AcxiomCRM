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
    [Route("api/followups")]
    [Authorize]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsApiController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowUps([FromQuery] DateTime? date, [FromQuery] string? status, [FromQuery] string? assignedTo, [FromQuery] int? customerId, [FromQuery] int? leadId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var followUps = await _crmService.GetFollowUpsAsync(user.Id, isElevated, date, status, assignedTo, customerId, leadId);

            var dtos = followUps.Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                CustomerName = f.Customer?.CustomerName,
                LeadId = f.LeadId,
                LeadName = f.Lead?.LeadName,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo,
                AssignedToName = f.AssignedToUser?.FullName
            }).ToList();

            return Ok(dtos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] FollowUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.Today)
            {
                return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
            }

            var followUp = new FollowUp
            {
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = string.IsNullOrWhiteSpace(dto.FollowUpType) ? "Call" : dto.FollowUpType,
                Subject = dto.Subject,
                Remarks = dto.Remarks,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Planned" : dto.Status,
                AssignedTo = string.IsNullOrWhiteSpace(dto.AssignedTo) ? user.Id : dto.AssignedTo
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateFollowUpAsync(followUp, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            dto.FollowUpId = result.FollowUp!.FollowUpId;

            return Created($"/api/followups/{dto.FollowUpId}", dto);
        }
    }
}
