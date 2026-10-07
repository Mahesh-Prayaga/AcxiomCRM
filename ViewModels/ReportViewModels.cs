using System;
using System.Collections.Generic;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class CustomerReportItem
    {
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class LeadReportItem
    {
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsConverted => Status == "Converted";
    }

    public class FollowUpReportItem
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string FollowUpType { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RelatedTo { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public bool IsOverdue => Status == "Planned" && FollowUpDate.Date < DateTime.Today;
    }

    public class OpportunityReportItem
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Probability { get; set; }
        public decimal WeightedValue => (Amount * Probability) / 100m;
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
    }

    public class PipelineReportViewModel
    {
        public decimal TotalPipelineAmount { get; set; }
        public decimal TotalWeightedPipeline { get; set; }
        public Dictionary<string, decimal> StageWiseTotals { get; set; } = new();
        public Dictionary<string, decimal> OwnerWiseTotals { get; set; } = new();
        public List<OpportunityReportItem> Opportunities { get; set; } = new();
    }

    public class ConversionReportViewModel
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int OpenLeads { get; set; }
        public int LostLeads { get; set; }
        public double ConversionRate => TotalLeads > 0 ? Math.Round((double)ConvertedLeads / TotalLeads * 100, 2) : 0;
        public List<LeadReportItem> ConvertedList { get; set; } = new();
    }

    public class UserActivityReportItem
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalActivities { get; set; }
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int FollowUpsHandled { get; set; }
    }
}
