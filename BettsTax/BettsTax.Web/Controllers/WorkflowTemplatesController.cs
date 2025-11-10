using Microsoft.AspNetCore.Mvc;
using BettsTax.Core.Services;
using BettsTax.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BettsTax.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkflowTemplatesController : ControllerBase
    {
        private readonly IWorkflowEngineService _workflowEngineService;

        public WorkflowTemplatesController(IWorkflowEngineService workflowEngineService)
        {
            _workflowEngineService = workflowEngineService;
        }

        /// <summary>
        /// Gets all workflow templates
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkflowTemplate>>> GetTemplates(
            [FromQuery] bool includeSystemTemplates = true)
        {
            var templates = await _workflowEngineService.GetWorkflowTemplatesAsync(includeSystemTemplates);
            return Ok(templates);
        }

        /// <summary>
        /// Creates a workflow from a template
        /// </summary>
        [HttpPost("{templateId}/create-workflow")]
        public async Task<ActionResult<Workflow>> CreateWorkflowFromTemplate(
            Guid templateId,
            [FromBody] CreateWorkflowRequest request)
        {
            try
            {
                var workflow = await _workflowEngineService.CreateWorkflowFromTemplateAsync(
                    templateId, 
                    request.Name, 
                    request.Parameters);
                return Ok(workflow);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Template {templateId} not found");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Gets workflow execution history for monitoring
        /// </summary>
        [HttpGet("{workflowId}/executions")]
        public async Task<ActionResult<IEnumerable<WorkflowExecution>>> GetWorkflowExecutions(
            Guid workflowId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var executions = await _workflowEngineService.GetWorkflowExecutionsAsync(workflowId, page, pageSize);
            return Ok(executions);
        }

        /// <summary>
        /// Triggers a workflow execution manually
        /// </summary>
        [HttpPost("{workflowId}/trigger")]
        public async Task<ActionResult<WorkflowExecution>> TriggerWorkflow(
            Guid workflowId,
            [FromBody] Dictionary<string, object> contextData)
        {
            try
            {
                var execution = await _workflowEngineService.TriggerWorkflowAsync(workflowId, contextData);
                return Ok(execution);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Workflow {workflowId} not found");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Gets a specific workflow execution status
        /// </summary>
        [HttpGet("executions/{executionId}")]
        public async Task<ActionResult<WorkflowExecution>> GetWorkflowExecution(Guid executionId)
        {
            var execution = await _workflowEngineService.GetWorkflowExecutionAsync(executionId);
            if (execution == null)
            {
                return NotFound();
            }
            return Ok(execution);
        }
    }

    public class CreateWorkflowRequest
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}