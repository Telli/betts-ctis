using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BettsTax.Data.Models;

namespace BettsTax.Data
{
    /// <summary>
    /// Extended workflow instance entity - represents a running instance of a workflow with Phase 3 enhancements
    /// </summary>
    public class WorkflowInstance
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WorkflowId { get; set; } // Links to Models.Workflow

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public WorkflowInstanceStatus Status { get; set; }

        [Column(TypeName = "TEXT")]
        public string Variables { get; set; } = "{}"; // JSON storage

        [Column(TypeName = "TEXT")]
        public string Context { get; set; } = "{}"; // JSON storage
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(450)]
        public string? CompletedBy { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        // Navigation properties
        public Workflow Workflow { get; set; } = null!;
        public ApplicationUser Creator { get; set; } = null!;
        public ApplicationUser? Completer { get; set; }
        public List<WorkflowStepInstance> StepInstances { get; set; } = new();
        public List<WorkflowApproval> Approvals { get; set; } = new();
    }

    /// <summary>
    /// Workflow step instance entity - represents a running instance of a workflow step
    /// </summary>
    public class WorkflowStepInstance
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WorkflowInstanceId { get; set; }

        [Required]
        public Guid WorkflowStepId { get; set; } // Links to step in workflow definition

        [Required]
        public WorkflowStepInstanceStatus Status { get; set; }

        [Column(TypeName = "TEXT")]
        public string Input { get; set; } = "{}"; // JSON storage

        [Column(TypeName = "TEXT")]
        public string Output { get; set; } = "{}"; // JSON storage

        [MaxLength(450)]
        public string? AssignedTo { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(450)]
        public string? CompletedBy { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        // Navigation properties
        public WorkflowInstance WorkflowInstance { get; set; } = null!;
        public ApplicationUser? AssignedUser { get; set; }
        public ApplicationUser? Completer { get; set; }
    }

    /// <summary>
    /// Workflow trigger entity - defines when workflows should be triggered
    /// </summary>
    public class WorkflowTrigger
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WorkflowId { get; set; } // Links to Models.Workflow

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public WorkflowTriggerType Type { get; set; }

        [Column(TypeName = "TEXT")]
        public string Configuration { get; set; } = "{}"; // JSON storage

        public bool IsActive { get; set; } = true;

        [Required, MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Workflow Workflow { get; set; } = null!;
        public ApplicationUser Creator { get; set; } = null!;
    }

    /// <summary>
    /// Workflow approval entity - tracks approvals in workflows
    /// </summary>
    public class WorkflowApproval
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WorkflowInstanceId { get; set; }

        [Required]
        public Guid WorkflowStepInstanceId { get; set; }

        [Required, MaxLength(450)]
        public string RequiredApprover { get; set; } = string.Empty;

        [Required]
        public WorkflowApprovalStatus Status { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        [MaxLength(450)]
        public string? RespondedBy { get; set; }

        // Navigation properties
        public WorkflowInstance WorkflowInstance { get; set; } = null!;
        public WorkflowStepInstance WorkflowStepInstance { get; set; } = null!;
        public ApplicationUser RequiredApproverUser { get; set; } = null!;
        public ApplicationUser? Responder { get; set; }
    }
}