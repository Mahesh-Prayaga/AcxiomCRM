using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IReportService
    {
        Task<List<CustomerReportItem>> GetCustomerReportAsync(string? userId, bool isElevated, string? status = null);
        Task<List<LeadReportItem>> GetLeadReportAsync(string? userId, bool isElevated, string? source = null, string? status = null);
        Task<List<FollowUpReportItem>> GetFollowUpReportAsync(string? userId, bool isElevated, string? status = null);
        Task<List<OpportunityReportItem>> GetOpportunityReportAsync(string? userId, bool isElevated, string? stage = null);
        Task<PipelineReportViewModel> GetPipelineReportAsync(string? userId, bool isElevated);
        Task<ConversionReportViewModel> GetConversionReportAsync(string? userId, bool isElevated);
        Task<List<UserActivityReportItem>> GetUserActivityReportAsync();
    }
}
