using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<CustomerReportItem>> GetCustomerReportAsync(string? userId, bool isElevated, string? status = null)
        {
            var query = _context.Customers
                .Include(c => c.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            return await query
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new CustomerReportItem
                {
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    Status = c.Status,
                    OwnerName = c.AssignedToUser != null ? c.AssignedToUser.FullName : "Unassigned",
                    CreatedDate = c.CreatedDate
                })
                .ToListAsync();
        }

        public async Task<List<LeadReportItem>> GetLeadReportAsync(string? userId, bool isElevated, string? source = null, string? status = null)
        {
            var query = _context.Leads
                .Include(l => l.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(l => l.AssignedTo == userId);
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                query = query.Where(l => l.Source == source);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            return await query
                .OrderByDescending(l => l.CreatedDate)
                .Select(l => new LeadReportItem
                {
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    ExpectedValue = l.ExpectedValue,
                    OwnerName = l.AssignedToUser != null ? l.AssignedToUser.FullName : "Unassigned",
                    CreatedDate = l.CreatedDate
                })
                .ToListAsync();
        }

        public async Task<List<FollowUpReportItem>> GetFollowUpReportAsync(string? userId, bool isElevated, string? status = null)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(f => f.AssignedTo == userId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            return await query
                .OrderBy(f => f.FollowUpDate)
                .Select(f => new FollowUpReportItem
                {
                    FollowUpId = f.FollowUpId,
                    Subject = f.Subject,
                    FollowUpType = f.FollowUpType,
                    FollowUpDate = f.FollowUpDate,
                    Status = f.Status,
                    RelatedTo = f.Customer != null ? $"Customer: {f.Customer.CustomerName}" : (f.Lead != null ? $"Lead: {f.Lead.LeadName}" : "General"),
                    OwnerName = f.AssignedToUser != null ? f.AssignedToUser.FullName : "Unassigned"
                })
                .ToListAsync();
        }

        public async Task<List<OpportunityReportItem>> GetOpportunityReportAsync(string? userId, bool isElevated, string? stage = null)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.AssignedTo == userId);
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            return await query
                .OrderByDescending(o => o.Amount)
                .Select(o => new OpportunityReportItem
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerName = o.Customer != null ? o.Customer.CustomerName : "Unknown",
                    Stage = o.Stage,
                    Amount = o.Amount,
                    Probability = o.Probability,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    OwnerName = o.AssignedToUser != null ? o.AssignedToUser.FullName : "Unassigned"
                })
                .ToListAsync();
        }

        public async Task<PipelineReportViewModel> GetPipelineReportAsync(string? userId, bool isElevated)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.AssignedTo == userId);
            }

            var opps = await query.ToListAsync();

            var vm = new PipelineReportViewModel
            {
                TotalPipelineAmount = opps.Where(o => o.Status == "Open").Sum(o => o.Amount),
                TotalWeightedPipeline = opps.Where(o => o.Status == "Open").Sum(o => (o.Amount * o.Probability) / 100m),
                Opportunities = opps.Select(o => new OpportunityReportItem
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerName = o.Customer?.CustomerName ?? "Unknown",
                    Stage = o.Stage,
                    Amount = o.Amount,
                    Probability = o.Probability,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    OwnerName = o.AssignedToUser?.FullName ?? "Unassigned"
                }).ToList()
            };

            foreach (var group in opps.GroupBy(o => o.Stage))
            {
                vm.StageWiseTotals[group.Key] = group.Sum(o => o.Amount);
            }

            foreach (var group in opps.GroupBy(o => o.AssignedToUser?.FullName ?? "Unassigned"))
            {
                vm.OwnerWiseTotals[group.Key] = group.Sum(o => o.Amount);
            }

            return vm;
        }

        public async Task<ConversionReportViewModel> GetConversionReportAsync(string? userId, bool isElevated)
        {
            var query = _context.Leads
                .Include(l => l.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(l => l.AssignedTo == userId);
            }

            var leads = await query.ToListAsync();

            var vm = new ConversionReportViewModel
            {
                TotalLeads = leads.Count,
                ConvertedLeads = leads.Count(l => l.Status == "Converted"),
                LostLeads = leads.Count(l => l.Status == "Lost"),
                OpenLeads = leads.Count(l => l.Status != "Converted" && l.Status != "Lost"),
                ConvertedList = leads.Where(l => l.Status == "Converted").Select(l => new LeadReportItem
                {
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    ExpectedValue = l.ExpectedValue,
                    OwnerName = l.AssignedToUser?.FullName ?? "Unassigned",
                    CreatedDate = l.CreatedDate
                }).ToList()
            };

            return vm;
        }

        public async Task<List<UserActivityReportItem>> GetUserActivityReportAsync()
        {
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var result = new List<UserActivityReportItem>();

            var activities = await _context.Activities.AsNoTracking().ToListAsync();
            var followUps = await _context.FollowUps.AsNoTracking().ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userActivities = activities.Where(a => a.AssignedTo == user.Id).ToList();
                var userFollowUps = followUps.Where(f => f.AssignedTo == user.Id).ToList();

                result.Add(new UserActivityReportItem
                {
                    UserId = user.Id,
                    UserName = user.FullName ?? user.UserName ?? "User",
                    Role = roles.FirstOrDefault() ?? "No Role",
                    TotalActivities = userActivities.Count,
                    CallsCount = userActivities.Count(a => a.ActivityType == "Call"),
                    MeetingsCount = userActivities.Count(a => a.ActivityType == "Meeting"),
                    EmailsCount = userActivities.Count(a => a.ActivityType == "Email"),
                    TasksCount = userActivities.Count(a => a.ActivityType == "Task"),
                    FollowUpsHandled = userFollowUps.Count
                });
            }

            return result;
        }
    }
}
