using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(IReportService reportService, UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Customers(string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var report = await _reportService.GetCustomerReportAsync(user.Id, isElevated, status);

            ViewData["Status"] = status;
            return View(report);
        }

        public async Task<IActionResult> Leads(string? source, string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var report = await _reportService.GetLeadReportAsync(user.Id, isElevated, source, status);

            ViewData["Source"] = source;
            ViewData["Status"] = status;
            return View(report);
        }

        public async Task<IActionResult> FollowUps(string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var report = await _reportService.GetFollowUpReportAsync(user.Id, isElevated, status);

            ViewData["Status"] = status;
            return View(report);
        }

        public async Task<IActionResult> Opportunities(string? stage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var report = await _reportService.GetOpportunityReportAsync(user.Id, isElevated, stage);

            ViewData["Stage"] = stage;
            return View(report);
        }

        public async Task<IActionResult> Pipeline()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var report = await _reportService.GetPipelineReportAsync(user.Id, isElevated);

            return View(report);
        }

        public async Task<IActionResult> Conversion()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var report = await _reportService.GetConversionReportAsync(user.Id, isElevated);

            return View(report);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UserActivities()
        {
            var report = await _reportService.GetUserActivityReportAsync();
            return View(report);
        }
    }
}
