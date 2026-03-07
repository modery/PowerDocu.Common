using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PowerDocu.Common
{
    /// <summary>
    /// Extracts key information from a Dataverse TableDefinition Expression into a structured format
    /// for cleaner rendering in documentation.
    /// </summary>
    public class TableDefinitionInfo
    {
        public string TableName { get; set; }
        public string LogicalName { get; set; }
        public string EntitySetName { get; set; }
        public string DisplayName { get; set; }
        public string PrimaryIdAttribute { get; set; }
        public string PrimaryNameAttribute { get; set; }
        public string ObjectTypeCode { get; set; }
        public string OwnershipType { get; set; }
        public string IsManaged { get; set; }
        public string IsActivity { get; set; }
        public string HasNotes { get; set; }
        public string IsIntersect { get; set; }
        public string IsPrivate { get; set; }
        public string IsLogicalEntity { get; set; }
        public List<PrivilegeInfo> Privileges { get; set; } = new List<PrivilegeInfo>();
    }

    public class PrivilegeInfo
    {
        public string Name { get; set; }
        public string PrivilegeType { get; set; }
    }

    public static class TableDefinitionHelper
    {
        /// <summary>
        /// Parses a TableDefinition Expression and extracts key metadata.
        /// The TableDefinition value can be either:
        /// - A raw JSON string in expressionOperands[0]
        /// - A nested Expression tree (if the source was a JObject)
        /// Returns null if the expression cannot be parsed.
        /// </summary>
        public static TableDefinitionInfo Parse(Expression tableDefinition)
        {
            if (tableDefinition == null || tableDefinition.expressionOperator != "TableDefinition")
                return null;

            // Try raw JSON string first (most common case for Dataverse tables)
            if (tableDefinition.expressionOperands.Count > 0 && tableDefinition.expressionOperands[0] is string jsonString)
            {
                return ParseFromJson(jsonString);
            }

            // Fall back to Expression tree traversal
            return ParseFromExpressionTree(tableDefinition);
        }

        private static TableDefinitionInfo ParseFromJson(string jsonString)
        {
            JObject json;
            try
            {
                json = JObject.Parse(jsonString);
            }
            catch
            {
                return null;
            }

            var info = new TableDefinitionInfo();
            info.TableName = json.Value<string>("TableName");

            JObject entityMetadata = null;
            JToken emToken = json["EntityMetadata"];
            if (emToken is JObject emObj)
            {
                entityMetadata = emObj;
            }
            else if (emToken != null)
            {
                // EntityMetadata may be a JSON string that needs to be parsed
                string emString = emToken.ToString();
                try { entityMetadata = JObject.Parse(emString); } catch { }
            }

            if (entityMetadata != null)
            {
                info.LogicalName = entityMetadata.Value<string>("LogicalName");
                info.EntitySetName = entityMetadata.Value<string>("EntitySetName");
                info.PrimaryIdAttribute = entityMetadata.Value<string>("PrimaryIdAttribute");
                info.PrimaryNameAttribute = entityMetadata.Value<string>("PrimaryNameAttribute");
                info.ObjectTypeCode = entityMetadata["ObjectTypeCode"]?.ToString();
                info.OwnershipType = MapOwnershipType(entityMetadata.Value<string>("OwnershipType"));
                info.IsManaged = entityMetadata["IsManaged"]?.ToString();
                info.IsActivity = entityMetadata["IsActivity"]?.ToString();
                info.HasNotes = entityMetadata["HasNotes"]?.ToString();
                info.IsIntersect = entityMetadata["IsIntersect"]?.ToString();
                info.IsPrivate = entityMetadata["IsPrivate"]?.ToString();
                info.IsLogicalEntity = entityMetadata["IsLogicalEntity"]?.ToString();

                // Extract display name from DisplayCollectionName -> LocalizedLabels
                JObject displayCollectionName = entityMetadata.Value<JObject>("DisplayCollectionName");
                if (displayCollectionName != null)
                {
                    JArray localizedLabels = displayCollectionName.Value<JArray>("LocalizedLabels");
                    if (localizedLabels != null && localizedLabels.Count > 0)
                    {
                        info.DisplayName = localizedLabels[0]?.Value<string>("Label");
                    }
                }

                // Extract privileges
                JArray privileges = entityMetadata.Value<JArray>("Privileges");
                if (privileges != null)
                {
                    foreach (JToken priv in privileges)
                    {
                        string name = priv.Value<string>("Name");
                        string type = priv.Value<string>("PrivilegeType");
                        if (name != null)
                        {
                            info.Privileges.Add(new PrivilegeInfo { Name = name, PrivilegeType = type });
                        }
                    }
                }
            }

            return info;
        }

        private static TableDefinitionInfo ParseFromExpressionTree(Expression tableDefinition)
        {
            var info = new TableDefinitionInfo();

            info.TableName = FindStringValue(tableDefinition, "TableName");

            Expression entityMetadata = FindChildExpression(tableDefinition, "EntityMetadata");
            if (entityMetadata != null)
            {
                info.LogicalName = FindStringValue(entityMetadata, "LogicalName");
                info.EntitySetName = FindStringValue(entityMetadata, "EntitySetName");
                info.PrimaryIdAttribute = FindStringValue(entityMetadata, "PrimaryIdAttribute");
                info.PrimaryNameAttribute = FindStringValue(entityMetadata, "PrimaryNameAttribute");
                info.ObjectTypeCode = FindStringValue(entityMetadata, "ObjectTypeCode");
                info.OwnershipType = MapOwnershipType(FindStringValue(entityMetadata, "OwnershipType"));
                info.IsManaged = FindStringValue(entityMetadata, "IsManaged");
                info.IsActivity = FindStringValue(entityMetadata, "IsActivity");
                info.HasNotes = FindStringValue(entityMetadata, "HasNotes");
                info.IsIntersect = FindStringValue(entityMetadata, "IsIntersect");
                info.IsPrivate = FindStringValue(entityMetadata, "IsPrivate");
                info.IsLogicalEntity = FindStringValue(entityMetadata, "IsLogicalEntity");
                info.DisplayName = ExtractDisplayNameFromExpression(entityMetadata);
                info.Privileges = ExtractPrivilegesFromExpression(entityMetadata);
            }

            return info;
        }

        private static string FindStringValue(Expression parent, string key)
        {
            foreach (object operand in parent.expressionOperands)
            {
                if (operand is Expression expr && expr.expressionOperator == key)
                {
                    if (expr.expressionOperands.Count > 0 && expr.expressionOperands[0] is string val)
                        return val;
                }
            }
            return null;
        }

        private static Expression FindChildExpression(Expression parent, string key)
        {
            foreach (object operand in parent.expressionOperands)
            {
                if (operand is Expression expr && expr.expressionOperator == key)
                    return expr;
            }
            return null;
        }

        private static string ExtractDisplayNameFromExpression(Expression entityMetadata)
        {
            Expression displayCollectionName = FindChildExpression(entityMetadata, "DisplayCollectionName");
            if (displayCollectionName == null) return null;

            Expression localizedLabels = FindChildExpression(displayCollectionName, "LocalizedLabels");
            if (localizedLabels == null) return null;

            foreach (object operand in localizedLabels.expressionOperands)
            {
                if (operand is List<object> labelList)
                {
                    foreach (object item in labelList)
                    {
                        if (item is List<object> innerList)
                        {
                            string label = null;
                            foreach (object inner in innerList)
                            {
                                if (inner is Expression expr && expr.expressionOperator == "Label" && expr.expressionOperands.Count > 0)
                                    label = expr.expressionOperands[0]?.ToString();
                            }
                            if (label != null) return label;
                        }
                        else if (item is Expression expr && expr.expressionOperator == "Label" && expr.expressionOperands.Count > 0)
                        {
                            return expr.expressionOperands[0]?.ToString();
                        }
                    }
                }
                else if (operand is Expression expr)
                {
                    Expression labelExpr = FindChildExpression(expr, "Label");
                    if (labelExpr != null && labelExpr.expressionOperands.Count > 0)
                        return labelExpr.expressionOperands[0]?.ToString();
                }
            }
            return null;
        }

        private static List<PrivilegeInfo> ExtractPrivilegesFromExpression(Expression entityMetadata)
        {
            var privileges = new List<PrivilegeInfo>();
            Expression privilegesExpr = FindChildExpression(entityMetadata, "Privileges");
            if (privilegesExpr == null) return privileges;

            // Privileges is typically an array of objects, each with Name, PrivilegeType, etc.
            foreach (object operand in privilegesExpr.expressionOperands)
            {
                if (operand is List<object> privList)
                {
                    foreach (object item in privList)
                    {
                        if (item is List<object> innerList)
                        {
                            var priv = ExtractPrivilegeFromList(innerList);
                            if (priv != null) privileges.Add(priv);
                        }
                    }
                }
                else if (operand is Expression expr)
                {
                    string name = FindStringValue(expr, "Name");
                    string type = FindStringValue(expr, "PrivilegeType");
                    if (name != null)
                        privileges.Add(new PrivilegeInfo { Name = name, PrivilegeType = type });
                }
            }
            return privileges;
        }

        private static PrivilegeInfo ExtractPrivilegeFromList(List<object> list)
        {
            string name = null;
            string type = null;
            foreach (object item in list)
            {
                if (item is Expression expr)
                {
                    if (expr.expressionOperator == "Name" && expr.expressionOperands.Count > 0)
                        name = expr.expressionOperands[0]?.ToString();
                    if (expr.expressionOperator == "PrivilegeType" && expr.expressionOperands.Count > 0)
                        type = expr.expressionOperands[0]?.ToString();
                }
            }
            if (name != null)
                return new PrivilegeInfo { Name = name, PrivilegeType = type };
            return null;
        }

        /// <summary>
        /// Returns a list of key-value pairs for the main table definition properties (excluding privileges).
        /// </summary>
        public static List<KeyValuePair<string, string>> GetSummaryProperties(TableDefinitionInfo info)
        {
            var props = new List<KeyValuePair<string, string>>();
            if (info == null) return props;

            AddIfNotNull(props, "Table Name", info.TableName);
            AddIfNotNull(props, "Logical Name", info.LogicalName);
            AddIfNotNull(props, "Entity Set Name", info.EntitySetName);
            AddIfNotNull(props, "Display Name", info.DisplayName);
            AddIfNotNull(props, "Primary ID Attribute", info.PrimaryIdAttribute);
            AddIfNotNull(props, "Primary Name Attribute", info.PrimaryNameAttribute);
            AddIfNotNull(props, "Ownership Type", info.OwnershipType);

            return props;
        }

        public static string MapOwnershipType(string ownershipType)
        {
            return ownershipType switch
            {
                "UserOwned" => "User or Team",
                "OrgOwned" => "Organization",
                _ => ownershipType
            };
        }

        private static void AddIfNotNull(List<KeyValuePair<string, string>> list, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
                list.Add(new KeyValuePair<string, string>(key, value));
        }
    }
}
