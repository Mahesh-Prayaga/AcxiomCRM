using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null, string? ipAddress = null);
        Task<List<AuditLog>> GetAuditLogsAsync(string? userId = null, string? action = null, string? entityName = null, DateTime? fromDate = null, DateTime? toDate = null);
    }
}
