using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogsController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? userId, string? action, string? entityName, DateTime? fromDate, DateTime? toDate)
        {
            var logs = await _auditService.GetAuditLogsAsync(userId, action, entityName, fromDate, toDate);

            ViewData["UserId"] = userId;
            ViewData["Action"] = action;
            ViewData["EntityName"] = entityName;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

            return View(logs);
        }
    }
}
