using System;
using System.Collections.Generic;
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
    [Route("api/customers")]
    [Authorize]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersApiController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? searchName, [FromQuery] string? email, [FromQuery] string? phone, [FromQuery] string? company, [FromQuery] string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var customers = await _crmService.GetCustomersAsync(user.Id, isElevated, searchName, email, phone, company, status);

            var dtos = customers.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                AssignedTo = c.AssignedTo,
                AssignedToName = c.AssignedToUser?.FullName
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var c = await _crmService.GetCustomerByIdAsync(id, user.Id, isElevated);

            if (c == null)
            {
                return NotFound(new { message = $"Customer with ID {id} not found or access denied." });
            }

            var dto = new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                AssignedTo = c.AssignedTo,
                AssignedToName = c.AssignedToUser?.FullName
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var customer = new Customer
            {
                CustomerCode = dto.CustomerCode,
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
                AssignedTo = string.IsNullOrWhiteSpace(dto.AssignedTo) ? user.Id : dto.AssignedTo
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateCustomerAsync(customer, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                return StatusCode(409, new { message = result.ErrorMessage });
            }

            dto.CustomerId = result.Customer!.CustomerId;
            dto.CustomerCode = result.Customer.CustomerCode;
            dto.CreatedDate = result.Customer.CreatedDate;

            return CreatedAtAction(nameof(GetCustomer), new { id = dto.CustomerId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            var customer = new Customer
            {
                CustomerId = id,
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status,
                AssignedTo = dto.AssignedTo
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.UpdateCustomerAsync(customer, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                if (result.ErrorMessage != null && result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = result.ErrorMessage });
                }
                if (result.ErrorMessage != null && result.ErrorMessage.Contains("not authorized", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, new { message = result.ErrorMessage });
                }
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Customer updated successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.DeleteCustomerAsync(id, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                if (result.ErrorMessage != null && result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { message = result.ErrorMessage });
                }
                if (result.ErrorMessage != null && result.ErrorMessage.Contains("not authorized", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(403, new { message = result.ErrorMessage });
                }
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { message = "Customer deactivated/deleted successfully" });
        }
    }
}
