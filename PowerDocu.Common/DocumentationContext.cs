using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerDocu.Common
{
    /// <summary>
    /// Central registry holding all parsed entities from a solution or standalone app.
    /// Provides cross-reference resolvers so any documenter can look up related components.
    /// </summary>
    public class DocumentationContext
    {
        public SolutionEntity Solution { get; set; }
        public CustomizationsEntity Customizations { get; set; }
        public List<FlowEntity> Flows { get; set; } = new List<FlowEntity>();
        public List<AppEntity> Apps { get; set; } = new List<AppEntity>();
        public List<AgentEntity> Agents { get; set; } = new List<AgentEntity>();
        public List<AppModuleEntity> AppModules { get; set; } = new List<AppModuleEntity>();
        public List<TableEntity> Tables { get; set; } = new List<TableEntity>();
        public List<RoleEntity> Roles { get; set; } = new List<RoleEntity>();
        public ConfigHelper Config { get; set; }
        public string OutputPath { get; set; }
        public bool FullDocumentation { get; set; }

        /// <summary>
        /// Resolves a flow ID (GUID) to the flow's display name.
        /// Tries the parsed FlowEntity list first, then falls back to customizations.xml.
        /// </summary>
        public string GetFlowNameById(string flowId)
        {
            if (string.IsNullOrEmpty(flowId)) return flowId;
            string normalizedId = flowId.Trim('{', '}');

            // Try parsed flows first (most reliable - has the actual flow name)
            FlowEntity flow = Flows?.FirstOrDefault(f =>
                f.ID != null && f.ID.Trim('{', '}').Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
            if (flow != null) return flow.Name;

            // Fall back to customizations.xml workflow name
            if (Customizations != null)
            {
                string name = Customizations.getFlowNameById(flowId);
                if (!string.IsNullOrEmpty(name)) return name;
            }

            return null;
        }

        /// <summary>
        /// Finds the parsed FlowEntity by its ID.
        /// </summary>
        public FlowEntity GetFlowById(string flowId)
        {
            if (string.IsNullOrEmpty(flowId)) return null;
            string normalizedId = flowId.Trim('{', '}');
            return Flows?.FirstOrDefault(f =>
                f.ID != null && f.ID.Trim('{', '}').Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Resolves a canvas app schema name to its display name.
        /// </summary>
        public string GetAppNameBySchemaName(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName)) return schemaName;

            if (Customizations != null)
            {
                string resolved = Customizations.getAppNameBySchemaName(schemaName);
                if (!string.IsNullOrEmpty(resolved))
                {
                    AppEntity app = Apps?.FirstOrDefault(a => a.Name.Equals(resolved, StringComparison.OrdinalIgnoreCase));
                    return app != null ? app.Name : resolved;
                }
            }
            return schemaName;
        }

        /// <summary>
        /// Finds a parsed AppEntity by name.
        /// </summary>
        public AppEntity GetAppByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Apps?.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Resolves a table schema name to its display name.
        /// </summary>
        public string GetTableDisplayName(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName)) return schemaName;
            TableEntity table = Tables?.FirstOrDefault(t => t.getName().Equals(schemaName, StringComparison.OrdinalIgnoreCase));
            return table?.getLocalizedName() ?? schemaName;
        }

        /// <summary>
        /// Resolves a security role GUID to its display name.
        /// </summary>
        public string GetRoleNameById(string roleId)
        {
            if (string.IsNullOrEmpty(roleId)) return roleId;
            RoleEntity role = Roles?.FirstOrDefault(r =>
                r.ID != null && r.ID.Trim('{', '}').Equals(roleId.Trim('{', '}'), StringComparison.OrdinalIgnoreCase));
            if (role?.Name != null) return role.Name;
            return SecurityRoles.GetDisplayName(roleId) ?? roleId;
        }

        /// <summary>
        /// Resolves a view (saved query) GUID to its display name, parent table, and query type.
        /// </summary>
        public (string ViewName, string TableName, string QueryType) GetViewDetails(string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return (viewId, "", "");
            string normalizedId = viewId.Trim('{', '}');
            if (Tables != null)
            {
                foreach (var table in Tables)
                {
                    foreach (var view in table.GetViews())
                    {
                        if (view.GetViewId().Trim('{', '}').Equals(normalizedId, StringComparison.OrdinalIgnoreCase))
                        {
                            string viewName = view.GetViewName();
                            string tableName = table.getLocalizedName() ?? table.getName();
                            string queryType = view.GetQueryTypeDisplayName();
                            return (string.IsNullOrEmpty(viewName) ? viewId : viewName, tableName, queryType);
                        }
                    }
                }
            }
            return (viewId, "", "");
        }

        /// <summary>
        /// Resolves an AI Model GUID to its display name.
        /// </summary>
        public string GetAIModelNameById(string aiModelId)
        {
            if (string.IsNullOrEmpty(aiModelId)) return null;
            string normalizedId = aiModelId.Trim('{', '}');
            var aiModels = Customizations?.getAIModels();
            if (aiModels != null)
            {
                var model = aiModels.FirstOrDefault(m =>
                    m.getID().Trim('{', '}').Equals(normalizedId, StringComparison.OrdinalIgnoreCase));
                if (model != null)
                {
                    string name = model.getLocalizedName();
                    if (string.IsNullOrEmpty(name)) name = model.getName();
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            return null;
        }
    }
}
