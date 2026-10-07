using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        [Display(Name = "Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [Display(Name = "Follow-Up Date")]
        public DateTime FollowUpDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [MaxLength(50)]
        [Display(Name = "Follow-Up Type")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        public string? Remarks { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [MaxLength(450)]
        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedToUser { get; set; }
    }
}
