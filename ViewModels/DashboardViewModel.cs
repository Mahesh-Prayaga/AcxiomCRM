using System.Collections.Generic;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        // Metric Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public int PendingFollowUps { get; set; }

        // Scope description
        public string UserRole { get; set; } = string.Empty;
        public string ScopeDescription { get; set; } = string.Empty;

        // Chart 1: Lead Status
        public List<string> LeadStatusLabels { get; set; } = new();
        public List<int> LeadStatusCounts { get; set; } = new();

        // Chart 2: Opportunity Pipeline by Stage
        public List<string> PipelineStageLabels { get; set; } = new();
        public List<decimal> PipelineStageAmounts { get; set; } = new();

        // Chart 3: Monthly Sales (Won)
        public List<string> MonthlySalesLabels { get; set; } = new();
        public List<decimal> MonthlySalesAmounts { get; set; } = new();

        // Quick Lists
        public List<FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<Opportunity> TopOpportunities { get; set; } = new();
        public List<Lead> RecentLeads { get; set; } = new();
    }
}
