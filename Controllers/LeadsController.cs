using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class LeadsController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? searchName, string? searchCompany, string? status, string? assignedTo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var leads = await _crmService.GetLeadsAsync(user.Id, isElevated, searchName, searchCompany, status, assignedTo);

            ViewData["SearchName"] = searchName;
            ViewData["SearchCompany"] = searchCompany;
            ViewData["Status"] = status;
            ViewData["AssignedTo"] = assignedTo;

            if (isElevated)
            {
                var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
                ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedTo);
            }

            return View(leads);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var lead = await _crmService.GetLeadByIdAsync(id, user.Id, isElevated);

            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateUsersDropDownList();
            return View(new Lead { Status = "New", ExpectedValue = 10000 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lead lead)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropDownList(lead.AssignedTo);
                return View(lead);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateLeadAsync(lead, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create lead.");
                await PopulateUsersDropDownList(lead.AssignedTo);
                return View(lead);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var lead = await _crmService.GetLeadByIdAsync(id, user.Id, isElevated);

            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUsersDropDownList(lead.AssignedTo);
            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lead lead)
        {
            if (id != lead.LeadId) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropDownList(lead.AssignedTo);
                return View(lead);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.UpdateLeadAsync(lead, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update lead.");
                await PopulateUsersDropDownList(lead.AssignedTo);
                return View(lead);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var lead = await _crmService.GetLeadByIdAsync(id, user.Id, isElevated);

            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(lead);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.DeleteLeadAsync(id, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Lead removed successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var lead = await _crmService.GetLeadByIdAsync(id, user.Id, isElevated);

            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "Lead is already converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.OpportunityName = $"{lead.LeadName} - Deal";
            ViewBag.OpportunityAmount = lead.ExpectedValue;
            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertLead(int id, string? opportunityName, decimal? opportunityAmount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.ConvertLeadAsync(id, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress, opportunityName, opportunityAmount);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "Lead converted successfully to Customer and Opportunity!";
            return RedirectToAction("Details", "Customers", new { id = result.Customer?.CustomerId });
        }

        private async Task PopulateUsersDropDownList(object? selectedUser = null)
        {
            var users = await _userManager.Users.AsNoTracking().Where(u => u.IsActive).ToListAsync();
            ViewBag.AssignedTo = new SelectList(users, "Id", "FullName", selectedUser);
        }
    }
}
