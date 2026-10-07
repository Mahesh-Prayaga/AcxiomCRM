using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class OpportunitiesController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public OpportunitiesController(
            ICrmService crmService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _crmService = crmService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchName, int? customerId, string? stage, string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var opportunities = await _crmService.GetOpportunitiesAsync(user.Id, isElevated, searchName, customerId, stage, status);

            ViewData["SearchName"] = searchName;
            ViewData["Stage"] = stage;
            ViewData["Status"] = status;

            var customers = await _crmService.GetCustomersAsync(user.Id, isElevated);
            ViewBag.CustomerList = new SelectList(customers, "CustomerId", "CustomerName", customerId);

            return View(opportunities);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var opp = await _crmService.GetOpportunityByIdAsync(id, user.Id, isElevated);

            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(opp);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            await PopulateDropdowns(user.Id, isElevated, customerId, leadId);

            var model = new Opportunity
            {
                CustomerId = customerId ?? 0,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                Probability = 20,
                Amount = 10000,
                Stage = "Qualification"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Opportunity opportunity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            // Server-side Business Validation
            if (opportunity.Amount <= 0)
            {
                ModelState.AddModelError(nameof(opportunity.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                ModelState.AddModelError(nameof(opportunity.Probability), "Probability must be between 0 and 100.");
            }

            var isActive = opportunity.Stage != "Won" && opportunity.Stage != "Lost";
            if (isActive && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(user.Id, isElevated, opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
                return View(opportunity);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateOpportunityAsync(opportunity, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create opportunity.");
                await PopulateDropdowns(user.Id, isElevated, opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
                return View(opportunity);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var opp = await _crmService.GetOpportunityByIdAsync(id, user.Id, isElevated);

            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(user.Id, isElevated, opp.CustomerId, opp.LeadId, opp.AssignedTo);
            return View(opp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Opportunity opportunity)
        {
            if (id != opportunity.OpportunityId) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            // Server-side Business Validation
            if (opportunity.Amount <= 0)
            {
                ModelState.AddModelError(nameof(opportunity.Amount), "Opportunity Amount must be greater than 0.");
            }

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
            {
                ModelState.AddModelError(nameof(opportunity.Probability), "Probability must be between 0 and 100.");
            }

            var isActive = opportunity.Stage != "Won" && opportunity.Stage != "Lost";
            if (isActive && opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(user.Id, isElevated, opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
                return View(opportunity);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.UpdateOpportunityAsync(opportunity, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update opportunity.");
                await PopulateDropdowns(user.Id, isElevated, opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
                return View(opportunity);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var opp = await _crmService.GetOpportunityByIdAsync(id, user.Id, isElevated);

            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(opp);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.DeleteOpportunityAsync(id, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Opportunity removed successfully.";
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
