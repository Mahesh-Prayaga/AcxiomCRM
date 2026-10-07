using System;
using System.Security.Claims;
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
    public class CustomersController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? searchName, string? searchEmail, string? searchPhone, string? searchCompany, string? status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var customers = await _crmService.GetCustomersAsync(user.Id, isElevated, searchName, searchEmail, searchPhone, searchCompany, status);

            ViewData["SearchName"] = searchName;
            ViewData["SearchEmail"] = searchEmail;
            ViewData["SearchPhone"] = searchPhone;
            ViewData["SearchCompany"] = searchCompany;
            ViewData["Status"] = status;

            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var customer = await _crmService.GetCustomerByIdAsync(id, user.Id, isElevated);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or you are not authorized to view this record.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateUsersDropDownList();
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Server-side validation
            if (!ModelState.IsValid)
            {
                await PopulateUsersDropDownList(customer.AssignedTo);
                return View(customer);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.CreateCustomerAsync(customer, user.Id, user.FullName ?? user.UserName!, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to save customer.");
                await PopulateUsersDropDownList(customer.AssignedTo);
                return View(customer);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var customer = await _crmService.GetCustomerByIdAsync(id, user.Id, isElevated);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateUsersDropDownList(customer.AssignedTo);
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return BadRequest();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropDownList(customer.AssignedTo);
                return View(customer);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _crmService.UpdateCustomerAsync(customer, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update customer.");
                await PopulateUsersDropDownList(customer.AssignedTo);
                return View(customer);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var customer = await _crmService.GetCustomerByIdAsync(id, user.Id, isElevated);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var isElevated = User.IsInRole("Admin") || User.IsInRole("Manager");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _crmService.DeleteCustomerAsync(id, user.Id, user.FullName ?? user.UserName!, isElevated, ipAddress);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Customer removed / deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropDownList(object? selectedUser = null)
        {
            var users = await _userManager.Users.AsNoTracking().Where(u => u.IsActive).ToListAsync();
            ViewBag.AssignedTo = new SelectList(users, "Id", "FullName", selectedUser);
        }
    }
}
