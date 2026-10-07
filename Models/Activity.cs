using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity Type is required.")]
        [MaxLength(50)]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Activity Date is required.")]
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Lead (Optional)")]
        public int? LeadId { get; set; }

        [MaxLength(450)]
        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Completed"; // Planned, Completed

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedToUser { get; set; }
    }
}
