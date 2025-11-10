using System.Collections.Generic;
using System.Text.Json;
using BettsTax.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BettsTax.Data
{
    public static class WorkflowSeeder
    {
        public static async Task SeedWorkflowTemplatesAsync(IServiceProvider sp)
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();

            // Check if workflow templates already exist
            if (await context.WorkflowRuleTemplates.AnyAsync())
                return;

            // Seed basic workflow templates
            var templates = new List<WorkflowTemplate>
            {
                new WorkflowTemplate
                {
                    Name = "Payment Approval Notification",
                    Description = "Automatically notify client when payment is approved",
                    Category = "Payment",
                    TriggerType = "PaymentApproved",
                    IsPublic = true,
                    CreatedBy = "system",
                    CreatedDate = DateTime.UtcNow,
                    UsageCount = 0,
                    Rating = 0,
                    TagsJson = JsonSerializer.Serialize(new[] { "payment", "notification", "approval" }),
                    DefinitionJson = JsonSerializer.Serialize(new
                    {
                        trigger = "PaymentApproved",
                        conditions = new[]
                        {
                            new { field = "payment.status", op = "equals", value = "Approved" }
                        },
                        actions = new[]
                        {
                            new { type = "SendNotification", template = "PaymentApproved", recipients = "client" },
                            new { type = "SendSms", template = "PaymentApprovedSms", recipients = "client" }
                        }
                    })
                },
                new WorkflowTemplate
                {
                    Name = "Document Verification Reminder",
                    Description = "Send reminders for pending document verification",
                    Category = "Document",
                    TriggerType = "DocumentUploaded",
                    IsPublic = true,
                    CreatedBy = "system",
                    CreatedDate = DateTime.UtcNow,
                    UsageCount = 0,
                    Rating = 0,
                    TagsJson = JsonSerializer.Serialize(new[] { "document", "verification", "reminder" }),
                    DefinitionJson = JsonSerializer.Serialize(new
                    {
                        trigger = "DocumentUploaded",
                        conditions = new[]
                        {
                            new { field = "document.status", op = "equals", value = "Pending" }
                        },
                        actions = new[]
                        {
                            new { type = "ScheduleReminder", delay = "24h", template = "DocumentVerificationReminder" }
                        }
                    })
                },
                new WorkflowTemplate
                {
                    Name = "Tax Filing Deadline Alert",
                    Description = "Alert clients about upcoming tax filing deadlines",
                    Category = "Tax",
                    TriggerType = "TaxFilingDue",
                    IsPublic = true,
                    CreatedBy = "system",
                    CreatedDate = DateTime.UtcNow,
                    UsageCount = 0,
                    Rating = 0,
                    TagsJson = JsonSerializer.Serialize(new[] { "tax", "deadline", "alert" }),
                    DefinitionJson = JsonSerializer.Serialize(new
                    {
                        trigger = "TaxFilingDue",
                        conditions = new[]
                        {
                            new { field = "taxFiling.dueDate", op = "within", value = "7d" }
                        },
                        actions = new[]
                        {
                            new { type = "SendNotification", template = "TaxDeadlineAlert", recipients = "client" },
                            new { type = "SendSms", template = "TaxDeadlineSms", recipients = "client" }
                        }
                    })
                }
            };

            await context.WorkflowRuleTemplates.AddRangeAsync(templates);
            await context.SaveChangesAsync();
        }

        public static async Task SeedEnhancedWorkflowAutomationAsync(IServiceProvider sp)
        {
            var context = sp.GetRequiredService<ApplicationDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            if (await context.Workflows.AnyAsync())
            {
                return;
            }

            var adminUser = await userManager.Users
                .Where(u => u.Email == "admin@thebettsfirmsl.com")
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync()
                ?? await userManager.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();

            if (adminUser == null)
            {
                return;
            }

            var systemUserId = adminUser.Id;
            var now = DateTime.UtcNow;

            var paymentWorkflow = new Models.Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Payment Approval Workflow",
                Description = "Automates finance approval and notifications for high-value client payments.",
                Type = WorkflowType.PaymentApproval,
                Trigger = Models.WorkflowTrigger.EventBased,
                IsActive = true,
                Priority = 1,
                TriggerConditions = JsonSerializer.Serialize(new { eventType = "payment.created", minimumAmount = 50000 }),
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { name = "Validate Payment", kind = "system" },
                    new { name = "Finance Approval", kind = "approval" },
                    new { name = "Notify Client", kind = "notification" }
                }),
                CreatedBy = systemUserId,
                UpdatedBy = systemUserId,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-1)
            };

            var documentWorkflow = new Models.Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Document Verification Workflow",
                Description = "Routes newly uploaded compliance documents through verification steps.",
                Type = WorkflowType.DocumentReview,
                Trigger = Models.WorkflowTrigger.EventBased,
                IsActive = true,
                Priority = 2,
                TriggerConditions = JsonSerializer.Serialize(new { eventType = "document.uploaded", category = "Compliance" }),
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { name = "Assign Reviewer", kind = "system" },
                    new { name = "Review Document", kind = "human" },
                    new { name = "Archive Result", kind = "system" }
                }),
                CreatedBy = systemUserId,
                UpdatedBy = systemUserId,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-2)
            };

            var complianceWorkflow = new Models.Workflow
            {
                Id = Guid.NewGuid(),
                Name = "Compliance Breach Escalation",
                Description = "Escalates overdue compliance tasks and notifies leadership.",
                Type = WorkflowType.ComplianceCheck,
                Trigger = Models.WorkflowTrigger.Scheduled,
                IsActive = true,
                Priority = 3,
                TriggerConditions = JsonSerializer.Serialize(new { schedule = "0 0 * * *", severity = "high" }),
                Steps = JsonSerializer.Serialize(new object[]
                {
                    new { name = "Identify Breaches", kind = "system" },
                    new { name = "Notify Compliance Lead", kind = "notification" },
                    new { name = "Create Task", kind = "system" }
                }),
                CreatedBy = systemUserId,
                UpdatedBy = systemUserId,
                CreatedAt = now.AddDays(-15),
                UpdatedAt = now.AddDays(-3)
            };

            await context.Workflows.AddRangeAsync(paymentWorkflow, documentWorkflow, complianceWorkflow);

            var triggers = new List<WorkflowTrigger>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowId = paymentWorkflow.Id,
                    Name = "High Value Payment",
                    Type = WorkflowTriggerType.Event,
                    Configuration = JsonSerializer.Serialize(new { eventType = "payment.created", threshold = 50000 }),
                    CreatedBy = systemUserId,
                    CreatedAt = now.AddDays(-6)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowId = documentWorkflow.Id,
                    Name = "Compliance Document Uploaded",
                    Type = WorkflowTriggerType.Event,
                    Configuration = JsonSerializer.Serialize(new { eventType = "document.uploaded", tags = new [] { "compliance" } }),
                    CreatedBy = systemUserId,
                    CreatedAt = now.AddDays(-9)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    WorkflowId = complianceWorkflow.Id,
                    Name = "Daily Compliance Scan",
                    Type = WorkflowTriggerType.Schedule,
                    Configuration = JsonSerializer.Serialize(new { cron = "0 0 * * *" }),
                    CreatedBy = systemUserId,
                    CreatedAt = now.AddDays(-14)
                }
            };

            await context.WorkflowTriggers.AddRangeAsync(triggers);

            var paymentInstance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkflowId = paymentWorkflow.Id,
                Name = "Payment Approval Demo",
                Description = "Demo instance awaiting finance approval.",
                Status = WorkflowInstanceStatus.WaitingForApproval,
                Variables = JsonSerializer.Serialize(new { paymentId = "PAY-2025-0001", amount = 72000m }),
                Context = JsonSerializer.Serialize(new { clientId = "CLN-001-2024", initiatedBy = "system" }),
                CreatedBy = systemUserId,
                CreatedAt = now.AddHours(-6),
                StartedAt = now.AddHours(-6)
            };

            var validationStep = new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = paymentInstance.Id,
                WorkflowStepId = Guid.NewGuid(),
                Status = WorkflowStepInstanceStatus.Completed,
                Input = JsonSerializer.Serialize(new { stage = "validation" }),
                Output = JsonSerializer.Serialize(new { isValid = true }),
                StartedAt = now.AddHours(-6),
                CompletedAt = now.AddHours(-5),
                CompletedBy = systemUserId
            };

            var approvalStep = new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = paymentInstance.Id,
                WorkflowStepId = Guid.NewGuid(),
                Status = WorkflowStepInstanceStatus.WaitingForApproval,
                Input = JsonSerializer.Serialize(new { amount = 72000m, currency = "SLL" }),
                StartedAt = now.AddHours(-5),
                AssignedTo = systemUserId
            };

            validationStep.WorkflowInstance = paymentInstance;
            approvalStep.WorkflowInstance = paymentInstance;

            var pendingApproval = new WorkflowApproval
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = paymentInstance.Id,
                WorkflowStepInstanceId = approvalStep.Id,
                RequiredApprover = systemUserId,
                Status = WorkflowApprovalStatus.Pending,
                Comments = "Awaiting finance review",
                RequestedAt = now.AddHours(-5).AddMinutes(15)
            };

            pendingApproval.WorkflowInstance = paymentInstance;
            pendingApproval.WorkflowStepInstance = approvalStep;

            paymentInstance.StepInstances = new List<WorkflowStepInstance> { validationStep, approvalStep };
            paymentInstance.Approvals = new List<WorkflowApproval> { pendingApproval };

            var documentInstance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkflowId = documentWorkflow.Id,
                Name = "Document Verification Demo",
                Description = "Completed document verification for onboarding client.",
                Status = WorkflowInstanceStatus.Completed,
                Variables = JsonSerializer.Serialize(new { documentId = "DOC-2025-001", type = "Tax Certificate" }),
                Context = JsonSerializer.Serialize(new { clientId = "CLN-002-2024" }),
                CreatedBy = systemUserId,
                CreatedAt = now.AddDays(-2),
                StartedAt = now.AddDays(-2),
                CompletedAt = now.AddDays(-2).AddHours(3),
                CompletedBy = systemUserId
            };

            var documentReviewStep = new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = documentInstance.Id,
                WorkflowStepId = Guid.NewGuid(),
                Status = WorkflowStepInstanceStatus.Completed,
                Input = JsonSerializer.Serialize(new { reviewer = "compliance.team" }),
                Output = JsonSerializer.Serialize(new { status = "Approved" }),
                StartedAt = now.AddDays(-2),
                CompletedAt = now.AddDays(-2).AddHours(2),
                CompletedBy = systemUserId,
                WorkflowInstance = documentInstance
            };

            documentInstance.StepInstances = new List<WorkflowStepInstance> { documentReviewStep };

            var complianceInstance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                WorkflowId = complianceWorkflow.Id,
                Name = "Compliance Escalation Demo",
                Description = "Running instance monitoring overdue compliance tasks.",
                Status = WorkflowInstanceStatus.Running,
                Variables = JsonSerializer.Serialize(new { overdueCount = 4, severity = "High" }),
                Context = JsonSerializer.Serialize(new { reportDate = now.Date }),
                CreatedBy = systemUserId,
                CreatedAt = now.AddHours(-12),
                StartedAt = now.AddHours(-12)
            };

            var complianceStep = new WorkflowStepInstance
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = complianceInstance.Id,
                WorkflowStepId = Guid.NewGuid(),
                Status = WorkflowStepInstanceStatus.Running,
                Input = JsonSerializer.Serialize(new { region = "Western" }),
                StartedAt = now.AddHours(-1),
                WorkflowInstance = complianceInstance
            };

            complianceInstance.StepInstances = new List<WorkflowStepInstance> { complianceStep };

            await context.WorkflowInstances.AddRangeAsync(paymentInstance, documentInstance, complianceInstance);

            await context.SaveChangesAsync();
        }
    }
}