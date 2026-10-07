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
    public class FollowUpsController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? date, string? status, string? assignedTo, int? customerId, int? leadId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var followUps = await _crmService.GetFollowUpsAsync(user.Id, isElevated, date, status, assignedTo, customerId, leadId);

            ViewData["Date"] = date?.ToString("yyyy-MM-dd");
            ViewData["Status"] = status;

            if (isElevated)
            {
                var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
                ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedTo);
            }

            return View(followUps);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            await PopulateDropdowns(user.Id, isElevated, customerId, leadId);

            var model = new FollowUp
            {
                CustomerId = customerId,
                LeadId = leadId,
                FollowUpDate = DateTime.Today.AddDays(1),
                Status = "Planned"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUp followUp)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            // Server-side Business Validation:
            // Follow-up date cannot be earlier than today for a new/planned follow-up
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(followUp.FollowUpDate), "Follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(user.Id, isElevated, followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
                return View(followUp);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateFollowUpAsync(followUp, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to schedule follow-up.");
                await PopulateDropdowns(user.Id, isElevated, followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
                return View(followUp);
            }

            TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? remarks)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.CompleteFollowUpAsync(id, user.Id, user.FullName ?? user.UserName!, isElevated, remarks, ipAddress);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up marked as completed.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.RescheduleFollowUpAsync(id, newDate, user.Id, user.FullName ?? user.UserName!, isElevated, remarks, ipAddress);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up rescheduled successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.DeleteFollowUpAsync(id, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns(string userId, bool isElevated, int? customerId = null, int? leadId = null, string? assignedTo = null)
        {
            var customers = await _crmService.GetCustomersAsync(userId, isElevated);
            ViewBag.CustomerId = new SelectList(customers, "CustomerId", "CustomerName", customerId);

            var leads = await _crmService.GetLeadsAsync(userId, isElevated);
            ViewBag.LeadId = new SelectList(leads, "LeadId", "LeadName", leadId);

            var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
            ViewBag.AssignedTo = new SelectList(users, "Id", "FullName", assignedTo);
        }
    }
}
