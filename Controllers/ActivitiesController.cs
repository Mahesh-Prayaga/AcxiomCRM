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
    public class ActivitiesController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? activityType, DateTime? date, string? status, string? assignedTo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var activities = await _crmService.GetActivitiesAsync(user.Id, isElevated, activityType, date, status, assignedTo);

            ViewData["ActivityType"] = activityType;
            ViewData["Status"] = status;
            ViewData["Date"] = date?.ToString("yyyy-MM-dd");

            if (isElevated)
            {
                var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
                ViewBag.UsersList = new SelectList(users, "Id", "FullName", assignedTo);
            }

            return View(activities);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            await PopulateDropdowns(user.Id, isElevated, customerId, leadId);

            var model = new Activity
            {
                CustomerId = customerId,
                LeadId = leadId,
                ActivityDate = DateTime.UtcNow,
                Status = "Completed"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(user.Id, isElevated, activity.CustomerId, activity.LeadId, activity.AssignedTo);
                return View(activity);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateActivityAsync(activity, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to log activity.");
                await PopulateDropdowns(user.Id, isElevated, activity.CustomerId, activity.LeadId, activity.AssignedTo);
                return View(activity);
            }

            TempData["SuccessMessage"] = "Activity recorded successfully.";
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
