using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BettsTax.Data;
using BettsTax.Core.DTOs;
using System.ComponentModel.DataAnnotations;

namespace BettsTax.Core.Services.WorkflowEngine
{
    /// <summary>
    /// Service for dynamic entity schema discovery and field mapping
    /// Provides metadata about entities for visual workflow builder
    /// </summary>
    public class EntitySchemaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EntitySchemaService> _logger;
        private readonly Dictionary<string, EntitySchema> _schemaCache;

        public EntitySchemaService(
            ApplicationDbContext context,
            ILogger<EntitySchemaService> logger)
        {
            _context = context;
            _logger = logger;
            _schemaCache = new Dictionary<string, EntitySchema>();
        }

        /// <summary>
        /// Gets all entity schemas available in the system
        /// </summary>
        public async Task<List<EntitySchema>> GetAllEntitySchemasAsync()
        {
            try
            {
                var schemas = new List<EntitySchema>();

                // Get all entity types from DbContext
                var entityTypes = _context.Model.GetEntityTypes();

                foreach (var entityType in entityTypes)
                {
                    var schema = await BuildEntitySchemaAsync(entityType.ClrType);
                    if (schema != null)
                    {
                        schemas.Add(schema);
                    }
                }

                // Add commonly used entities manually if not in DbContext
                await AddCommonEntitySchemasAsync(schemas);

                // Cache the results
                foreach (var schema in schemas)
                {
                    _schemaCache[schema.Name] = schema;
                }

                _logger.LogInformation("Retrieved {SchemaCount} entity schemas", schemas.Count);
                return schemas.OrderBy(s => s.DisplayName).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity schemas");
                return new List<EntitySchema>();
            }
        }

        /// <summary>
        /// Gets entity schema by name
        /// </summary>
        public async Task<EntitySchema?> GetEntitySchemaAsync(string entityName)
        {
            try
            {
                // Check cache first
                if (_schemaCache.ContainsKey(entityName))
                {
                    return _schemaCache[entityName];
                }

                // Find entity type
                var entityType = _context.Model.GetEntityTypes()
                    .FirstOrDefault(et => et.ClrType.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

                if (entityType != null)
                {
                    var schema = await BuildEntitySchemaAsync(entityType.ClrType);
                    if (schema != null)
                    {
                        _schemaCache[entityName] = schema;
                        return schema;
                    }
                }

                _logger.LogWarning("Entity schema not found: {EntityName}", entityName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity schema for {EntityName}", entityName);
                return null;
            }
        }

        /// <summary>
        /// Gets fields available for a specific entity
        /// </summary>
        public async Task<List<EntityField>> GetEntityFieldsAsync(string entityName)
        {
            var schema = await GetEntitySchemaAsync(entityName);
            return schema?.Fields ?? new List<EntityField>();
        }

        /// <summary>
        /// Gets fields that can be used in workflow conditions
        /// </summary>
        public async Task<List<EntityField>> GetFilterableFieldsAsync(string entityName)
        {
            var fields = await GetEntityFieldsAsync(entityName);
            
            // Return fields that are suitable for filtering/conditions
            return fields.Where(f => IsFieldFilterable(f)).ToList();
        }

        /// <summary>
        /// Gets fields that can be used in workflow outputs
        /// </summary>
        public async Task<List<EntityField>> GetOutputFieldsAsync(string entityName)
        {
            var fields = await GetEntityFieldsAsync(entityName);
            
            // Return fields that can be used in outputs (excluding sensitive fields)
            return fields.Where(f => !IsSensitiveField(f)).ToList();
        }

        /// <summary>
        /// Builds entity schema from CLR type
        /// </summary>
        private Task<EntitySchema?> BuildEntitySchemaAsync(Type entityType)
        {
            try
            {
                var schema = new EntitySchema
                {
                    Name = entityType.Name,
                    DisplayName = GetDisplayName(entityType),
                    Description = GetEntityDescription(entityType),
                    Fields = new List<EntityField>(),
                    Relationships = new List<string>()
                };

                // Get properties
                var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    var field = BuildEntityField(property);
                    if (field != null)
                    {
                        schema.Fields.Add(field);
                    }

                    // Check for relationships
                    if (IsNavigationProperty(property))
                    {
                        var relationshipName = property.PropertyType.Name;
                        if (property.PropertyType.IsGenericType)
                        {
                            relationshipName = property.PropertyType.GetGenericArguments().FirstOrDefault()?.Name ?? relationshipName;
                        }
                        
                        if (!schema.Relationships.Contains(relationshipName))
                        {
                            schema.Relationships.Add(relationshipName);
                        }
                    }
                }

                return Task.FromResult<EntitySchema?>(schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building schema for entity type {EntityType}", entityType.Name);
                return Task.FromResult<EntitySchema?>(null);
            }
        }

        /// <summary>
        /// Builds entity field from property info
        /// </summary>
        private EntityField? BuildEntityField(PropertyInfo property)
        {
            try
            {
                // Skip navigation properties for field list
                if (IsNavigationProperty(property))
                {
                    return null;
                }

                var field = new EntityField
                {
                    Name = property.Name,
                    DisplayName = GetDisplayName(property),
                    Type = GetFieldType(property),
                    IsRequired = IsRequired(property),
                    IsReadOnly = !property.CanWrite,
                    Description = GetFieldDescription(property)
                };

                return field;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building field for property {PropertyName}", property.Name);
                return null;
            }
        }

        /// <summary>
        /// Gets display name for type or property
        /// </summary>
        private string GetDisplayName(MemberInfo member)
        {
            // Check for Display attribute
            var displayAttribute = member.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
            if (displayAttribute?.Name != null)
            {
                return displayAttribute.Name;
            }

            // Convert PascalCase to readable name
            var name = member.Name;
            return System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " ").Trim();
        }

        /// <summary>
        /// Gets entity description from attributes or conventions
        /// </summary>
        private string GetEntityDescription(Type entityType)
        {
            var descriptionAttribute = entityType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                return descriptionAttribute.Description;
            }

            // Generate description based on entity name
            var name = GetDisplayName(entityType);
            return $"Represents {name.ToLower()} data in the system";
        }

