using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
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
        public List<DvTableSearch> DvTableSearches { get; set; } = new List<DvTableSearch>();
        public List<DvTableSearchEntity> DvTableSearchEntities { get; set; } = new List<DvTableSearchEntity>();
        public List<CopilotSynonym> CopilotSynonyms { get; set; } = new List<CopilotSynonym>();
        public List<AIPluginEntity> AIPlugins { get; set; } = new List<AIPluginEntity>();
        public List<AIPluginOperationEntity> AIPluginOperations { get; set; } = new List<AIPluginOperationEntity>();
        public List<CustomApiEntity> CustomApis { get; set; } = new List<CustomApiEntity>();
        public List<AIModel> AIModels { get; set; } = new List<AIModel>();

        public AgentEntity() { }
        public List<BotComponent> GetTopics()
        {
            return GetBotComponents(9, "topic");
        }

        public List<BotComponent> GetKnowledge()
        {
            // Knowledge sources can use both "knowledge." and "topic." prefixes with type 16
            return BotComponents.Where(bc => bc.ComponentType == 16).ToList();
        }

        public List<BotComponent> GetTools()
        {
            // Include both component.* (connector-based tools) and action.* (flow-based actions) with type 9
            return BotComponents.Where(bc => bc.ComponentType == 9
                && (bc.SchemaName.StartsWith($"{SchemaName}.component.") || bc.SchemaName.StartsWith($"{SchemaName}.action."))
                && bc.GetTopicKind() == "TaskDialog").ToList();
        }

        /// <summary>
        /// Returns BotComponents that represent connected (child) agent invocations.
        /// These have SchemaName containing ".InvokeConnectedAgentTaskAction." and ComponentType 9.
        /// </summary>
        public List<BotComponent> GetConnectedAgents()
        {
            return BotComponents.Where(bc => bc.ComponentType == 9
                && bc.SchemaName.Contains(".InvokeConnectedAgentTaskAction.")).ToList();
        }

        /// <summary>
        /// Returns a list of connected agent info objects extracted from InvokeConnectedAgentTaskAction components.
        /// </summary>
        public List<ConnectedAgentInfo> GetAllConnectedAgentInfos()
        {
            var result = new List<ConnectedAgentInfo>();
            foreach (var component in GetConnectedAgents())
            {
                var details = component.GetConnectedAgentDetails();
                result.Add(new ConnectedAgentInfo
                {
                    Name = !string.IsNullOrEmpty(details.ModelDisplayName) ? details.ModelDisplayName : component.Name,
                    BotSchemaName = details.BotSchemaName,
                    Description = !string.IsNullOrEmpty(details.ModelDescription) ? details.ModelDescription : component.Description ?? "",
                    HistoryType = details.HistoryType,
                    ConnectionType = "Connected Agent"
                });
            }
            return result;
        }

        /// <summary>
        /// Returns a unified list of all agent tools, including flow/connector TaskDialog tools
        /// (from BotComponents) and prompt tools (from AIPlugins/AIModels).
        /// </summary>
        public List<AgentToolInfo> GetAllToolInfos()
        {
            var result = new List<AgentToolInfo>();

            // 1) Flow and connector tools from BotComponents (TaskDialog)
            foreach (var tool in GetTools())
            {
                var details = tool.GetToolDetails();
                string toolType = details.ActionKind switch
                {
                    "InvokeConnectorTaskAction" => "Connector",
                    "InvokeFlowTaskAction" => "Flow",
                    _ => details.ActionKind
                };
                string trigger = details.TriggerCondition == "false" ? "None" : "By agent";
                bool enabled = tool.StateCode == 0;
                string description = !string.IsNullOrEmpty(details.ModelDescription)
                    ? details.ModelDescription
                    : tool.Description ?? "";

                var info = new AgentToolInfo
                {
                    Name = !string.IsNullOrEmpty(details.ModelDisplayName) ? details.ModelDisplayName : tool.Name,
                    ToolType = toolType,
                    AvailableTo = Name,
                    Trigger = trigger,
                    Enabled = enabled,
                    Description = description,
                    ConnectionReference = details.ConnectionReference,
                    OperationId = details.OperationId,
                    FlowId = details.FlowId,
                    AgentFlowName = details.FlowId != null ? GetFlowNameForId(details.FlowId) : null,
                    ResponseActivity = details.ResponseActivity,
                    ResponseMode = details.ResponseMode,
                    OutputMode = details.OutputMode,
                    Inputs = details.Inputs,
                    Outputs = details.Outputs,
                    PromptText = null,
                    ModelParameters = null
                };
                result.Add(info);
            }

            // 2) Prompt tools from AIPlugins
            foreach (var plugin in AIPlugins)
            {
                // plugintype=0, pluginsubtype=4 indicates a prompt tool
                if (plugin.PluginType != 0) continue;

                // Find the linked AI model via the plugin operation
                var operation = AIPluginOperations.FirstOrDefault(op => op.AIPluginName == plugin.Name);
                string aiModelId = operation?.AIModelId;
                AIModel model = aiModelId != null
                    ? AIModels.FirstOrDefault(m => m.getID().Trim('{', '}').Equals(aiModelId.Trim('{', '}'), StringComparison.OrdinalIgnoreCase))
                    : null;

                // Find linked custom API for inputs/outputs
                string customApiUniqueName = operation?.CustomApiUniqueName;
                CustomApiEntity customApi = customApiUniqueName != null
                    ? CustomApis.FirstOrDefault(ca => ca.UniqueName == customApiUniqueName)
                    : null;

                var inputs = new List<ToolInputInfo>();
                var outputs = new List<ToolOutputInfo>();

                if (customApi != null)
                {
                    foreach (var param in customApi.RequestParameters.OrderBy(p => p.Name))
                    {
                        inputs.Add(new ToolInputInfo
                        {
                            Name = param.DisplayName,
                            Description = param.Description,
                            DataType = GetCustomApiTypeDisplayName(param.Type),
                            IsRequired = !param.IsOptional,
                            FillUsing = "Dynamically fill with AI"
                        });
                    }
                    foreach (var prop in customApi.ResponseProperties.OrderBy(p => p.Name))
                    {
                        outputs.Add(new ToolOutputInfo
                        {
                            Name = prop.DisplayName,
                            Description = prop.Description,
                            DataType = GetCustomApiTypeDisplayName(prop.Type)
                        });
                    }
                }

                // Extract prompt text and parameters from AI model
                string promptText = null;
                string modelParams = null;
                List<ToolInputInfo> modelInputs = null;
                try
                {
                    if (model != null)
                    {
                        promptText = model.getPrompt();
                        var modelInputList = model.getInputs();
                        if (modelInputList != null)
                        {
                            modelInputs = modelInputList.Select(mi => new ToolInputInfo
                            {
                                Name = mi.Text ?? mi.Id,
                                Description = "",
                                DataType = mi.Type ?? "text",
                                IsRequired = true,
                                FillUsing = "Dynamically fill with AI"
                            }).ToList();
                        }
                        // Extract model parameters from JSON
                        var configJson = model.getCustomConfiguration();
                        if (configJson != null && configJson.TryGetValue("modelParameters", out JToken mpToken))
                        {
                            var mp = (JObject)mpToken;
                            modelParams = $"Model: {mp["modelType"]}, Temperature: {mp["gptParameters"]?["temperature"]}";
                        }
                    }
                }
                catch { }

                // Use model inputs if custom API had none
                if (inputs.Count == 0 && modelInputs != null)
                    inputs = modelInputs;

                bool enabled = plugin.StateCode == 0;
                var info = new AgentToolInfo
                {
                    Name = plugin.HumanName,
                    ToolType = "Prompt",
                    AvailableTo = Name,
                    Trigger = "None",
                    Enabled = enabled,
                    Description = model?.getName() ?? plugin.ModelName ?? "",
                    ConnectionReference = null,
                    OperationId = null,
                    FlowId = null,
                    AgentFlowName = null,
                    ResponseActivity = null,
                    ResponseMode = null,
                    OutputMode = null,
                    Inputs = inputs,
                    Outputs = outputs,
                    PromptText = promptText,
                    ModelParameters = modelParams
                };
                result.Add(info);
            }

            return result.OrderBy(t => t.Name).ToList();
        }

        private string GetFlowNameForId(string flowId)
        {
            // Try matching against known flow workflows in BotComponents isn't reliable;
            // flow name resolution happens via customizations.xml if available.
            return null;
        }

        private static string GetCustomApiTypeDisplayName(int type)
        {
            return type switch
            {
                0 => "Boolean",
                1 => "DateTime",
                2 => "Decimal",
                3 => "Entity",
                4 => "EntityCollection",
                5 => "EntityReference",
                6 => "Float",
                7 => "Integer",
                8 => "Money",
                9 => "Picklist",
                10 => "String",
                11 => "StringArray",
                12 => "Guid",
                _ => $"Type {type}"
            };
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

        /// <summary>
        /// Returns the Dataverse table entities linked to a knowledge source via skillConfiguration → dvtablesearch → dvtablesearchentity.
        /// </summary>
        public List<DvTableSearchEntity> GetDataverseTablesForKnowledge(BotComponent knowledge)
        {
            var (_, skillConfig) = knowledge.GetKnowledgeSourceDetails();
            if (string.IsNullOrEmpty(skillConfig)) return new List<DvTableSearchEntity>();
            var dvSearch = DvTableSearches.FirstOrDefault(d => d.Name == skillConfig);
            if (dvSearch == null) return new List<DvTableSearchEntity>();
            return DvTableSearchEntities.Where(e => e.DvTableSearchId == dvSearch.Id).ToList();
        }

        /// <summary>
        /// Returns the copilot synonyms linked to a dvtablesearchentity.
        /// </summary>
        public List<CopilotSynonym> GetSynonymsForEntity(DvTableSearchEntity entity)
        {
            return CopilotSynonyms.Where(s => s.DvTableSearchEntityId == entity.Id).ToList();
        }

        /// <summary>
        /// Returns a human-readable details summary for a knowledge source (URL for web, table names for Dataverse).
        /// </summary>
        public string GetKnowledgeDetailsSummary(BotComponent knowledge)
        {
            string site = knowledge.GetKnowledgeSourceSite();
            if (!string.IsNullOrEmpty(site)) return site;
            var tables = GetDataverseTablesForKnowledge(knowledge);
            if (tables.Count > 0)
                return string.Join(", ", tables.Select(t => t.Name));
            return "";
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
        public string Category { get; set; }

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
        /// Returns a human-readable display name for the knowledge source kind.
        /// </summary>
        public string GetSourceKindDisplayName()
        {
            var (sourceKind, _) = GetKnowledgeSourceDetails();
            return sourceKind switch
            {
                "DataverseStructuredSearchSource" => "Dataverse",
                "SharePointSearchSource" => "SharePoint",
                "PublicSiteSearchSource" => "Public Website",
                _ => sourceKind ?? "Unknown"
            };
        }

        /// <summary>
        /// Returns whether this is an official/authoritative source.
        /// </summary>
        public string GetOfficialSourceDisplayName()
        {
            if (string.IsNullOrEmpty(Category)) return "";
            return Category.Equals("Authoritative", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No";
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
        public (string ActionKind, string ConnectionReference, string OperationId, string FlowId, string ModelDisplayName, string ModelDescription, string ResponseActivity, string ResponseMode, string OutputMode, string TriggerCondition, List<ToolInputInfo> Inputs, List<ToolOutputInfo> Outputs) GetToolDetails()
        {
            string actionKind = "", connectionRef = "", operationId = "", flowId = "", modelDisplayName = "", modelDescription = "";
            string responseActivity = "", responseMode = "", outputMode = "", triggerCondition = "";
            var inputs = new List<ToolInputInfo>();
            var outputs = new List<ToolOutputInfo>();
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("modelDisplayName"), out var mdnNode))
                    modelDisplayName = mdnNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("modelDescription"), out var mdNode))
                    modelDescription = mdNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("outputMode"), out var omNode))
                    outputMode = omNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("triggerCondition"), out var tcNode))
                    triggerCondition = tcNode.ToString();
                // Parse response block
                if (mapping.Children.TryGetValue(new YamlScalarNode("response"), out var responseNode) && responseNode is YamlMappingNode responseMapping)
                {
                    if (responseMapping.Children.TryGetValue(new YamlScalarNode("activity"), out var actNode))
                        responseActivity = actNode.ToString();
                    if (responseMapping.Children.TryGetValue(new YamlScalarNode("mode"), out var modeNode))
                        responseMode = modeNode.ToString();
                }
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
                            string entity = inputMapping.Children.TryGetValue(new YamlScalarNode("entity"), out var eNode) ? eNode.ToString() : "";
                            string desc = inputMapping.Children.TryGetValue(new YamlScalarNode("description"), out var descNode) ? descNode.ToString() : "";
                            string dataType = !string.IsNullOrEmpty(entity) ? entity : "";
                            bool isRequired = inputMapping.Children.TryGetValue(new YamlScalarNode("isRequired"), out var reqNode) && reqNode.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
                            inputs.Add(new ToolInputInfo
                            {
                                Name = propName,
                                Description = desc,
                                DataType = dataType,
                                IsRequired = isRequired,
                                FillUsing = "Dynamically fill with AI"
                            });
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
                            string desc = outputMapping.Children.TryGetValue(new YamlScalarNode("description"), out var descNode) ? descNode.ToString() : "";
                            outputs.Add(new ToolOutputInfo
                            {
                                Name = propName,
                                Description = desc
                            });
                        }
                    }
                }
            }
            catch { }
            return (actionKind, connectionRef, operationId, flowId, modelDisplayName, modelDescription, responseActivity, responseMode, outputMode, triggerCondition, inputs, outputs);
        }

        /// <summary>
        /// Returns connected agent details from InvokeConnectedAgentTaskAction YAML data.
        /// Extracts botSchemaName, historyType, modelDisplayName, and modelDescription.
        /// </summary>
        public (string BotSchemaName, string HistoryType, string ModelDisplayName, string ModelDescription) GetConnectedAgentDetails()
        {
            string botSchemaName = "", historyType = "", modelDisplayName = "", modelDescription = "";
            try
            {
                var mapping = GetYamlMappingNode();
                if (mapping.Children.TryGetValue(new YamlScalarNode("modelDisplayName"), out var mdnNode))
                    modelDisplayName = mdnNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("modelDescription"), out var mdNode))
                    modelDescription = mdNode.ToString();
                if (mapping.Children.TryGetValue(new YamlScalarNode("action"), out var actionNode) && actionNode is YamlMappingNode actionMapping)
                {
                    if (actionMapping.Children.TryGetValue(new YamlScalarNode("botSchemaName"), out var bsnNode))
                        botSchemaName = bsnNode.ToString();
                    if (actionMapping.Children.TryGetValue(new YamlScalarNode("historyType"), out var htNode) && htNode is YamlMappingNode htMapping)
                    {
                        if (htMapping.Children.TryGetValue(new YamlScalarNode("kind"), out var kindNode))
                            historyType = kindNode.ToString();
                    }
                }
            }
            catch { }
            return (botSchemaName, historyType, modelDisplayName, modelDescription);
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
            var sanitized = SanitizeYaml(YamlData);
            var input = new StringReader(sanitized);
            var yaml = new YamlStream();
            yaml.Load(input);
            return (YamlMappingNode)yaml.Documents[0].RootNode;
        }

        /// <summary>
        /// Sanitizes YAML text line-by-line, skipping block scalar regions (|, |- , >, >-),
        /// to fix keys containing @ and plain scalar values containing ": ".
        /// </summary>
        private static string SanitizeYaml(string yamlText)
        {
            var lines = yamlText.Split('\n');
            var result = new List<string>(lines.Length);
            int blockScalarBaseIndent = -1; // -1 means not inside a block scalar

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimEnd('\r');

                // If we're inside a block scalar, check whether this line is still part of it
                if (blockScalarBaseIndent >= 0)
                {
                    // Empty lines are part of the block scalar
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        result.Add(line);
                        continue;
                    }
                    int currentIndent = trimmed.Length - trimmed.TrimStart().Length;
                    if (currentIndent > blockScalarBaseIndent)
                    {
                        // Still inside the block scalar - don't touch this line
                        result.Add(line);
                        continue;
                    }
                    // Indentation dropped back to or below the key level - block scalar ended
                    blockScalarBaseIndent = -1;
                }

                // Check if this line starts a block scalar (key: | or key: >  with optional - / + / digit)
                var blockMatch = Regex.Match(trimmed, @"^(\s*(?:-\s+)?)[\w][\w._-]*:\s+[|>][-+]?\s*$");
                if (blockMatch.Success)
                {
                    // Record the indentation of this key line; content lines will be indented further
                    blockScalarBaseIndent = trimmed.Length - trimmed.TrimStart().Length;
                    result.Add(line);
                    continue;
                }

                // Not inside a block scalar - apply sanitizations

                // 1) Quote unquoted YAML keys containing @
                string sanitizedLine = Regex.Replace(trimmed, @"^(\s*)([\w._-]*@[\w.@_-]*)(\s*:)", "$1\"$2\"$3");

                // 2) Quote plain scalar values that would break YAML parsing
                sanitizedLine = Regex.Replace(sanitizedLine, @"^(\s*(?:-\s+)?[\w][\w._-]*:\s)(.+)$", m =>
                {
                    string prefix = m.Groups[1].Value;
                    string value = m.Groups[2].Value;
                    // Skip values already quoted or using block/flow scalar indicators
                    if (value.Length > 0 && (value[0] == '"' || value[0] == '\'' || value[0] == '|' || value[0] == '>' || value[0] == '[' || value[0] == '{'))
                        return m.Value;
                    // Quote if value starts with a YAML indicator character that can't begin a plain scalar
                    // (e.g. * for alias, & for anchor, ! for tag, # for comment, @ and ` reserved)
                    bool needsQuoting = value.Length > 0 && "*&!#%@`".IndexOf(value[0]) >= 0;
                    // Quote if the value contains ": " which would confuse the YAML parser
                    if (value.Contains(": "))
                        needsQuoting = true;
                    if (needsQuoting)
                        return prefix + "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
                    return m.Value;
                });

                result.Add(sanitizedLine);
            }
            return string.Join("\n", result);
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

    public class DvTableSearch
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SearchType { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
    }

    public class DvTableSearchEntity
    {
        public string Id { get; set; }
        public string DvTableSearchId { get; set; }
        public string EntityLogicalName { get; set; }
        public string Name { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
    }

    public class CopilotSynonym
    {
        public string Id { get; set; }
        public string ColumnLogicalName { get; set; }
        public string Description { get; set; }
        public string DvTableSearchEntityId { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
    }

    /// <summary>
    /// Represents an AI Plugin (from aiplugins/*.xml) — typically a prompt or connector tool.
    /// </summary>
    public class AIPluginEntity
    {
        public string Name { get; set; }
        public string HumanName { get; set; }
        public string ModelName { get; set; }
        public int PluginSubType { get; set; }
        public int PluginType { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
    }

    /// <summary>
    /// Represents an AI Plugin Operation (from aipluginoperations/*.xml).
    /// Links an AIPlugin to an AIModel and CustomApi.
    /// </summary>
    public class AIPluginOperationEntity
    {
        public string AIPluginName { get; set; }
        public string OperationId { get; set; }
        public string Name { get; set; }
        public string AIModelId { get; set; }
        public string CustomApiUniqueName { get; set; }
        public int IsConsequential { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
    }

    /// <summary>
    /// Represents a Custom API definition (from customapis/*/customapi.xml).
    /// </summary>
    public class CustomApiEntity
    {
        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<CustomApiRequestParameterEntity> RequestParameters { get; set; } = new List<CustomApiRequestParameterEntity>();
        public List<CustomApiResponsePropertyEntity> ResponseProperties { get; set; } = new List<CustomApiResponsePropertyEntity>();
    }

    /// <summary>
    /// Represents a Custom API Request Parameter (from customapirequestparameters/*/customapirequestparameter.xml).
    /// </summary>
    public class CustomApiRequestParameterEntity
    {
        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }
        public bool IsOptional { get; set; }
    }

    /// <summary>
    /// Represents a Custom API Response Property (from customapiresponseproperties/*/customapiresponseproperty.xml).
    /// </summary>
    public class CustomApiResponsePropertyEntity
    {
        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }
    }

    /// <summary>
    /// Input info for a tool (unified across flow/connector/prompt tools).
    /// </summary>
    public class ToolInputInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DataType { get; set; }
        public bool IsRequired { get; set; }
        public string FillUsing { get; set; }
    }

    /// <summary>
    /// Output info for a tool (unified across flow/connector/prompt tools).
    /// </summary>
    public class ToolOutputInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DataType { get; set; }
    }

    /// <summary>
    /// Unified tool information, combining data from BotComponent TaskDialogs and AIPlugin prompt tools.
    /// </summary>
    /// <summary>
    /// Information about a connected (child) agent referenced via InvokeConnectedAgentTaskAction.
    /// </summary>
    public class ConnectedAgentInfo
    {
        public string Name { get; set; }
        public string BotSchemaName { get; set; }
        public string Description { get; set; }
        public string HistoryType { get; set; }
        public string ConnectionType { get; set; }
    }

    public class AgentToolInfo
    {
        public string Name { get; set; }
        public string ToolType { get; set; }
        public string AvailableTo { get; set; }
        public string Trigger { get; set; }
        public bool Enabled { get; set; }
        public string Description { get; set; }
        public string ConnectionReference { get; set; }
        public string OperationId { get; set; }
        public string FlowId { get; set; }
        public string AgentFlowName { get; set; }
        public string ResponseActivity { get; set; }
        public string ResponseMode { get; set; }
        public string OutputMode { get; set; }
        public List<ToolInputInfo> Inputs { get; set; } = new List<ToolInputInfo>();
        public List<ToolOutputInfo> Outputs { get; set; } = new List<ToolOutputInfo>();
        public string PromptText { get; set; }
        public string ModelParameters { get; set; }
    }

}
