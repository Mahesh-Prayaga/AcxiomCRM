using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, string role, bool isElevated)
        {
            var vm = new DashboardViewModel
            {
                UserRole = role,
                ScopeDescription = isElevated ? "Organization Wide Data Scope" : "Assigned Sales Representative Scope"
            };

            // Customers scope
            var customerQuery = _context.Customers.AsNoTracking().AsQueryable();
            if (!isElevated) customerQuery = customerQuery.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
            vm.TotalCustomers = await customerQuery.CountAsync();

            // Leads scope
            var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
            if (!isElevated) leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
            vm.TotalLeads = await leadQuery.CountAsync();
            vm.OpenLeads = await leadQuery.CountAsync(l => l.Status == "New" || l.Status == "Contacted" || l.Status == "Qualified");

            // Opportunities scope
            var oppQuery = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
            if (!isElevated) oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
            vm.TotalOpportunities = await oppQuery.CountAsync();
            vm.OpenOpportunities = await oppQuery.CountAsync(o => o.Status == "Open");
            vm.WonOpportunities = await oppQuery.CountAsync(o => o.Status == "Won");
            vm.LostOpportunities = await oppQuery.CountAsync(o => o.Status == "Lost");
            vm.TotalPipelineValue = await oppQuery.Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount) ?? 0m;

            // Follow-ups scope
            var followUpQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking().AsQueryable();
            if (!isElevated) followUpQuery = followUpQuery.Where(f => f.AssignedTo == userId);
            vm.PendingFollowUps = await followUpQuery.CountAsync(f => f.Status == "Planned");

            // Chart 1: Lead Status (New, Contacted, Qualified, Lost, Converted)
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Lost", "Converted" };
            var leadGroup = await leadQuery
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            vm.LeadStatusLabels = leadStatuses.ToList();
            vm.LeadStatusCounts = leadStatuses.Select(s => leadGroup.FirstOrDefault(g => g.Status == s)?.Count ?? 0).ToList();

            // Chart 2: Pipeline by Stage (Qualification, Proposal, Negotiation, Won, Lost)
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var stageGroup = await oppQuery
                .GroupBy(o => o.Stage)
                .Select(g => new { Stage = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            vm.PipelineStageLabels = stages.ToList();
            vm.PipelineStageAmounts = stages.Select(s => stageGroup.FirstOrDefault(g => g.Stage == s)?.Total ?? 0m).ToList();

            // Chart 3: Monthly Sales / Won deals over the last 6 months
            var now = DateTime.UtcNow;
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var monthLabel = monthStart.ToString("MMM yyyy");

                var salesTotal = await oppQuery
                    .Where(o => o.Status == "Won" && o.CreatedDate >= monthStart && o.CreatedDate < monthEnd)
                    .SumAsync(o => (decimal?)o.Amount) ?? 0m;

                vm.MonthlySalesLabels.Add(monthLabel);
                vm.MonthlySalesAmounts.Add(salesTotal);
            }

            // Quick display lists
            vm.UpcomingFollowUps = await followUpQuery
                .Where(f => f.Status == "Planned")
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            vm.TopOpportunities = await oppQuery
                .Where(o => o.Status == "Open")
                .OrderByDescending(o => o.Amount)
                .Take(5)
                .ToListAsync();

            vm.RecentLeads = await leadQuery
                .OrderByDescending(l => l.CreatedDate)
                .Take(5)
                .ToListAsync();

            return vm;
        }
    }
}
