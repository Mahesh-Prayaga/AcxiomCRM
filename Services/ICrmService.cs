using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface ICrmService
    {
        // Customer
        Task<List<Customer>> GetCustomersAsync(string? userId, bool isElevated, string? searchName = null, string? searchEmail = null, string? searchPhone = null, string? searchCompany = null, string? status = null);
        Task<Customer?> GetCustomerByIdAsync(int id, string? userId, bool isElevated);
        Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string currentUserName, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer, string currentUserId, string currentUserName, bool isElevated, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(int id, string currentUserId, string currentUserName, bool isElevated, string? ipAddress);
        Task<bool> IsCustomerEmailUniqueAsync(string email, int? excludeCustomerId = null);
        Task<bool> IsCustomerPhoneUniqueAsync(string phone, int? excludeCustomerId = null);

        // Lead
        Task<List<Lead>> GetLeadsAsync(string? userId, bool isElevated, string? searchName = null, string? searchCompany = null, string? status = null, string? assignedTo = null);
        Task<Lead?> GetLeadByIdAsync(int id, string? userId, bool isElevated);
        Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string currentUserName, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead, string currentUserId, string currentUserName, bool isElevated, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int id, string currentUserId, string currentUserName, bool isElevated, string? ipAddress);
        Task<(bool Success, string? ErrorMessage, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(int leadId, string currentUserId, string currentUserName, bool isElevated, string? ipAddress, string? opportunityName = null, decimal? opportunityAmount = null);

        // Opportunity
        Task<List<Opportunity>> GetOpportunitiesAsync(string? userId, bool isElevated, string? searchName = null, int? customerId = null, string? stage = null, string? status = null);
        Task<Opportunity?> GetOpportunityByIdAsync(int id, string? userId, bool isElevated);
        Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserName, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserName, bool isElevated, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int id, string currentUserId, string currentUserName, bool isElevated, string? ipAddress);

        // FollowUp
        Task<List<FollowUp>> GetFollowUpsAsync(string? userId, bool isElevated, DateTime? date = null, string? status = null, string? assignedTo = null, int? customerId = null, int? leadId = null);
        Task<FollowUp?> GetFollowUpByIdAsync(int id, string? userId, bool isElevated);
        Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserName, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string currentUserId, string currentUserName, bool isElevated, string? remarks, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, DateTime newDate, string currentUserId, string currentUserName, bool isElevated, string? remarks, string? ipAddress);
        Task<(bool Success, string? ErrorMessage)> DeleteFollowUpAsync(int id, string currentUserId, string currentUserName, bool isElevated, string? ipAddress);

        // Activity
        Task<List<Activity>> GetActivitiesAsync(string? userId, bool isElevated, string? activityType = null, DateTime? date = null, string? status = null, string? assignedTo = null);
        Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity, string currentUserId, string currentUserName, string? ipAddress);
    }
}
