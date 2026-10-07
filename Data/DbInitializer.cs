using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = new[] { "Admin", "Manager", "SalesExecutive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@acxiomcrm.com",
                    Email = "admin@acxiomcrm.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = "manager@acxiomcrm.com",
                    Email = "manager@acxiomcrm.com",
                    FullName = "Sarah Manager",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(managerUser, "Manager@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }

            var salesUser1 = await userManager.FindByEmailAsync("sales1@acxiomcrm.com");
            if (salesUser1 == null)
            {
                salesUser1 = new ApplicationUser
                {
                    UserName = "sales1@acxiomcrm.com",
                    Email = "sales1@acxiomcrm.com",
                    FullName = "Alice Jenkins",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser1, "Sales@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser1, "SalesExecutive");
                }
            }

            var salesUser2 = await userManager.FindByEmailAsync("sales2@acxiomcrm.com");
            if (salesUser2 == null)
            {
                salesUser2 = new ApplicationUser
                {
                    UserName = "sales2@acxiomcrm.com",
                    Email = "sales2@acxiomcrm.com",
                    FullName = "Bob Martinez",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(salesUser2, "Sales@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(salesUser2, "SalesExecutive");
                }
            }

            // 3. Seed Customers if empty
            if (!context.Customers.Any())
            {
                var customers = new[]
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Acme Global Industries",
                        Email = "contact@acmeglobal.com",
                        Phone = "9876543210",
                        CompanyName = "Acme Corp",
                        Address = "100 Tech Blvd",
                        City = "New York",
                        State = "NY",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-60),
                        CreatedBy = adminUser.Id,
                        AssignedTo = salesUser1.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "Nexus Software Solutions",
                        Email = "info@nexussoftware.com",
                        Phone = "9876543211",
                        CompanyName = "Nexus Inc",
                        Address = "45 Silicon Ave",
                        City = "San Jose",
                        State = "CA",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-45),
                        CreatedBy = adminUser.Id,
                        AssignedTo = salesUser1.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "Summit Healthcare Partners",
                        Email = "procure@summithealth.com",
                        Phone = "9876543212",
                        CompanyName = "Summit Health",
                        Address = "78 Medical Center Dr",
                        City = "Chicago",
                        State = "IL",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        CreatedBy = managerUser.Id,
                        AssignedTo = salesUser2.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Vanguard Logistics",
                        Email = "ops@vanguardlog.com",
                        Phone = "9876543213",
                        CompanyName = "Vanguard Logistics Ltd",
                        Address = "12 Harbor Way",
                        City = "Dallas",
                        State = "TX",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        CreatedBy = adminUser.Id,
                        AssignedTo = salesUser2.Id
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            // 4. Seed Leads if empty
            if (!context.Leads.Any())
            {
                var leads = new[]
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "David Clark",
                        Email = "david.clark@pioneergas.com",
                        Phone = "9123456780",
                        CompanyName = "Pioneer Energy",
                        Source = "Website",
                        Status = "New",
                        Priority = "High",
                        ExpectedValue = 50000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        AssignedTo = salesUser1.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Elena Rostova",
                        Email = "elena.r@fintechgroup.com",
                        Phone = "9123456781",
                        CompanyName = "FinTech Group",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "Medium",
                        ExpectedValue = 75000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-12),
                        AssignedTo = salesUser1.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Marcus Brody",
                        Email = "marcus.b@brodyretail.com",
                        Phone = "9123456782",
                        CompanyName = "Brody Retail chains",
                        Source = "Cold Call",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 120000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-8),
                        AssignedTo = salesUser2.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Sophia Turner",
                        Email = "sophia.t@aerodynamics.com",
                        Phone = "9123456783",
                        CompanyName = "Aero Dynamics Inc",
                        Source = "Trade Show",
                        Status = "Qualified",
                        Priority = "Medium",
                        ExpectedValue = 40000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        AssignedTo = salesUser1.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1005",
                        LeadName = "Jason Miller",
                        Email = "jason.m@solarsystems.com",
                        Phone = "9123456784",
                        CompanyName = "Solar Systems Direct",
                        Source = "Social Media",
                        Status = "Lost",
                        Priority = "Low",
                        ExpectedValue = 15000m,
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        AssignedTo = salesUser2.Id
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // 5. Seed Opportunities if empty
            if (!context.Opportunities.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-1001");
                var cust2 = context.Customers.First(c => c.CustomerCode == "CUST-1002");
                var cust3 = context.Customers.First(c => c.CustomerCode == "CUST-1003");
                var cust4 = context.Customers.First(c => c.CustomerCode == "CUST-1004");

                var opportunities = new[]
                {
                    new Opportunity
                    {
                        OpportunityName = "Enterprise Cloud Migration",
                        CustomerId = cust1.CustomerId,
                        Amount = 85000m,
                        Stage = "Proposal",
                        Probability = 60,
                        ExpectedCloseDate = DateTime.Today.AddDays(25),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-20),
                        AssignedTo = salesUser1.Id,
                        Notes = "Presented RFP response, waiting for executive review."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Security Operations Expansion",
                        CustomerId = cust1.CustomerId,
                        Amount = 45000m,
                        Stage = "Negotiation",
                        Probability = 80,
                        ExpectedCloseDate = DateTime.Today.AddDays(10),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-15),
                        AssignedTo = salesUser1.Id,
                        Notes = "Contract redlining in progress."
                    },
                    new Opportunity
                    {
                        OpportunityName = "CRM Infrastructure Upgrade",
                        CustomerId = cust2.CustomerId,
                        Amount = 110000m,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.Today.AddDays(-5),
                        Status = "Won",
                        CreatedDate = DateTime.UtcNow.AddDays(-40),
                        AssignedTo = salesUser1.Id,
                        Notes = "Signed contract received. First installment billed."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Healthcare Compliance Analytics",
                        CustomerId = cust3.CustomerId,
                        Amount = 95000m,
                        Stage = "Qualification",
                        Probability = 25,
                        ExpectedCloseDate = DateTime.Today.AddDays(45),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-10),
                        AssignedTo = salesUser2.Id,
                        Notes = "Initial requirements gathering workshop scheduled."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Fleet Tracking Integration",
                        CustomerId = cust4.CustomerId,
                        Amount = 30000m,
                        Stage = "Lost",
                        Probability = 0,
                        ExpectedCloseDate = DateTime.Today.AddDays(-15),
                        Status = "Lost",
                        CreatedDate = DateTime.UtcNow.AddDays(-35),
                        AssignedTo = salesUser2.Id,
                        Notes = "Client chose in-house proprietary solution."
                    }
                };

                context.Opportunities.AddRange(opportunities);
                await context.SaveChangesAsync();
            }

            // 6. Seed FollowUps if empty
            if (!context.FollowUps.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-1001");
                var lead1 = context.Leads.First(l => l.LeadCode == "LEAD-1001");

                var followUps = new[]
                {
                    new FollowUp
                    {
                        CustomerId = cust1.CustomerId,
                        FollowUpDate = DateTime.Today.AddDays(2),
                        FollowUpType = "Meeting",
                        Subject = "Quarterly Business Review & Cloud Scope",
                        Remarks = "Meet with VP of Engineering to discuss rollout schedule.",
                        Status = "Planned",
                        AssignedTo = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new FollowUp
                    {
                        LeadId = lead1.LeadId,
                        FollowUpDate = DateTime.Today.AddDays(4),
                        FollowUpType = "Call",
                        Subject = "Introductory Product Demo",
                        Remarks = "Walk through the pipeline automation features.",
                        Status = "Planned",
                        AssignedTo = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new FollowUp
                    {
                        CustomerId = cust1.CustomerId,
                        FollowUpDate = DateTime.Today.AddDays(-3),
                        FollowUpType = "Email",
                        Subject = "Sent proposal pricing tier document",
                        Remarks = "Emailed updated rate sheet.",
                        Status = "Completed",
                        AssignedTo = salesUser1.Id,
                        CreatedDate = DateTime.UtcNow.AddDays(-5)
                    }
                };

                context.FollowUps.AddRange(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities if empty
            if (!context.Activities.Any())
            {
                var cust1 = context.Customers.First(c => c.CustomerCode == "CUST-1001");
                var lead2 = context.Leads.First(l => l.LeadCode == "LEAD-1002");

                var activities = new[]
                {
                    new Activity
                    {
                        ActivityType = "Call",
                        Subject = "Initial discovery phone call",
                        Description = "Discussed current CRM pain points and project timelines.",
                        ActivityDate = DateTime.UtcNow.AddDays(-7),
                        CustomerId = cust1.CustomerId,
                        AssignedTo = salesUser1.Id,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-7)
                    },
                    new Activity
                    {
                        ActivityType = "Email",
                        Subject = "Brochure and Case Studies shared",
                        Description = "Sent FinTech industry case study and customer testimonials.",
                        ActivityDate = DateTime.UtcNow.AddDays(-6),
                        LeadId = lead2.LeadId,
                        AssignedTo = salesUser1.Id,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-6)
                    },
                    new Activity
                    {
                        ActivityType = "Meeting",
                        Subject = "Architecture Review Session",
                        Description = "Technical meeting with Solution Architect team.",
                        ActivityDate = DateTime.UtcNow.AddDays(3),
                        CustomerId = cust1.CustomerId,
                        AssignedTo = salesUser2.Id,
                        Status = "Planned",
                        CreatedDate = DateTime.UtcNow.AddDays(-1)
                    }
                };

                context.Activities.AddRange(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Initial Audit Logs
            if (!context.AuditLogs.Any())
            {
                var audit = new AuditLog
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.UserName,
                    Action = "System Initialized",
                    EntityName = "System",
                    RecordId = "0",
                    OldValue = null,
                    NewValue = "Database seeded with baseline roles, demo accounts, and CRM sample records.",
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = "127.0.0.1"
                };
                context.AuditLogs.Add(audit);
                await context.SaveChangesAsync();
            }
        }
    }
}
