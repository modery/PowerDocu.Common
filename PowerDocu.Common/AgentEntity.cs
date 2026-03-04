using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace PowerDocu.Common
{
    public class AgentEntity
    {
        public string SchemaName { get; set; }
        public int AuthenticationMode { get; set; }
        public int AuthenticationTrigger { get; set; }
        public string IconBase64 { get; set; }
        public int IsCustomizable { get; set; }
        public int Language { get; set; }
        public string Name { get; set; }
        public int RuntimeProvider { get; set; }
        public string SynchronizationStatus { get; set; }
        public string Template { get; set; }
        public int TimeZoneRuleVersionNumber { get; set; }
        public AgentConfiguration Configuration { get; set; }
        public List<BotComponent> BotComponents { get; set; } = new List<BotComponent>();

        public AgentEntity() { }
        public List<BotComponent> GetTopics()
        {
            return GetBotComponents(9, "topic");
        }

        public List<BotComponent> GetKnowledge()
        {
            //websites are stored as knowledge folders
            //Dataverse tables are stored differently.  dvtablesearchentities ?
            return GetBotComponents(16, "knowledge");
        }

        public List<BotComponent> GetTools()
        {
            // Include both component.* (connector-based tools) and action.* (flow-based actions) with type 9
            return BotComponents.Where(bc => bc.ComponentType == 9
                && (bc.SchemaName.StartsWith($"{SchemaName}.component.") || bc.SchemaName.StartsWith($"{SchemaName}.action."))
                && bc.GetTopicKind() == "TaskDialog").ToList();
        }

        public List<BotComponent> GetEntities()
        {
            return GetBotComponents(11, "entity");
        }

        public List<BotComponent> GetVariables()
        {
            // Include both component.* and GlobalVariableComponent.* with type 12
            return BotComponents.Where(bc => bc.ComponentType == 12
                && (bc.SchemaName.StartsWith($"{SchemaName}.component.") || bc.SchemaName.StartsWith($"{SchemaName}.GlobalVariableComponent."))).ToList();
        }

        public List<BotComponent> GetTriggers()
        {
            return BotComponents.Where(bc => bc.ComponentType == 17).ToList();
        }

        public List<BotComponent> GetFileKnowledge()
        {
            return BotComponents.Where(bc => bc.ComponentType == 14).ToList();
        }

        public List<BotComponent> GetGptDefault()
        {
            return GetBotComponents(15, "gpt");
        }

        private List<BotComponent> GetBotComponents(int type, string subType)
        {
            return BotComponents.Where(bc => bc.ComponentType == type && bc.SchemaName.StartsWith($"{SchemaName}.{subType}.")).ToList();
        }

        public string GetDescription()
        {
            return GetGptDefault().FirstOrDefault()?.Description;
        }

        public string GetInstructions()
        {
            var mapping = GetGptDefault().FirstOrDefault()?.GetYamlMappingNode();
            if (mapping != null && mapping.Children.TryGetValue(new YamlScalarNode("instructions"), out var node))
            {
                return ((YamlScalarNode)node).Value ?? string.Empty;
            }
            return string.Empty;
        }

        public Dictionary<string, string> GetSuggestedPrompts()
        {
            Dictionary<string, string> conversationStarters = new Dictionary<string, string>();
            var mapping = GetGptDefault().FirstOrDefault()?.GetYamlMappingNode();
            if (mapping != null && mapping.Children.TryGetValue(new YamlScalarNode("conversationStarters"), out var conversationsStartsNode) && conversationsStartsNode is YamlSequenceNode conversationsStartersSequence)
            {
                foreach (var conversationsStarter in conversationsStartersSequence)
                {
                    //var text = ((YamlScalarNode)action.Children[new YamlScalarNode("text")]).Value ?? string.Empty;
                    //var image = ((YamlScalarNode)action.Children[new YamlScalarNode("image")]).Value ?? string.Empty;
                    //conversationStarters[text] = image;
                    conversationStarters[conversationsStarter["title"].ToString()] = conversationsStarter["text"].ToString();
                }
            }
            return conversationStarters;
        }

        public string GetOrchestration()
        {
            return Configuration.settings.GenerativeActionsEnabled ? "Enabled" : "Disabled";
        }

        public string GetResponseModel()
        {
            try
            {
                var mapping = GetGptDefault().FirstOrDefault()?.GetYamlMappingNode();
                if (mapping != null &&
                    mapping.Children.TryGetValue(new YamlScalarNode("aISettings"), out var aiNode) &&
                    aiNode is YamlMappingNode aiMapping &&
                    aiMapping.Children.TryGetValue(new YamlScalarNode("model"), out var modelNode) &&
                    modelNode is YamlMappingNode modelMapping &&
                    modelMapping.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode))
                {
                    return kindNode.ToString();
                }
            }
            catch { }
            return "";
        }

        public string GetAuthenticationModeDisplayName()
        {
            return AuthenticationMode switch
            {
                0 => "No authentication",
                1 => "Only for Teams and Power Apps",
                2 => "Microsoft Entra ID",
                _ => $"Unknown ({AuthenticationMode})"
            };
        }

        public string GetAuthenticationTriggerDisplayName()
        {
            return AuthenticationTrigger switch
            {
                0 => "Always",
                1 => "As needed",
                _ => $"Unknown ({AuthenticationTrigger})"
            };
        }

        public string GetLanguageDisplayName()
        {
            return Language switch
            {
                1033 => "English (en-US)",
                1031 => "German (de-DE)",
                1036 => "French (fr-FR)",
                1034 => "Spanish (es-ES)",
                1040 => "Italian (it-IT)",
                1041 => "Japanese (ja-JP)",
                1042 => "Korean (ko-KR)",
                1046 => "Portuguese (pt-BR)",
                2052 => "Chinese Simplified (zh-CN)",
                1028 => "Chinese Traditional (zh-TW)",
                1043 => "Dutch (nl-NL)",
                1053 => "Swedish (sv-SE)",
                1044 => "Norwegian (nb-NO)",
                1030 => "Danish (da-DK)",
                1035 => "Finnish (fi-FI)",
                _ => $"LCID {Language}"
            };
        }

        public string GetRecognizerDisplayName()
        {
            var kind = Configuration?.recognizer?.kind;
            if (string.IsNullOrEmpty(kind)) return "Unknown";
            return kind.Replace("Recognizer", " Recognizer").Replace("AI ", "AI ");
        }

    }

    public class BotComponent
    {
        public string SchemaName { get; set; }
        public int ComponentType { get; set; }
        public int IsCustomizable { get; set; }
        public string Name { get; set; }
        public string ParentBotSchemaName { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
        public string YamlData { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Returns the top-level "kind" from the YAML data (e.g. AdaptiveDialog, KnowledgeSourceConfiguration).
        /// </summary>
        public string GetTopicKind()
        {
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode))
                {
                    return kindNode.ToString();
                }
            }
            catch { }
            return "Unknown";
        }

        /// <summary>
        /// Returns the modelDescription from the YAML data, if present.
        /// </summary>
        public string GetModelDescription()
        {
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("modelDescription"), out var node))
                {
                    return node.ToString();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Returns the startBehavior from the YAML data, if present (e.g. CancelOtherTopics).
        /// </summary>
        public string GetStartBehavior()
        {
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("startBehavior"), out var node))
                {
                    return node.ToString();
                }
            }
            catch { }
            return null;
        }

        public string GetTriggerTypeForTopic()
        {
            try
            {
                var mapping = GetYamlMappingNode();
                if (!mapping.Children.TryGetValue(new YamlScalarNode("beginDialog"), out var beginDialogNode)
                    || !(beginDialogNode is YamlMappingNode triggerYaml))
                {
                    // Non-AdaptiveDialog topics (e.g. KnowledgeSourceConfiguration)
                    return GetTopicKind();
                }
                return GetTriggerTypeDisplayName(triggerYaml.Children[new YamlScalarNode("kind")].ToString());
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Returns trigger queries defined in the topic, if any.
        /// </summary>
        public List<string> GetTriggerQueries()
        {
            var queries = new List<string>();
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("beginDialog"), out var beginDialogNode)
                    && beginDialogNode is YamlMappingNode triggerYaml
                    && triggerYaml.Children.TryGetValue(new YamlScalarNode("intent"), out var intentNode)
                    && intentNode is YamlMappingNode intentMapping
                    && intentMapping.Children.TryGetValue(new YamlScalarNode("triggerQueries"), out var triggerQueryNode)
                    && triggerQueryNode is YamlSequenceNode triggerQuerySequence)
                {
                    foreach (var q in triggerQuerySequence)
                    {
                        queries.Add(q.ToString());
                    }
                }
            }
            catch { }
            return queries;
        }

        /// <summary>
        /// Returns a display name for the ComponentType.
        /// </summary>
        public string GetComponentTypeDisplayName()
        {
            switch (ComponentType)
            {
                case 9: return "Topic";
                case 11: return "Entity";
                case 12: return "Variable";
                case 14: return "File Knowledge";
                case 15: return "GPT";
                case 16: return "Knowledge";
                case 17: return "Trigger";
                default: return $"Type {ComponentType}";
            }
        }

        /// <summary>
        /// Extracts variables used in this topic from YAML actions (SetVariable, output bindings, input bindings).
        /// Returns a list of (VariableName, Context) tuples.
        /// </summary>
        public List<(string Variable, string Context)> GetTopicVariables()
        {
            var variables = new List<(string Variable, string Context)>();
            try
            {
                var mapping = GetYamlMappingNode();
                CollectVariablesFromNode(mapping, variables);
            }
            catch { }
            // Deduplicate and sort by variable name
            return variables.GroupBy(v => v.Variable + "|" + v.Context).Select(g => g.First()).OrderBy(v => v.Variable).ToList();
        }

        private void CollectVariablesFromNode(YamlNode node, List<(string Variable, string Context)> variables)
        {
            if (node is YamlMappingNode mappingNode)
            {
                // Check for SetVariable action
                if (mappingNode.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode)
                    && kindNode.ToString() == "SetVariable"
                    && mappingNode.Children.TryGetValue(new YamlScalarNode("variable"), out var varNode))
                {
                    string displayName = "";
                    if (mappingNode.Children.TryGetValue(new YamlScalarNode("displayName"), out var dnNode))
                        displayName = dnNode.ToString();
                    variables.Add((varNode.ToString(), $"SetVariable{(string.IsNullOrEmpty(displayName) ? "" : $" ({displayName})")}" ));
                }

                // Check for output.binding
                if (mappingNode.Children.TryGetValue(new YamlScalarNode("output"), out var outputNode)
                    && outputNode is YamlMappingNode outputMapping
                    && outputMapping.Children.TryGetValue(new YamlScalarNode("binding"), out var bindingNode)
                    && bindingNode is YamlMappingNode bindingMapping)
                {
                    string actionDisplayName = "";
                    if (mappingNode.Children.TryGetValue(new YamlScalarNode("displayName"), out var adnNode))
                        actionDisplayName = adnNode.ToString();
                    foreach (var kvp in bindingMapping.Children)
                    {
                        string bindingValue = kvp.Value.ToString();
                        // Skip formula expressions (e.g. =If(...), =Text(...)); only include variable references
                        if (!bindingValue.StartsWith("="))
                        {
                            variables.Add((bindingValue, $"Output binding{(string.IsNullOrEmpty(actionDisplayName) ? "" : $" ({actionDisplayName})")}" ));
                        }
                    }
                }

                // Check for input.binding
                if (mappingNode.Children.TryGetValue(new YamlScalarNode("input"), out var inputNode)
                    && inputNode is YamlMappingNode inputMapping
                    && inputMapping.Children.TryGetValue(new YamlScalarNode("binding"), out var inputBindingNode)
                    && inputBindingNode is YamlMappingNode inputBindingMapping)
                {
                    string actionDisplayName = "";
                    if (mappingNode.Children.TryGetValue(new YamlScalarNode("displayName"), out var adnNode))
                        actionDisplayName = adnNode.ToString();
                    foreach (var kvp in inputBindingMapping.Children)
                    {
                        string bindingValue = kvp.Value.ToString();
                        // Skip formula expressions (e.g. =If(...), =Text(...)); only include variable references
                        if (!bindingValue.StartsWith("="))
                        {
                            variables.Add((bindingValue, $"Input binding{(string.IsNullOrEmpty(actionDisplayName) ? "" : $" ({actionDisplayName})")}" ));
                        }
                    }
                }

                // Check for Question variable
                if (mappingNode.Children.TryGetValue(new YamlScalarNode("kind"), out var qKindNode)
                    && qKindNode.ToString() == "Question"
                    && mappingNode.Children.TryGetValue(new YamlScalarNode("variable"), out var qVarNode))
                {
                    variables.Add((qVarNode.ToString(), "Question response"));
                }

                // Recurse into all children
                foreach (var child in mappingNode.Children)
                {
                    CollectVariablesFromNode(child.Value, variables);
                }
            }
            else if (node is YamlSequenceNode sequenceNode)
            {
                foreach (var item in sequenceNode)
                {
                    CollectVariablesFromNode(item, variables);
                }
            }
        }

        /// <summary>
        /// Returns knowledge source details for KnowledgeSourceConfiguration topics.
        /// </summary>
        public (string SourceKind, string SkillConfiguration) GetKnowledgeSourceDetails()
        {
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("source"), out var sourceNode)
                    && sourceNode is YamlMappingNode sourceMapping)
                {
                    string sourceKind = "";
                    string skillConfig = "";
                    if (sourceMapping.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode))
                        sourceKind = kindNode.ToString();
                    if (sourceMapping.Children.TryGetValue(new YamlScalarNode("skillConfiguration"), out var skillNode))
                        skillConfig = skillNode.ToString();
                    return (sourceKind, skillConfig);
                }
            }
            catch { }
            return (null, null);
        }

        public string getTopicFileName()
        {
            return SchemaName.Contains('.') ? SchemaName.Substring(SchemaName.LastIndexOf('.') + 1) : SchemaName;
        }

        /// <summary>
        /// Returns the knowledge source site URL (for SharePoint and Public Site sources).
        /// </summary>
        public string GetKnowledgeSourceSite()
        {
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("source"), out var sourceNode)
                    && sourceNode is YamlMappingNode sourceMapping
                    && sourceMapping.Children.TryGetValue(new YamlScalarNode("site"), out var siteNode))
                {
                    return siteNode.ToString();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Returns entity items for ClosedListEntity kinds.
        /// </summary>
        public List<(string Id, string DisplayName)> GetEntityItems()
        {
            var items = new List<(string Id, string DisplayName)>();
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("items"), out var itemsNode)
                    && itemsNode is YamlSequenceNode itemsSequence)
                {
                    foreach (var item in itemsSequence)
                    {
                        if (item is YamlMappingNode itemMapping)
                        {
                            string id = itemMapping.Children.TryGetValue(new YamlScalarNode("id"), out var idNode) ? idNode.ToString() : "";
                            string displayName = itemMapping.Children.TryGetValue(new YamlScalarNode("displayName"), out var dnNode) ? dnNode.ToString() : "";
                            items.Add((id, displayName));
                        }
                    }
                }
            }
            catch { }
            return items;
        }

        /// <summary>
        /// Returns the regex pattern for RegexEntity kinds.
        /// </summary>
        public string GetEntityPattern()
        {
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("pattern"), out var patternNode))
                {
                    return patternNode.ToString();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Returns variable details from the YAML data (scope, AI visibility, data type, etc.).
        /// </summary>
        public (string Scope, string AIVisibility, string DataType, bool IsExternalInitAllowed) GetVariableDetails()
        {
            string scope = "", aiVisibility = "", dataType = "";
            bool isExternalInit = false;
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("scope"), out var scopeNode))
                    scope = scopeNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("aIVisibility"), out var aiNode))
                    aiVisibility = aiNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("displayNameForDataType"), out var dtNode))
                    dataType = dtNode.ToString();
                else if (mapping.Children.TryGetValue(new YamlScalarNode("dataType"), out var dataTypeNode))
                {
                    if (dataTypeNode is YamlMappingNode dtMapping && dtMapping.Children.TryGetValue(new YamlScalarNode("$kind"), out var kindNode))
                        dataType = kindNode.ToString();
                    else
                        dataType = dataTypeNode.ToString();
                }
                if (mapping.Children.TryGetValue(new YamlScalarNode("isExternalInitializationAllowed"), out var extInitNode))
                    bool.TryParse(extInitNode.ToString(), out isExternalInit);
            }
            catch { }
            return (scope, aiVisibility, dataType, isExternalInit);
        }

        /// <summary>
        /// Returns tool/action details from TaskDialog YAML data.
        /// </summary>
        public (string ActionKind, string ConnectionReference, string OperationId, string FlowId, string ModelDisplayName, List<string> Inputs, List<string> Outputs) GetToolDetails()
        {
            string actionKind = "", connectionRef = "", operationId = "", flowId = "", modelDisplayName = "";
            var inputs = new List<string>();
            var outputs = new List<string>();
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("modelDisplayName"), out var mdnNode))
                    modelDisplayName = mdnNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("action"), out var actionNode) && actionNode is YamlMappingNode actionMapping)
                {
                    if (actionMapping.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode))
                        actionKind = kindNode.ToString();
                    if (actionMapping.Children.TryGetValue(new YamlScalarNode("connectionReference"), out var crNode))
                        connectionRef = crNode.ToString();
                    if (actionMapping.Children.TryGetValue(new YamlScalarNode("operationId"), out var opNode))
                        operationId = opNode.ToString();
                    if (actionMapping.Children.TryGetValue(new YamlScalarNode("flowId"), out var fiNode))
                        flowId = fiNode.ToString();
                }
                if (mapping.Children.TryGetValue(new YamlScalarNode("inputs"), out var inputsNode) && inputsNode is YamlSequenceNode inputsSequence)
                {
                    foreach (var input in inputsSequence)
                    {
                        if (input is YamlMappingNode inputMapping)
                        {
                            string propName = inputMapping.Children.TryGetValue(new YamlScalarNode("propertyName"), out var pnNode) ? pnNode.ToString() : "";
                            string entity = inputMapping.Children.TryGetValue(new YamlScalarNode("entity"), out var eNode) ? $" ({eNode})" : "";
                            inputs.Add(propName + entity);
                        }
                    }
                }
                if (mapping.Children.TryGetValue(new YamlScalarNode("outputs"), out var outputsNode) && outputsNode is YamlSequenceNode outputsSequence)
                {
                    foreach (var output in outputsSequence)
                    {
                        if (output is YamlMappingNode outputMapping)
                        {
                            string propName = outputMapping.Children.TryGetValue(new YamlScalarNode("propertyName"), out var pnNode) ? pnNode.ToString() : "";
                            outputs.Add(propName);
                        }
                    }
                }
            }
            catch { }
            return (actionKind, connectionRef, operationId, flowId, modelDisplayName, inputs, outputs);
        }

        /// <summary>
        /// Returns trigger details from ExternalTriggerConfiguration YAML data.
        /// </summary>
        public (string TriggerKind, string FlowId, string TriggerConnectionType) GetTriggerDetails()
        {
            string triggerKind = "", flowId = "", connectionType = "";
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("externalTriggerSource"), out var sourceNode) && sourceNode is YamlMappingNode sourceMapping)
                {
                    if (sourceMapping.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode))
                        triggerKind = kindNode.ToString();
                    if (sourceMapping.Children.TryGetValue(new YamlScalarNode("flowId"), out var fiNode))
                        flowId = fiNode.ToString();
                }
                if (mapping.Children.TryGetValue(new YamlScalarNode("extensionData"), out var extNode) && extNode is YamlMappingNode extMapping)
                {
                    if (extMapping.Children.TryGetValue(new YamlScalarNode("triggerConnectionType"), out var connNode))
                        connectionType = connNode.ToString();
                }
            }
            catch { }
            return (triggerKind, flowId, connectionType);
        }

        /// <summary>
        /// Returns the file data info for file-based knowledge components (type 14).
        /// The actual file reference is in botcomponent.xml filedata element.
        /// </summary>
        public string FileDataMimeType { get; set; }
        public string FileDataName { get; set; }

        public YamlMappingNode GetYamlMappingNode()
        {
            // Quote any unquoted YAML keys containing @ to prevent parser errors
            var sanitized = Regex.Replace(YamlData, @"(?m)^(\s*)([\w._-]*@[\w.@_-]*)(\s*:)", "$1\"$2\"$3");
            var input = new StringReader(sanitized);
            var yaml = new YamlStream();
            yaml.Load(input);
            return (YamlMappingNode)yaml.Documents[0].RootNode;
        }

        private string GetTriggerTypeDisplayName(string triggerType)
        {
            switch (triggerType)
            {
                case "OnRecognizedIntent":
                    return "By agent";
                case "OnEscalate":
                    return "On Talk to Representative";
                case "OnUnknownIntent":
                    return "Unknown topic";
                case "OnConversationStart":
                    return "On Conversation Start";
                case "OnSystemRedirect":
                    return "Redirect";
                case "OnRedirect":
                    return "Redirect";
                case "OnError":
                    return "On Error";
                case "OnSignIn":
                    return "On Sign In";
                case "OnSelectIntent":
                    return "Selection";
                default:
                    return triggerType;
            }
        }
    }

    public class AgentConfiguration
    {
        public string kind { get; set; }
        public Settings settings { get; set; }
        public GPTSettings gPTSettings { get; set; }
        public AISettings aISettings { get; set; }
        public Recognizer recognizer { get; set; }

        public class Settings
        {
            public bool GenerativeActionsEnabled { get; set; }
        }

        public class GPTSettings
        {
            public string kind { get; set; }
            public string defaultSchemaName { get; set; }
        }

        public class AISettings
        {
            public string kind { get; set; }
            public bool useModelKnowledge { get; set; }
            public bool isFileAnalysisEnabled { get; set; }
            public bool isSemanticSearchEnabled { get; set; }
            public string contentModeration { get; set; }
            public bool optInUseLatestModels { get; set; }
        }

        public class Recognizer
        {
            public string kind { get; set; }
        }
    }

}
