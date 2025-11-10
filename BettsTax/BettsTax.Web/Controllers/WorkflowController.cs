using System;
using System.Security.Claims;
using BettsTax.Core.DTOs.Workflows;
using BettsTax.Core.Services.Interfaces;
using BettsTax.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IEnhancedWorkflowService _workflowService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(IEnhancedWorkflowService workflowService, ILogger<WorkflowController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    #region Helper Methods

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private IActionResult FromResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true });
        }

        var response = new { success = false, message = result.ErrorMessage };
        if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(response);
        }

        return BadRequest(response);
    }

    private IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { success = true, data = result.Value });
        }

        var response = new { success = false, message = result.ErrorMessage };
        if (result.ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(response);
        }

        return BadRequest(response);
    }

    #endregion

    #region Workflow Definitions

    [HttpGet("definitions")]
    public async Task<IActionResult> GetWorkflowDefinitions([FromQuery] bool includeInactive = false)
    {
        try
        {
            var result = await _workflowService.GetWorkflowDefinitionsAsync(includeInactive);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve workflow definitions");
            return StatusCode(500, new { success = false, message = "Failed to retrieve workflow definitions" });
        }
    }

    #endregion

    #region Workflow Instances

    [HttpGet("instances")]
    public async Task<IActionResult> GetWorkflowInstances([FromQuery] Guid? workflowId = null, [FromQuery] string? status = null)
    {
        try
        {
            var result = await _workflowService.GetWorkflowInstancesAsync(workflowId, status);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve workflow instances");
            return StatusCode(500, new { success = false, message = "Failed to retrieve workflow instances" });
        }
    }

    [HttpGet("instances/{instanceId:guid}")]
    public async Task<IActionResult> GetWorkflowInstance(Guid instanceId)
    {
        try
        {
            var result = await _workflowService.GetWorkflowInstanceAsync(instanceId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve workflow instance {InstanceId}", instanceId);
            return StatusCode(500, new { success = false, message = "Failed to retrieve workflow instance" });
        }
    }

    [HttpPost("instances")]
    public async Task<IActionResult> StartWorkflowInstance([FromBody] StartWorkflowInstanceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
        }

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.StartWorkflowInstanceAsync(request.WorkflowId, request.Variables, userId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start workflow instance for workflow {WorkflowId}", request.WorkflowId);
            return StatusCode(500, new { success = false, message = "Failed to start workflow instance" });
        }
    }

    [HttpPost("instances/{instanceId:guid}/cancel")]
    public async Task<IActionResult> CancelWorkflowInstance(Guid instanceId, [FromBody] CancelWorkflowInstanceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
        }

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.CancelWorkflowInstanceAsync(instanceId, userId, request.Reason);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel workflow instance {InstanceId}", instanceId);
            return StatusCode(500, new { success = false, message = "Failed to cancel workflow instance" });
        }
    }

    #endregion

    #region Step Execution

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/execute")]
    public async Task<IActionResult> ExecuteWorkflowStep(Guid instanceId, Guid stepId, [FromBody] ExecuteWorkflowStepRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.ExecuteStepAsync(instanceId, stepId, request.Inputs, userId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute step {StepId} for instance {InstanceId}", stepId, instanceId);
            return StatusCode(500, new { success = false, message = "Failed to execute workflow step" });
        }
    }

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/complete")]
    public async Task<IActionResult> CompleteWorkflowStep(Guid instanceId, Guid stepId, [FromBody] CompleteWorkflowStepRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.CompleteStepAsync(instanceId, stepId, request.Outputs, userId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete step {StepId} for instance {InstanceId}", stepId, instanceId);
            return StatusCode(500, new { success = false, message = "Failed to complete workflow step" });
        }
    }

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/assign")]
    public async Task<IActionResult> AssignWorkflowStep(Guid instanceId, Guid stepId, [FromBody] AssignWorkflowStepRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
        }

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.AssignStepAsync(instanceId, stepId, request.AssigneeId, userId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign step {StepId} for instance {InstanceId}", stepId, instanceId);
            return StatusCode(500, new { success = false, message = "Failed to assign workflow step" });
        }
    }

    #endregion

    #region Approvals

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/request-approval")]
    public async Task<IActionResult> RequestApproval(Guid instanceId, Guid stepId, [FromBody] WorkflowApprovalCommandRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
        }

        try
        {
            var result = await _workflowService.RequestApprovalAsync(instanceId, stepId, request.ApproverId, request.Comments);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request approval for step {StepId} in instance {InstanceId}", stepId, instanceId);
            return StatusCode(500, new { success = false, message = "Failed to request workflow approval" });
        }
    }

    [HttpPost("approvals/{approvalId:guid}/approve")]
    public async Task<IActionResult> ApproveStep(Guid approvalId, [FromBody] WorkflowApprovalDecisionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.ApproveStepAsync(approvalId, userId, request.Comments);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve workflow step for approval {ApprovalId}", approvalId);
            return StatusCode(500, new { success = false, message = "Failed to approve workflow step" });
        }
    }

    [HttpPost("approvals/{approvalId:guid}/reject")]
    public async Task<IActionResult> RejectStep(Guid approvalId, [FromBody] WorkflowApprovalDecisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Comments))
        {
            return BadRequest(new { success = false, message = "Comments are required to reject an approval" });
        }

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.RejectStepAsync(approvalId, userId, request.Comments!);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject workflow step for approval {ApprovalId}", approvalId);
            return StatusCode(500, new { success = false, message = "Failed to reject workflow step" });
        }
    }

    [HttpGet("approvals/pending")]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] string? approverId = null)
    {
        try
        {
            var targetApproverId = approverId;
            if (string.IsNullOrWhiteSpace(targetApproverId))
            {
                targetApproverId = GetUserId();
            }

            if (string.IsNullOrWhiteSpace(targetApproverId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.GetPendingApprovalsAsync(targetApproverId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve pending approvals");
            return StatusCode(500, new { success = false, message = "Failed to retrieve pending approvals" });
        }
    }

    #endregion

    #region Analytics

    [HttpGet("{workflowId:guid}/analytics")]
    public async Task<IActionResult> GetWorkflowAnalytics(Guid workflowId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        try
        {
            var result = await _workflowService.GetWorkflowAnalyticsAsync(workflowId, from, to);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve workflow analytics for {WorkflowId}", workflowId);
            return StatusCode(500, new { success = false, message = "Failed to retrieve workflow analytics" });
        }
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetWorkflowMetrics()
    {
        try
        {
            var result = await _workflowService.GetWorkflowMetricsAsync();
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve workflow metrics");
            return StatusCode(500, new { success = false, message = "Failed to retrieve workflow metrics" });
        }
    }

    #endregion

    #region Triggers

    [HttpGet("{workflowId:guid}/triggers")]
    public async Task<IActionResult> GetWorkflowTriggers(Guid workflowId)
    {
        try
        {
            var result = await _workflowService.GetTriggersAsync(workflowId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve workflow triggers for workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { success = false, message = "Failed to retrieve workflow triggers" });
        }
    }

    [HttpPost("{workflowId:guid}/triggers")]
    public async Task<IActionResult> CreateWorkflowTrigger(Guid workflowId, [FromBody] CreateTriggerRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
        }

        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.CreateTriggerAsync(workflowId, request, userId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create workflow trigger for workflow {WorkflowId}", workflowId);
            return StatusCode(500, new { success = false, message = "Failed to create workflow trigger" });
        }
    }

    [HttpDelete("triggers/{triggerId:guid}")]
    public async Task<IActionResult> DeleteWorkflowTrigger(Guid triggerId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { success = false, message = "User context not available" });
            }

            var result = await _workflowService.DeleteTriggerAsync(triggerId, userId);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workflow trigger {TriggerId}", triggerId);
            return StatusCode(500, new { success = false, message = "Failed to delete workflow trigger" });
        }
    }

    [HttpPost("triggers/evaluate")]
    public async Task<IActionResult> EvaluateTriggers([FromBody] EvaluateTriggersRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request", errors = ModelState });
        }

        try
        {
            var result = await _workflowService.EvaluateTriggersAsync(request.EventType, request.EventData);
            return FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate workflow triggers");
            return StatusCode(500, new { success = false, message = "Failed to evaluate workflow triggers" });
        }
    }

    #endregion
}
