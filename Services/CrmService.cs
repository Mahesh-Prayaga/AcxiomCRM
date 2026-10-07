using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public class CrmService : ICrmService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CrmService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        #region Customer

        public async Task<List<Customer>> GetCustomersAsync(
            string? userId,
            bool isElevated,
            string? searchName = null,
            string? searchEmail = null,
            string? searchPhone = null,
            string? searchCompany = null,
            string? status = null)
        {
            var query = _context.Customers
                .Include(c => c.CreatedByUser)
                .Include(c => c.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
            }

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(c => c.CustomerName.Contains(searchName));
            }

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(c => c.Email.Contains(searchEmail));
            }

            if (!string.IsNullOrWhiteSpace(searchPhone))
            {
                query = query.Where(c => c.Phone.Contains(searchPhone));
            }

            if (!string.IsNullOrWhiteSpace(searchCompany))
            {
                query = query.Where(c => c.CompanyName != null && c.CompanyName.Contains(searchCompany));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id, string? userId, bool isElevated)
        {
            var query = _context.Customers
                .Include(c => c.CreatedByUser)
                .Include(c => c.AssignedToUser)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .AsQueryable();

            var customer = await query.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null) return null;

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                if (customer.AssignedTo != userId && customer.CreatedBy != userId)
                {
                    return null; // Deny access
                }
            }

            return customer;
        }

        public async Task<bool> IsCustomerEmailUniqueAsync(string email, int? excludeCustomerId = null)
        {
            var query = _context.Customers.AsNoTracking().Where(c => c.Email.ToLower() == email.Trim().ToLower());
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<bool> IsCustomerPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
        {
            var cleanPhone = Regex.Replace(phone, @"\s+", "");
            var query = _context.Customers.AsNoTracking().Where(c => c.Phone == cleanPhone || c.Phone == phone.Trim());
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(
            Customer customer,
            string currentUserId,
            string currentUserName,
            string? ipAddress)
        {
            // Business Validation
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                return (false, "Customer Name is required.", null);

            if (string.IsNullOrWhiteSpace(customer.Email))
                return (false, "Email is required.", null);

            if (!IsValidEmail(customer.Email))
                return (false, "Enter a valid email address.", null);

            if (string.IsNullOrWhiteSpace(customer.Phone))
                return (false, "Phone is required.", null);

            if (!IsValidPhone(customer.Phone))
                return (false, "Enter a valid phone number.", null);

            if (!await IsCustomerEmailUniqueAsync(customer.Email))
                return (false, "A customer with this email address already exists.", null);

            if (!await IsCustomerPhoneUniqueAsync(customer.Phone))
                return (false, "A customer with this phone number already exists.", null);

            // Auto-code generation if needed
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                customer.CustomerCode = "CUST-" + (1000 + await _context.Customers.CountAsync() + 1);
            }

            customer.CreatedDate = DateTime.UtcNow;
            customer.CreatedBy = currentUserId;
            if (string.IsNullOrEmpty(customer.AssignedTo))
            {
                customer.AssignedTo = currentUserId;
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                null,
                $"Created customer {customer.CustomerName} ({customer.CustomerCode})",
                ipAddress);

            return (true, null, customer);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(
            Customer customer,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existing == null)
                return (false, "Customer not found.");

            if (!isElevated && existing.AssignedTo != currentUserId && existing.CreatedBy != currentUserId)
                return (false, "You are not authorized to update this customer.");

            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                return (false, "Customer Name is required.");

            if (!IsValidEmail(customer.Email))
                return (false, "Enter a valid email address.");

            if (!IsValidPhone(customer.Phone))
                return (false, "Enter a valid phone number.");

            if (!await IsCustomerEmailUniqueAsync(customer.Email, customer.CustomerId))
                return (false, "Another customer with this email address already exists.");

            if (!await IsCustomerPhoneUniqueAsync(customer.Phone, customer.CustomerId))
                return (false, "Another customer with this phone number already exists.");

            var oldValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;
            if (isElevated && !string.IsNullOrEmpty(customer.AssignedTo))
            {
                existing.AssignedTo = customer.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Update",
                "Customer",
                existing.CustomerId.ToString(),
                oldValue,
                newValue,
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(
            int id,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
                return (false, "Customer not found.");

            if (!isElevated && customer.AssignedTo != currentUserId && customer.CreatedBy != currentUserId)
                return (false, "You are not authorized to delete or deactivate this customer.");

            // Soft-deactivate if has linked opportunities, else remove
            var hasOpportunities = await _context.Opportunities.AnyAsync(o => o.CustomerId == id);
            if (hasOpportunities)
            {
                customer.Status = "Inactive";
                await _context.SaveChangesAsync();
                await _auditService.LogAsync(
                    currentUserId,
                    currentUserName,
                    "Deactivate",
                    "Customer",
                    customer.CustomerId.ToString(),
                    "Status: Active",
                    "Status: Inactive (Has active dependencies)",
                    ipAddress);
            }
            else
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync(
                    currentUserId,
                    currentUserName,
                    "Delete",
                    "Customer",
                    id.ToString(),
                    $"Deleted customer {customer.CustomerName}",
                    null,
                    ipAddress);
            }

            return (true, null);
        }

        #endregion

        #region Lead

        public async Task<List<Lead>> GetLeadsAsync(
            string? userId,
            bool isElevated,
            string? searchName = null,
            string? searchCompany = null,
            string? status = null,
            string? assignedTo = null)
        {
            var query = _context.Leads
                .Include(l => l.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(l => l.AssignedTo == userId);
            }

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(l => l.LeadName.Contains(searchName));
            }

            if (!string.IsNullOrWhiteSpace(searchCompany))
            {
                query = query.Where(l => l.CompanyName != null && l.CompanyName.Contains(searchCompany));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (isElevated && !string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(l => l.AssignedTo == assignedTo);
            }

            return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        }

        public async Task<Lead?> GetLeadByIdAsync(int id, string? userId, bool isElevated)
        {
            var query = _context.Leads
                .Include(l => l.AssignedToUser)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .Include(l => l.Opportunities)
                .AsQueryable();

            var lead = await query.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null) return null;

            if (!isElevated && !string.IsNullOrEmpty(userId) && lead.AssignedTo != userId)
            {
                return null;
            }

            return lead;
        }

        public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(
            Lead lead,
            string currentUserId,
            string currentUserName,
            string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadName))
                return (false, "Lead Name is required.", null);

            if (string.IsNullOrWhiteSpace(lead.Email) || !IsValidEmail(lead.Email))
                return (false, "Enter a valid email address.", null);

            if (string.IsNullOrWhiteSpace(lead.Phone) || !IsValidPhone(lead.Phone))
                return (false, "Enter a valid phone number.", null);

            if (lead.ExpectedValue < 0)
                return (false, "Expected Value must be greater than or equal to 0.", null);

            var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
            if (!validStatuses.Contains(lead.Status))
                return (false, "Invalid lead status specified.", null);

            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                lead.LeadCode = "LEAD-" + (1000 + await _context.Leads.CountAsync() + 1);
            }

            lead.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(lead.AssignedTo))
            {
                lead.AssignedTo = currentUserId;
            }

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                null,
                $"Created lead {lead.LeadName} ({lead.LeadCode})",
                ipAddress);

            return (true, null, lead);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(
            Lead lead,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress)
        {
            var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
            if (existing == null)
                return (false, "Lead not found.");

            if (!isElevated && existing.AssignedTo != currentUserId)
                return (false, "You are not authorized to edit this lead.");

            if (string.IsNullOrWhiteSpace(lead.LeadName))
                return (false, "Lead Name is required.");

            if (!IsValidEmail(lead.Email))
                return (false, "Enter a valid email address.");

            if (!IsValidPhone(lead.Phone))
                return (false, "Enter a valid phone number.");

            if (lead.ExpectedValue < 0)
                return (false, "Expected Value must be greater than or equal to 0.");

            var validStatuses = new[] { "New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost" };
            if (!validStatuses.Contains(lead.Status))
                return (false, "Invalid lead status specified.");

            var oldValue = $"Name: {existing.LeadName}, Status: {existing.Status}, Value: {existing.ExpectedValue}";

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Status = lead.Status;
            existing.Priority = lead.Priority;
            existing.ExpectedValue = lead.ExpectedValue;

            if (isElevated && !string.IsNullOrEmpty(lead.AssignedTo))
            {
                existing.AssignedTo = lead.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {existing.LeadName}, Status: {existing.Status}, Value: {existing.ExpectedValue}";

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Update",
                "Lead",
                existing.LeadId.ToString(),
                oldValue,
                newValue,
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(
            int id,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null)
                return (false, "Lead not found.");

            if (!isElevated && lead.AssignedTo != currentUserId)
                return (false, "You are not authorized to delete this lead.");

            var hasOpportunities = await _context.Opportunities.AnyAsync(o => o.LeadId == id);
            if (hasOpportunities || lead.Status == "Converted")
            {
                lead.Status = "Lost";
                await _context.SaveChangesAsync();
                await _auditService.LogAsync(
                    currentUserId,
                    currentUserName,
                    "Deactivate",
                    "Lead",
                    lead.LeadId.ToString(),
                    "Status: Active",
                    "Status: Lost (Archived)",
                    ipAddress);
            }
            else
            {
                _context.Leads.Remove(lead);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync(
                    currentUserId,
                    currentUserName,
                    "Delete",
                    "Lead",
                    id.ToString(),
                    $"Deleted lead {lead.LeadName}",
                    null,
                    ipAddress);
            }

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer, Opportunity? Opportunity)> ConvertLeadAsync(
            int leadId,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress,
            string? opportunityName = null,
            decimal? opportunityAmount = null)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
            if (lead == null)
                return (false, "Lead not found.", null, null);

            if (!isElevated && lead.AssignedTo != currentUserId)
                return (false, "You are not authorized to convert this lead.", null, null);

            if (lead.Status == "Converted")
                return (false, "Lead is already converted.", null, null);

            // 1. Check or create Customer
            var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
            Customer targetCustomer;

            if (existingCustomer != null)
            {
                targetCustomer = existingCustomer;
            }
            else
            {
                targetCustomer = new Customer
                {
                    CustomerCode = "CUST-" + (1000 + await _context.Customers.CountAsync() + 1),
                    CustomerName = lead.LeadName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUserId,
                    AssignedTo = lead.AssignedTo ?? currentUserId
                };
                _context.Customers.Add(targetCustomer);
                await _context.SaveChangesAsync();
            }

            // 2. Optionally create Opportunity if amount provided or expected value > 0
            Opportunity? newOpportunity = null;
            decimal oppAmount = opportunityAmount ?? lead.ExpectedValue;
            if (oppAmount > 0)
            {
                newOpportunity = new Opportunity
                {
                    OpportunityName = string.IsNullOrWhiteSpace(opportunityName) ? $"{lead.LeadName} - Deal" : opportunityName,
                    CustomerId = targetCustomer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = oppAmount,
                    Stage = "Qualification",
                    Probability = 20,
                    ExpectedCloseDate = DateTime.Today.AddDays(30),
                    Status = "Open",
                    AssignedTo = lead.AssignedTo ?? currentUserId,
                    CreatedDate = DateTime.UtcNow,
                    Notes = $"Converted from Lead {lead.LeadCode}"
                };
                _context.Opportunities.Add(newOpportunity);
                await _context.SaveChangesAsync();
            }

            // 3. Mark Lead as Converted
            lead.Status = "Converted";
            await _context.SaveChangesAsync();

            // 4. Record in Audit Log
            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Lead Conversion",
                "Lead",
                lead.LeadId.ToString(),
                $"Status: Qualified",
                $"Converted to Customer ID: {targetCustomer.CustomerId}, Opportunity ID: {newOpportunity?.OpportunityId}",
                ipAddress);

            return (true, null, targetCustomer, newOpportunity);
        }

        #endregion

        #region Opportunity

        public async Task<List<Opportunity>> GetOpportunitiesAsync(
            string? userId,
            bool isElevated,
            string? searchName = null,
            int? customerId = null,
            string? stage = null,
            string? status = null)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.AssignedTo == userId);
            }

            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(o => o.OpportunityName.Contains(searchName));
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id, string? userId, bool isElevated)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            var opp = await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opp == null) return null;

            if (!isElevated && !string.IsNullOrEmpty(userId) && opp.AssignedTo != userId)
            {
                return null;
            }

            return opp;
        }

        public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(
            Opportunity opportunity,
            string currentUserId,
            string currentUserName,
            string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
                return (false, "Opportunity Name is required.", null);

            if (opportunity.CustomerId <= 0)
                return (false, "A valid Customer must be selected.", null);

            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == opportunity.CustomerId);
            if (!customerExists)
                return (false, "Selected Customer does not exist.", null);

            // Mandatory Business Validation:
            // 1. Amount cannot be negative and must be > 0
            if (opportunity.Amount <= 0)
                return (false, "Opportunity Amount must be greater than 0.", null);

            // 2. Probability must be between 0 and 100
            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.", null);

            // 3. ExpectedCloseDate cannot be in the past for an active opportunity
            var isActive = opportunity.Stage != "Won" && opportunity.Stage != "Lost";
            if (isActive && opportunity.ExpectedCloseDate.Date < DateTime.Today)
                return (false, "Expected Close Date cannot be in the past.", null);

            // Set Status based on Stage
            if (opportunity.Stage == "Won") opportunity.Status = "Won";
            else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";
            else opportunity.Status = "Open";

            opportunity.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(opportunity.AssignedTo))
            {
                opportunity.AssignedTo = currentUserId;
            }

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "Opportunity",
                opportunity.OpportunityId.ToString(),
                null,
                $"Created opportunity {opportunity.OpportunityName} for amount {opportunity.Amount:C}",
                ipAddress);

            return (true, null, opportunity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(
            Opportunity opportunity,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
            if (existing == null)
                return (false, "Opportunity not found.");

            if (!isElevated && existing.AssignedTo != currentUserId)
                return (false, "You are not authorized to edit this opportunity.");

            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
                return (false, "Opportunity Name is required.");

            if (opportunity.Amount <= 0)
                return (false, "Opportunity Amount must be greater than 0.");

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.");

            var isActive = opportunity.Stage != "Won" && opportunity.Stage != "Lost";
            if (isActive && opportunity.ExpectedCloseDate.Date < DateTime.Today)
                return (false, "Expected Close Date cannot be in the past.");

            var oldValue = $"Name: {existing.OpportunityName}, Stage: {existing.Stage}, Amount: {existing.Amount}, Prob: {existing.Probability}";

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.LeadId = opportunity.LeadId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.Notes = opportunity.Notes;

            if (opportunity.Stage == "Won") existing.Status = "Won";
            else if (opportunity.Stage == "Lost") existing.Status = "Lost";
            else existing.Status = "Open";

            if (isElevated && !string.IsNullOrEmpty(opportunity.AssignedTo))
            {
                existing.AssignedTo = opportunity.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {existing.OpportunityName}, Stage: {existing.Stage}, Amount: {existing.Amount}, Prob: {existing.Probability}";

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Update",
                "Opportunity",
                existing.OpportunityId.ToString(),
                oldValue,
                newValue,
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(
            int id,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress)
        {
            var opp = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opp == null)
                return (false, "Opportunity not found.");

            if (!isElevated && opp.AssignedTo != currentUserId)
                return (false, "You are not authorized to delete this opportunity.");

            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Delete",
                "Opportunity",
                id.ToString(),
                $"Deleted opportunity {opp.OpportunityName}",
                null,
                ipAddress);

            return (true, null);
        }

        #endregion

        #region FollowUp

        public async Task<List<FollowUp>> GetFollowUpsAsync(
            string? userId,
            bool isElevated,
            DateTime? date = null,
            string? status = null,
            string? assignedTo = null,
            int? customerId = null,
            int? leadId = null)
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

            if (date.HasValue)
            {
                query = query.Where(f => f.FollowUpDate.Date == date.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (isElevated && !string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(f => f.AssignedTo == assignedTo);
            }

            if (customerId.HasValue)
            {
                query = query.Where(f => f.CustomerId == customerId.Value);
            }

            if (leadId.HasValue)
            {
                query = query.Where(f => f.LeadId == leadId.Value);
            }

            return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id, string? userId, bool isElevated)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedToUser)
                .AsQueryable();

            var followUp = await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null) return null;

            if (!isElevated && !string.IsNullOrEmpty(userId) && followUp.AssignedTo != userId)
            {
                return null;
            }

            return followUp;
        }

        public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(
            FollowUp followUp,
            string currentUserId,
            string currentUserName,
            string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(followUp.Subject))
                return (false, "Subject is required.", null);

            // Mandatory Business Rule:
            // Follow-Up Date cannot be earlier than today for a new/planned follow-up
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
                return (false, "Follow-up date cannot be earlier than today.", null);

            followUp.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(followUp.AssignedTo))
            {
                followUp.AssignedTo = currentUserId;
            }

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                null,
                $"Scheduled follow-up '{followUp.Subject}' on {followUp.FollowUpDate:yyyy-MM-dd}",
                ipAddress);

            return (true, null, followUp);
        }

        public async Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(
            int id,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? remarks,
            string? ipAddress)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
                return (false, "Follow-up not found.");

            if (!isElevated && followUp.AssignedTo != currentUserId)
                return (false, "You are not authorized to update this follow-up.");

            var oldStatus = followUp.Status;
            followUp.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                    ? $"[Completed]: {remarks}"
                    : $"{followUp.Remarks} | [Completed]: {remarks}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Complete",
                "FollowUp",
                id.ToString(),
                $"Status: {oldStatus}",
                $"Status: Completed",
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(
            int id,
            DateTime newDate,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? remarks,
            string? ipAddress)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
                return (false, "Follow-up not found.");

            if (!isElevated && followUp.AssignedTo != currentUserId)
                return (false, "You are not authorized to reschedule this follow-up.");

            if (newDate.Date < DateTime.Today)
                return (false, "Follow-up date cannot be earlier than today.");

            var oldDateStr = followUp.FollowUpDate.ToString("yyyy-MM-dd");
            followUp.FollowUpDate = newDate;
            followUp.Status = "Planned";

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = string.IsNullOrWhiteSpace(followUp.Remarks)
                    ? $"[Rescheduled to {newDate:yyyy-MM-dd}]: {remarks}"
                    : $"{followUp.Remarks} | [Rescheduled]: {remarks}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Reschedule",
                "FollowUp",
                id.ToString(),
                $"Date: {oldDateStr}",
                $"Date: {newDate:yyyy-MM-dd}",
                ipAddress);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteFollowUpAsync(
            int id,
            string currentUserId,
            string currentUserName,
            bool isElevated,
            string? ipAddress)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
                return (false, "Follow-up not found.");

            if (!isElevated && followUp.AssignedTo != currentUserId)
                return (false, "You are not authorized to delete this follow-up.");

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Delete",
                "FollowUp",
                id.ToString(),
                $"Subject: {followUp.Subject}",
                null,
                ipAddress);

            return (true, null);
        }

        #endregion

        #region Activity

        public async Task<List<Activity>> GetActivitiesAsync(
            string? userId,
            bool isElevated,
            string? activityType = null,
            DateTime? date = null,
            string? status = null,
            string? assignedTo = null)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedToUser)
                .AsNoTracking()
                .AsQueryable();

            if (!isElevated && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.AssignedTo == userId);
            }

            if (!string.IsNullOrWhiteSpace(activityType))
            {
                query = query.Where(a => a.ActivityType == activityType);
            }

            if (date.HasValue)
            {
                query = query.Where(a => a.ActivityDate.Date == date.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (isElevated && !string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(a => a.AssignedTo == assignedTo);
            }

            return await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
        }

        public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(
            Activity activity,
            string currentUserId,
            string currentUserName,
            string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(activity.Subject))
                return (false, "Subject is required.", null);

            activity.CreatedDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(activity.AssignedTo))
            {
                activity.AssignedTo = currentUserId;
            }

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "Activity",
                activity.ActivityId.ToString(),
                null,
                $"Recorded {activity.ActivityType}: {activity.Subject}",
                ipAddress);

            return (true, null, activity);
        }

        #endregion

        #region Helpers

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

        private static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            return Regex.IsMatch(phone, @"^[0-9+\-\s()]{7,20}$");
        }

        #endregion
    }
}