        /// <summary>
        /// Gets field description from attributes
        /// </summary>
        private string? GetFieldDescription(PropertyInfo property)
        {
            var descriptionAttribute = property.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
            return descriptionAttribute?.Description;
        }

        /// <summary>
        /// Gets field type string for workflow system
        /// </summary>
        private string GetFieldType(PropertyInfo property)
        {
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            return propertyType.Name.ToLower() switch
            {
                "string" => "string",
                "int32" or "int64" or "int16" => "integer",
                "decimal" or "double" or "float" => "number",
                "boolean" => "boolean",
                "datetime" => "datetime",
                "guid" => "guid",
                "timespan" => "timespan",
                _ when propertyType.IsEnum => "enum",
                _ => "object"
            };
        }

        /// <summary>
        /// Checks if property is required
        /// </summary>
        private bool IsRequired(PropertyInfo property)
        {
            return property.GetCustomAttribute<RequiredAttribute>() != null;
        }

        /// <summary>
        /// Checks if property is a navigation property
        /// </summary>
        private bool IsNavigationProperty(PropertyInfo property)
        {
            // Check if it's a collection
            if (property.PropertyType.IsGenericType && 
                (property.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                 property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>) ||
                 property.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                return true;
            }

            // Check if it's a reference to another entity
            var propertyType = property.PropertyType;
            return propertyType.IsClass && 
                   propertyType != typeof(string) && 
                   !propertyType.IsPrimitive && 
                   !propertyType.IsValueType;
        }

        /// <summary>
        /// Checks if field is suitable for filtering
        /// </summary>
        private bool IsFieldFilterable(EntityField field)
        {
            return field.Type switch
            {
                "string" or "integer" or "number" or "boolean" or "datetime" or "enum" => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if field contains sensitive data
        /// </summary>
        private bool IsSensitiveField(EntityField field)
        {
            var sensitiveFieldNames = new[]
            {
                "password", "secret", "key", "token", "hash", "salt",
                "ssn", "socialsecuritynumber", "creditcard", "bankaccount"
            };

            return sensitiveFieldNames.Any(name => 
                field.Name.ToLower().Contains(name) || 
                field.DisplayName.ToLower().Contains(name));
        }

        /// <summary>
        /// Adds common entity schemas that might not be in DbContext
        /// </summary>
        private Task AddCommonEntitySchemasAsync(List<EntitySchema> schemas)
        {
            // Add system entities
            var systemSchema = new EntitySchema
            {
                Name = "System",
                DisplayName = "System",
                Description = "System-level data and metadata",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "CurrentUser", DisplayName = "Current User", Type = "string", Description = "Currently logged-in user" },
                    new EntityField { Name = "CurrentDateTime", DisplayName = "Current Date/Time", Type = "datetime", Description = "Current system date and time" },
                    new EntityField { Name = "TenantId", DisplayName = "Tenant ID", Type = "guid", Description = "Current tenant identifier" }
                },
                Relationships = new List<string>()
            };

            if (!schemas.Any(s => s.Name == "System"))
            {
                schemas.Add(systemSchema);
            }

            // Add request context schema
            var contextSchema = new EntitySchema
            {
                Name = "Context",
                DisplayName = "Request Context",
                Description = "Context information for the current request",
                Fields = new List<EntityField>
                {
                    new EntityField { Name = "UserAgent", DisplayName = "User Agent", Type = "string", Description = "Client user agent" },
                    new EntityField { Name = "IpAddress", DisplayName = "IP Address", Type = "string", Description = "Client IP address" },
                    new EntityField { Name = "RequestPath", DisplayName = "Request Path", Type = "string", Description = "Current request path" }
                },
                Relationships = new List<string>()
            };

            if (!schemas.Any(s => s.Name == "Context"))
            {
                schemas.Add(contextSchema);
            }

            return Task.CompletedTask;
        }
    }
}