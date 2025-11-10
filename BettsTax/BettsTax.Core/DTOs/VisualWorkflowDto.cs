using System;
using System.Collections.Generic;

namespace BettsTax.Core.DTOs
{
    /// <summary>
    /// Visual workflow definition containing drag-and-drop components
    /// </summary>
    public class VisualWorkflowDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<WorkflowNode> Nodes { get; set; } = new();
        public List<WorkflowConnection> Connections { get; set; } = new();
        public WorkflowCanvas Canvas { get; set; } = new();
        public WorkflowMetadata Metadata { get; set; } = new();
        public VisualWorkflowSettings Settings { get; set; } = new();
    }

    /// <summary>
    /// Visual workflow node representing a single operation
    /// </summary>
    public class WorkflowNode
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public NodePosition Position { get; set; } = new();
        public NodeSize Size { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
        public NodeStyle Style { get; set; } = new();
        public bool IsSelected { get; set; }
        public bool IsDisabled { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Connection between workflow nodes
    /// </summary>
    public class WorkflowConnection
    {
        public string Id { get; set; } = string.Empty;
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public string? SourcePortId { get; set; }
        public string? TargetPortId { get; set; }
        public ConnectionStyle Style { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Workflow canvas configuration
    /// </summary>
    public class WorkflowCanvas
    {
        public CanvasViewport Viewport { get; set; } = new();
        public CanvasGrid Grid { get; set; } = new();
        public CanvasTheme Theme { get; set; } = new();
        public bool IsReadOnly { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    /// <summary>
    /// Workflow metadata and versioning
    /// </summary>
    public class WorkflowMetadata
    {
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public bool IsPublished { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> CustomMetadata { get; set; } = new();
    }

    /// <summary>
    /// Visual workflow settings and preferences
    /// </summary>
    public class VisualWorkflowSettings
    {
        public bool AutoSave { get; set; } = true;
        public bool ShowGrid { get; set; } = true;
        public bool SnapToGrid { get; set; } = true;
        public int GridSize { get; set; } = 20;
        public bool ShowMinimap { get; set; } = true;
        public Dictionary<string, object> UserPreferences { get; set; } = new();
    }

    /// <summary>
    /// Node position on canvas
    /// </summary>
    public class NodePosition
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    /// <summary>
    /// Node size dimensions
    /// </summary>
    public class NodeSize
    {
        public double Width { get; set; } = 200;
        public double Height { get; set; } = 100;
    }

    /// <summary>
    /// Node visual styling
    /// </summary>
    public class NodeStyle
    {
        public string BackgroundColor { get; set; } = "#ffffff";
        public string BorderColor { get; set; } = "#cccccc";
        public string TextColor { get; set; } = "#000000";
        public int BorderWidth { get; set; } = 1;
        public int BorderRadius { get; set; } = 4;
        public string FontFamily { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;
        public Dictionary<string, object> CustomStyles { get; set; } = new();
    }

    /// <summary>
    /// Connection visual styling
    /// </summary>
    public class ConnectionStyle
    {
        public string Color { get; set; } = "#666666";
        public int Width { get; set; } = 2;
        public string Style { get; set; } = "solid"; // solid, dashed, dotted
        public bool Animated { get; set; }
        public string ArrowType { get; set; } = "arrow";
    }

    /// <summary>
    /// Canvas viewport settings
    /// </summary>
    public class CanvasViewport
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Zoom { get; set; } = 1.0;
    }

    /// <summary>
    /// Canvas grid configuration
    /// </summary>
    public class CanvasGrid
    {
        public bool Enabled { get; set; } = true;
        public int Size { get; set; } = 20;
        public string Color { get; set; } = "#f0f0f0";
        public bool SnapToGrid { get; set; } = true;
    }

    /// <summary>
    /// Canvas theme settings
    /// </summary>
    public class CanvasTheme
    {
        public string Name { get; set; } = "light";
        public string BackgroundColor { get; set; } = "#ffffff";
        public Dictionary<string, string> Colors { get; set; } = new();
    }

    /// <summary>
    /// Available workflow node type with configuration
    /// </summary>
    public class WorkflowNodeType
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<NodePort> InputPorts { get; set; } = new();
        public List<NodePort> OutputPorts { get; set; } = new();
        public List<NodeProperty> Properties { get; set; } = new();
        public NodeStyle DefaultStyle { get; set; } = new();
        public bool IsCustom { get; set; }
    }

    /// <summary>
    /// Node connection port
    /// </summary>
    public class NodePort
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Node property configuration
    /// </summary>
    public class NodeProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // string, number, boolean, select, json, etc.
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<PropertyOption>? Options { get; set; }
        public PropertyValidation? Validation { get; set; }
    }

    /// <summary>
    /// Property option for select fields
    /// </summary>
    public class PropertyOption
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool Disabled { get; set; }
    }

    /// <summary>
    /// Property validation rules
    /// </summary>
    public class PropertyValidation
    {
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public string? Pattern { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request to create visual workflow
    /// </summary>
    public class CreateVisualWorkflowRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public List<WorkflowNode>? Nodes { get; set; }
        public List<WorkflowConnection>? Connections { get; set; }
        public WorkflowCanvas? Canvas { get; set; }
        public VisualWorkflowSettings? Settings { get; set; }
    }

    /// <summary>
    /// Executable workflow generated from visual definition
    /// </summary>
    public class ExecutableWorkflow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ExecutableWorkflowStep> Steps { get; set; } = new();
        public WorkflowMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Executable workflow step
    /// </summary>
    public class ExecutableWorkflowStep
    {
        public string Id { get; set; } = string.Empty;
        public ExecutableStepType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> NextSteps { get; set; } = new();
        public Dictionary<string, List<string>>? ConditionalNextSteps { get; set; }
    }

    /// <summary>
    /// Types of executable workflow steps
    /// </summary>
    public enum ExecutableStepType
    {
        ScheduleTrigger,
        EventTrigger,
        Condition,
        Notification,
        Webhook,
        DataTransform,
        Custom
    }

    /// <summary>
    /// Entity schema for dynamic field selection
    /// </summary>
    public class EntitySchema
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<EntityField> Fields { get; set; } = new();
        public List<string> Relationships { get; set; } = new();
    }

    /// <summary>
    /// Entity field definition
    /// </summary>
    public class EntityField
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Workflow test result
    /// </summary>
    public class WorkflowTestResult
    {
        public Guid WorkflowId { get; set; }
        public bool IsSuccessful { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public List<NodeExecutionResult> NodeResults { get; set; } = new();
        public Dictionary<string, object> OutputData { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Node execution result for testing
    /// </summary>
    public class NodeExecutionResult
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool IsSuccess { get; set; }
        public object? Output { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Workflow test execution context
    /// </summary>
    public class WorkflowTestContext
    {
        public Guid WorkflowId { get; set; }
        public Dictionary<string, object> TestData { get; set; } = new();
        public DateTime StartTime { get; set; }
        public List<NodeExecutionResult> NodeResults { get; set; } = new();
        public Dictionary<string, object> OutputData { get; set; } = new();
    }

    /// <summary>
    /// Unified template metadata class
    /// </summary>
    public class TemplateMetadata
    {
        public Guid? OriginalWorkflowId { get; set; }
        public string CreationContext { get; set; } = string.Empty;
        public string SourceVersion { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
        public List<string> Screenshots { get; set; } = new();
        public string? Documentation { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; } = new();
        public string Author { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> RequiredPermissions { get; set; } = new();
    }
}