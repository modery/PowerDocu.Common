using System;
using System.Collections.Generic;
using System.Xml;

namespace PowerDocu.Common
{
    /// <summary>
    /// Parses Classic Workflow XAML definitions into ClassicWorkflowEntity step trees.
    /// 
    /// Classic Workflow XAML structure:
    /// - Root: &lt;Activity&gt; → &lt;mxswa:Workflow&gt;
    /// - Direct mxswa: elements: UpdateEntity, CreateEntity, SendEmail, SetState, AssignEntity,
    ///   GetEntityProperty, SetEntityProperty, Postpone, RetrieveEntity
    /// - ActivityReference wrappers: ConditionSequence, ConditionBranch, Composite,
    ///   EvaluateCondition, EvaluateExpression, custom activities
    /// - Client activities (mcwc:): SetDisplayMode, SetVisibility, SetFieldRequiredLevel,
    ///   SetAttributeValue, SetDefaultValue, SetMessage
    /// - Bare elements: TerminateWorkflow, Sequence, Assign, If
    /// </summary>
    public static class ClassicWorkflowXamlParser
    {
        /// <summary>
        /// Maps variable names to human-readable descriptions of their source.
        /// Built by scanning GetEntityProperty and EvaluateExpression (CreateCrmType) nodes
        /// before parsing steps, so that dynamic references can be resolved.
        /// </summary>
        private static Dictionary<string, string> _variableMap;

        /// <summary>
        /// Maps condition result variable names to structured ConditionExpression trees.
        /// Built by scanning EvaluateCondition and EvaluateLogicalCondition nodes.
        /// </summary>
        private static Dictionary<string, ConditionExpression> _conditionTreeMap;

        public static void ParseClassicWorkflowXaml(ClassicWorkflowEntity workflow, string xamlContent)
        {
            if (string.IsNullOrEmpty(xamlContent)) return;
            XmlDocument doc = new XmlDocument { XmlResolver = null };
            try
            {
                doc.LoadXml(xamlContent);
            }
            catch
            {
                return;
            }

            // Find the mxswa:Workflow element
            XmlNode workflowNode = FindWorkflowNode(doc.DocumentElement);
            if (workflowNode == null)
                workflowNode = doc.DocumentElement; // fallback to root

            // Phase 1: Build variable resolution map from GetEntityProperty and CreateCrmType
            _variableMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _conditionTreeMap = new Dictionary<string, ConditionExpression>(StringComparer.OrdinalIgnoreCase);
            BuildVariableMap(workflowNode);

            // Phase 2: Parse steps
            ParseStepsRecursive(workflowNode, workflow.Steps, 0);
            BuildTableReferences(workflow);

            _variableMap = null;
            _conditionTreeMap = null;
        }

        private static XmlNode FindWorkflowNode(XmlNode root)
        {
            if (root == null) return null;
            foreach (XmlNode child in root.ChildNodes)
            {
                if (child.LocalName == "Workflow")
                    return child;
                XmlNode found = FindWorkflowNode(child);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Scans the entire XAML tree to build a variable-to-description map.
        /// Traces: GetEntityProperty → variable (e.g., "Entity.Attribute")
        ///         CreateCrmType → variable (e.g., literal value "100000002")
        ///         SelectFirstNonNull/ConvertCrmXrmTypes → propagate source
        /// </summary>
        private static void BuildVariableMap(XmlNode node)
        {
            if (node == null) return;

            // GetEntityProperty: maps Value variable to "{EntityName}.{Attribute}"
            if (node.LocalName == "GetEntityProperty")
            {
                string attr = node.Attributes?["Attribute"]?.Value;
                string entityName = node.Attributes?["EntityName"]?.Value;
                string valueVar = node.Attributes?["Value"]?.Value;
                if (!string.IsNullOrEmpty(valueVar) && !string.IsNullOrEmpty(attr))
                {
                    string varName = valueVar.Trim('[', ']');
                    string entityPart = !string.IsNullOrEmpty(entityName) ? entityName + "." : "";

                    // Check if the Entity references a related entity
                    string entityRef = node.Attributes?["Entity"]?.Value ?? "";
                    if (entityRef.Contains("related_"))
                    {
                        // InputEntities("related_msf_assigned#msf_person") → "msf_person (Related)"
                        var match = System.Text.RegularExpressions.Regex.Match(entityRef, @"related_[^#]*#(\w+)");
                        if (match.Success)
                            entityPart = match.Groups[1].Value + " (Related).";
                    }

                    _variableMap[varName] = "{" + entityPart + attr + "}";
                }
            }

            // EvaluateExpression with CreateCrmType: maps Result variable to literal value
            if (node.LocalName == "ActivityReference")
            {
                string aqn = node.Attributes?["AssemblyQualifiedName"]?.Value;
                if (aqn != null && aqn.Contains("EvaluateExpression"))
                {
                    string op = GetArgumentValue(node, "ExpressionOperator");
                    string resultVar = GetArgumentValue(node, "Result", isOutput: true);

                    if (op == "CreateCrmType" && !string.IsNullOrEmpty(resultVar))
                    {
                        // Parameters: [New Object() { WorkflowPropertyType.String, "value", "String" }]
                        string paramRaw = GetArgumentValue(node, "Parameters");
                        string literal = ExtractLiteralFromCreateCrmType(paramRaw);
                        if (!string.IsNullOrEmpty(literal))
                            _variableMap[resultVar] = "\"" + literal + "\"";
                    }
                    else if (op == "SelectFirstNonNull" && !string.IsNullOrEmpty(resultVar))
                    {
                        // Propagate source: Parameters contains the source variable
                        string paramRaw = GetArgumentValue(node, "Parameters");
                        string sourceVar = ExtractVariableFromParams(paramRaw);
                        if (!string.IsNullOrEmpty(sourceVar) && _variableMap.ContainsKey(sourceVar))
                            _variableMap[resultVar] = _variableMap[sourceVar];
                    }
                    else if (op == "Add" && !string.IsNullOrEmpty(resultVar))
                    {
                        // String concatenation: resolve each part
                        string paramRaw = GetArgumentValue(node, "Parameters");
                        string resolved = ResolveAddExpression(paramRaw);
                        if (!string.IsNullOrEmpty(resolved))
                            _variableMap[resultVar] = resolved;
                    }
                    else if (!string.IsNullOrEmpty(op) && !string.IsNullOrEmpty(resultVar))
                    {
                        // Handle other expression operators as system values
                        // RetrieveCurrentTime → "Execution Time"
                        // RetrieveActivityCount → "Activity Count"
                        string systemLabel = op switch
                        {
                            "RetrieveCurrentTime" => "{Process.Execution Time}",
                            "RetrieveActivityCount" => "{Process.Activity Count}",
                            "RetrieveUserId" => "{Process.Initiating User}",
                            _ => null
                        };
                        if (!string.IsNullOrEmpty(systemLabel))
                            _variableMap[resultVar] = systemLabel;
                    }
                }
                else if (aqn != null && aqn.Contains("ConvertCrmXrmTypes"))
                {
                    // Propagate: Value → Result
                    string valueVar = GetArgumentValue(node, "Value");
                    string resultVar = GetArgumentValue(node, "Result", isOutput: true);
                    if (!string.IsNullOrEmpty(valueVar) && !string.IsNullOrEmpty(resultVar))
                    {
                        if (_variableMap.ContainsKey(valueVar))
                            _variableMap[resultVar] = _variableMap[valueVar];
                    }
                    // Also map the _converted variant
                    if (!string.IsNullOrEmpty(resultVar) && !_variableMap.ContainsKey(resultVar) &&
                        !string.IsNullOrEmpty(valueVar) && _variableMap.ContainsKey(valueVar))
                        _variableMap[resultVar] = _variableMap[valueVar];
                }
                else if (aqn != null && aqn.Contains("EvaluateCondition"))
                {
                    // Single condition: Operand <ConditionOperator> Parameters → Result
                    string condOp = GetArgumentValue(node, "ConditionOperator");
                    string operandVar = GetArgumentValue(node, "Operand");
                    string paramRaw = GetArgumentValue(node, "Parameters");
                    string resultVar = GetArgumentValue(node, "Result", isOutput: true);

                    if (!string.IsNullOrEmpty(resultVar) && !string.IsNullOrEmpty(condOp))
                    {
                        string operandDesc = ResolveVariableRef(operandVar);
                        string paramDesc = "";
                        if (!string.IsNullOrEmpty(paramRaw))
                        {
                            // Handle multiple parameters (e.g., In operator with multiple values)
                            var allParamVars = ExtractAllVariablesFromParams(paramRaw);
                            if (allParamVars.Count > 0)
                            {
                                var resolvedParams = new List<string>();
                                foreach (string pv in allParamVars)
                                    resolvedParams.Add(ResolveVariableRef(pv));
                                paramDesc = string.Join(", ", resolvedParams);
                            }
                        }

                        string humanOp = HumanReadableOperator(condOp);

                        // Build leaf condition expression
                        var leaf = new ConditionExpression
                        {
                            Field = operandDesc,
                            Operator = humanOp,
                            Value = string.IsNullOrEmpty(paramDesc) ? null : paramDesc
                        };
                        _conditionTreeMap[resultVar] = leaf;
                    }
                }
                else if (aqn != null && aqn.Contains("EvaluateLogicalCondition"))
                {
                    // Logical combiner: LeftOperand <LogicalOperator> RightOperand → Result
                    string logicalOp = GetArgumentValue(node, "LogicalOperator");
                    string leftVar = GetArgumentValue(node, "LeftOperand");
                    string rightVar = GetArgumentValue(node, "RightOperand");
                    string resultVar = GetArgumentValue(node, "Result", isOutput: true);

                    if (!string.IsNullOrEmpty(resultVar))
                    {
                        string op = logicalOp?.ToUpperInvariant() ?? "AND";
                        ConditionExpression leftTree = _conditionTreeMap.ContainsKey(leftVar) ? _conditionTreeMap[leftVar] : null;
                        ConditionExpression rightTree = _conditionTreeMap.ContainsKey(rightVar) ? _conditionTreeMap[rightVar] : null;

                        // Flatten same-operator chains in both directions
                        bool leftIsSameGroup = leftTree != null && leftTree.IsGroup && leftTree.LogicalOperator == op;
                        bool rightIsSameGroup = rightTree != null && rightTree.IsGroup && rightTree.LogicalOperator == op;

                        if (leftIsSameGroup && rightIsSameGroup)
                        {
                            // Both are same-op groups: merge right's children into left
                            leftTree.Children.AddRange(rightTree.Children);
                            _conditionTreeMap[resultVar] = leftTree;
                        }
                        else if (leftIsSameGroup)
                        {
                            if (rightTree != null)
                                leftTree.Children.Add(rightTree);
                            _conditionTreeMap[resultVar] = leftTree;
                        }
                        else if (rightIsSameGroup)
                        {
                            // Right is same-op group: prepend left into it
                            if (leftTree != null)
                                rightTree.Children.Insert(0, leftTree);
                            _conditionTreeMap[resultVar] = rightTree;
                        }
                        else
                        {
                            var group = new ConditionExpression { LogicalOperator = op };
                            if (leftTree != null) group.Children.Add(leftTree);
                            if (rightTree != null) group.Children.Add(rightTree);
                            _conditionTreeMap[resultVar] = group;
                        }
                    }
                }
            }

            foreach (XmlNode child in node.ChildNodes)
                BuildVariableMap(child);
        }

        /// <summary>Resolves a variable name to its human-readable source from the variable map.</summary>
        private static string ResolveVariableRef(string varName)
        {
            if (string.IsNullOrEmpty(varName)) return "(unknown)";
            if (_variableMap != null && _variableMap.TryGetValue(varName, out string desc))
                return desc;
            return varName;
        }

        private static string HumanReadableOperator(string conditionOperator)
        {
            return conditionOperator switch
            {
                "Equal" => "Equals",
                "NotEqual" => "Does Not Equal",
                "GreaterThan" => ">",
                "LessThan" => "<",
                "GreaterEqual" or "GreaterOrEqual" => ">=",
                "LessEqual" or "LessOrEqual" => "<=",
                "NotNull" => "Contains Data",
                "Null" => "Does Not Contain Data",
                "In" => "Is In",
                "NotIn" => "Is Not In",
                "Like" => "Like",
                "NotLike" => "Not Like",
                "BeginsWith" => "Begins With",
                "EndsWith" => "Ends With",
                "Contains" => "Contains",
                "DoesNotContain" => "Does Not Contain",
                "DoesNotBeginWith" => "Does Not Begin With",
                "DoesNotEndWith" => "Does Not End With",
                _ => conditionOperator
            };
        }

        private static string GetArgumentValue(XmlNode activityRef, string key, bool isOutput = false)
        {
            foreach (XmlNode child in activityRef.ChildNodes)
            {
                if (child.LocalName == "ActivityReference.Arguments")
                {
                    foreach (XmlNode arg in child.ChildNodes)
                    {
                        string argKey = arg.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(arg, "Key");
                        if (argKey == key)
                        {
                            string raw = arg.InnerText?.Trim() ?? "";
                            return raw.Trim('[', ']');
                        }
                    }
                }
            }
            return null;
        }

        private static string ExtractLiteralFromCreateCrmType(string paramRaw)
        {
            // Pattern: New Object() { WorkflowPropertyType.String, "actualValue", "String" }
            // or: New Object() { WorkflowPropertyType.OptionSetValue, "100000002", "Picklist" }
            if (string.IsNullOrEmpty(paramRaw)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(paramRaw,
                @"WorkflowPropertyType\.\w+,\s*""([^""]*)""\s*(?:,\s*""[^""]*"")?");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string ExtractVariableFromParams(string paramRaw)
        {
            // Pattern: New Object() { VariableName }
            if (string.IsNullOrEmpty(paramRaw)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(paramRaw, @"\{\s*(\w+)\s*\}");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static List<string> ExtractAllVariablesFromParams(string paramRaw)
        {
            // Pattern: New Object() { Var1, Var2, Var3 }
            var result = new List<string>();
            if (string.IsNullOrEmpty(paramRaw)) return result;
            var match = System.Text.RegularExpressions.Regex.Match(paramRaw, @"\{(.+)\}");
            if (!match.Success) return result;
            string[] parts = match.Groups[1].Value.Split(',');
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            return result;
        }

        private static string ResolveAddExpression(string paramRaw)
        {
            // Pattern: New Object() { Var1, Var2, Var3 }
            if (string.IsNullOrEmpty(paramRaw)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(paramRaw, @"\{(.+)\}");
            if (!match.Success) return null;

            string[] parts = match.Groups[1].Value.Split(',');
            var resolved = new List<string>();
            foreach (string part in parts)
            {
                string varName = part.Trim();
                if (_variableMap.ContainsKey(varName))
                    resolved.Add(_variableMap[varName]);
                else
                    resolved.Add("{" + varName + "}");
            }
            return string.Join(" + ", resolved);
        }

        private static void ParseStepsRecursive(XmlNode parent, List<ClassicWorkflowStep> steps, int nestingLevel)
        {
            if (parent == null) return;

            foreach (XmlNode child in parent.ChildNodes)
            {
                ClassicWorkflowStep step = TryParseAsStep(child, nestingLevel);
                if (step != null)
                {
                    // Extract step description from parent Sequence's Variables
                    if (string.IsNullOrEmpty(step.StepDescription))
                        step.StepDescription = ExtractStepDescription(child);
                    steps.Add(step);
                }
                else if (!IsSkippableStructuralNode(child))
                {
                    // Recurse into non-step structural elements (Properties, Activities collections, etc.)
                    ParseStepsRecursive(child, steps, nestingLevel);
                }
            }
        }

        private static ClassicWorkflowStep TryParseAsStep(XmlNode node, int nestingLevel)
        {
            if (node == null) return null;
            string localName = node.LocalName;

            // ── Direct mxswa: elements ──
            switch (localName)
            {
                case "UpdateEntity":
                    return ParseDirectElement(node, ClassicWorkflowStepType.UpdateRecord, nestingLevel);
                case "CreateEntity":
                    return ParseDirectElement(node, ClassicWorkflowStepType.CreateRecord, nestingLevel);
                case "SendEmail":
                    return ParseSendEmail(node, nestingLevel);
                case "SetState":
                    return ParseSetState(node, nestingLevel);
                case "AssignEntity":
                    return ParseAssignEntity(node, nestingLevel);
                case "Postpone":
                    return ParsePostpone(node, nestingLevel);
            }

            // ── TerminateWorkflow (bare element) ──
            if (localName == "TerminateWorkflow")
            {
                string userDesc = ExtractDisplayNameDescription(node);
                string friendlyName = "Stop Workflow";
                if (!string.IsNullOrEmpty(userDesc) && !userDesc.StartsWith("StopWorkflowStep"))
                    friendlyName = userDesc;

                var step = new ClassicWorkflowStep
                {
                    StepType = ClassicWorkflowStepType.Stop,
                    Name = friendlyName,
                    StepId = ExtractStepIdFromDisplayName(node),
                    NestingLevel = nestingLevel
                };

                // Extract status (Succeeded/Canceled) from Exception attribute
                string exception = node.Attributes?["Exception"]?.Value ?? "";
                if (exception.Contains("Succeeded"))
                    step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Status", Value = "Succeeded", SourceType = "Static" });
                else if (exception.Contains("Canceled") || exception.Contains("Failed"))
                    step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Status", Value = "Canceled", SourceType = "Static" });

                // Extract reason/message
                string reason = node.Attributes?["Reason"]?.Value ?? "";
                string resolvedReason = CleanDynamicExpression(reason);
                if (!string.IsNullOrEmpty(resolvedReason) && resolvedReason != "(Dynamic)")
                    step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Message", Value = resolvedReason, SourceType = "Static" });

                return step;
            }

            // ── InvokeSdkMessageActivity (Action step) ──
            if (localName == "InvokeSdkMessageActivity")
            {
                string userDesc = ExtractDisplayNameDescription(node);
                string friendlyName = "Run Action";
                if (!string.IsNullOrEmpty(userDesc) && !userDesc.StartsWith("InvokeSdkMessageStep"))
                    friendlyName = userDesc;

                var step = new ClassicWorkflowStep
                {
                    StepType = ClassicWorkflowStepType.ChildWorkflow,
                    Name = friendlyName,
                    StepId = ExtractStepIdFromDisplayName(node),
                    NestingLevel = nestingLevel
                };

                string sdkMessageName = node.Attributes?["SdkMessageName"]?.Value;
                string sdkEntityName = node.Attributes?["SdkMessageEntityName"]?.Value;

                if (!string.IsNullOrEmpty(sdkMessageName))
                    step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Action Name", Value = sdkMessageName, SourceType = "Static" });
                if (!string.IsNullOrEmpty(sdkEntityName) && sdkEntityName != "none")
                    step.TargetEntity = sdkEntityName;
                else if (sdkEntityName == "none")
                    step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Entity", Value = "Global (none)", SourceType = "Static" });

                // Extract arguments
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.LocalName == "InvokeSdkMessageActivity.Arguments")
                    {
                        foreach (XmlNode arg in child.ChildNodes)
                        {
                            if (arg.LocalName == "InArgument" || arg.LocalName.StartsWith("InArgument"))
                            {
                                string key = arg.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(arg, "Key");
                                if (!string.IsNullOrEmpty(key) && key != "Target")
                                {
                                    string rawValue = arg.InnerText?.Trim() ?? "";
                                    step.Fields.Add(new ClassicWorkflowFieldAssignment
                                    {
                                        FieldName = key,
                                        Value = CleanDynamicExpression(rawValue),
                                        SourceType = rawValue.StartsWith("[") ? "Dynamic" : "Static"
                                    });
                                }
                            }
                        }
                        break;
                    }
                }

                return step;
            }

            // ── Client activities (mcwc:) ──
            switch (localName)
            {
                case "SetDisplayMode":
                    return ParseClientActivity(node, ClassicWorkflowStepType.SetDisplayMode, nestingLevel);
                case "SetVisibility":
                    return ParseClientActivity(node, ClassicWorkflowStepType.SetVisibility, nestingLevel);
                case "SetFieldRequiredLevel":
                    return ParseClientActivity(node, ClassicWorkflowStepType.SetFieldRequired, nestingLevel);
                case "SetAttributeValue":
                    return ParseClientActivity(node, ClassicWorkflowStepType.SetAttributeValue, nestingLevel);
                case "SetDefaultValue":
                    return ParseClientActivity(node, ClassicWorkflowStepType.SetDefaultValue, nestingLevel);
                case "SetMessage":
                    return ParseClientActivity(node, ClassicWorkflowStepType.SetMessage, nestingLevel);
            }

            // ── ActivityReference elements ──
            if (localName == "ActivityReference")
            {
                string aqn = node.Attributes?["AssemblyQualifiedName"]?.Value;
                if (string.IsNullOrEmpty(aqn)) return null;

                string typeName = ExtractTypeName(aqn);

                switch (typeName)
                {
                    case "ConditionSequence":
                        return ParseConditionSequence(node, nestingLevel);
                    case "ConditionBranch":
                        return ParseConditionBranch(node, nestingLevel);
                    case "Composite":
                        return ParseComposite(node, nestingLevel);

                    // Evaluation helpers — skip as standalone steps, they're internal plumbing
                    case "EvaluateCondition":
                    case "EvaluateExpression":
                    case "EvaluateLogicalCondition":
                    case "ConvertCrmXrmTypes":
                        return null;

                    // BPF-related elements in workflow context — skip
                    case "EntityComposite":
                    case "StageComposite":
                    case "StepComposite":
                    case "StageRelationshipCollectionComposite":
                        return null;

                    default:
                        // Custom activity (e.g., MARCY.Activities.*)
                        if (!aqn.StartsWith("Microsoft.Crm.Workflow") &&
                            !aqn.StartsWith("Microsoft.Xrm.Sdk"))
                        {
                            return ParseCustomActivity(node, aqn, nestingLevel);
                        }
                        return null;
                }
            }

            return null;
        }

        // ── Parsing methods for each step type ──

        private static ClassicWorkflowStep ParseDirectElement(XmlNode node, ClassicWorkflowStepType stepType, int nestingLevel)
        {
            string entityName = node.Attributes?["EntityName"]?.Value;
            string friendlyName = stepType switch
            {
                ClassicWorkflowStepType.UpdateRecord => "Update " + (entityName ?? "Record"),
                ClassicWorkflowStepType.CreateRecord => "Create " + (entityName ?? "Record"),
                _ => ExtractDisplayNameDescription(node) ?? stepType.ToString()
            };
            // Use user-provided description from DisplayName if it has one (e.g., "UpdateStep5: My Description")
            string userDesc = ExtractDisplayNameDescription(node);
            if (!string.IsNullOrEmpty(userDesc) && !userDesc.StartsWith("UpdateStep") && !userDesc.StartsWith("CreateStep"))
                friendlyName = userDesc;

            var step = new ClassicWorkflowStep
            {
                StepType = stepType,
                Name = friendlyName,
                StepId = ExtractStepIdFromDisplayName(node),
                TargetEntity = entityName,
                NestingLevel = nestingLevel
            };

            // For UpdateEntity/CreateEntity — collect SetEntityProperty children as field assignments
            if (stepType == ClassicWorkflowStepType.UpdateRecord || stepType == ClassicWorkflowStepType.CreateRecord)
            {
                CollectFieldAssignmentsFromSiblings(node, step.Fields);
            }

            return step;
        }

        private static ClassicWorkflowStep ParseSetState(XmlNode node, int nestingLevel)
        {
            string entityName = node.Attributes?["EntityName"]?.Value;
            string userDesc = ExtractDisplayNameDescription(node);
            string friendlyName = "Change Status";
            if (!string.IsNullOrEmpty(userDesc) && !userDesc.StartsWith("SetStateStep"))
                friendlyName = userDesc;
            else if (!string.IsNullOrEmpty(entityName))
                friendlyName = "Change Status: " + entityName;

            var step = new ClassicWorkflowStep
            {
                StepType = ClassicWorkflowStepType.ChangeStatus,
                Name = friendlyName,
                StepId = ExtractStepIdFromDisplayName(node),
                TargetEntity = entityName,
                NestingLevel = nestingLevel
            };

            // Extract state/status values from child elements
            string stateVal = ExtractOptionSetValue(node, "State");
            string statusVal = ExtractOptionSetValue(node, "Status");
            if (!string.IsNullOrEmpty(stateVal))
                step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "State", Value = stateVal, SourceType = "Static" });
            if (!string.IsNullOrEmpty(statusVal))
                step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Status", Value = statusVal, SourceType = "Static" });

            return step;
        }

        private static ClassicWorkflowStep ParsePostpone(XmlNode node, int nestingLevel)
        {
            var step = new ClassicWorkflowStep
            {
                StepType = ClassicWorkflowStepType.Wait,
                Name = ExtractDisplayNameDescription(node) ?? "Wait",
                StepId = ExtractStepIdFromDisplayName(node),
                NestingLevel = nestingLevel
            };

            // Extract wait details
            string blockExecution = node.Attributes?["BlockExecution"]?.Value;
            if (!string.IsNullOrEmpty(blockExecution))
                step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Block Execution", Value = blockExecution, SourceType = "Static" });

            string postponeUntil = node.Attributes?["PostponeUntil"]?.Value;
            if (!string.IsNullOrEmpty(postponeUntil))
                step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Wait Until", Value = CleanDynamicExpression(postponeUntil), SourceType = postponeUntil.StartsWith("[") ? "Dynamic" : "Static" });

            return step;
        }

        // Friendly label map for email entity fields
        private static readonly Dictionary<string, string> EmailFieldLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "from", "From" },
            { "to", "To" },
            { "cc", "CC" },
            { "bcc", "BCC" },
            { "subject", "Subject" },
            { "description", "Body" },
            { "regardingobjectid", "Regarding" },
            { "prioritycode", "Priority" },
            { "deliveryprioritycode", "Delivery Priority" },
            { "directioncode", "Direction" },
            { "transactioncurrencyid", "Currency" }
        };

        // System/internal email fields to skip in documentation
        private static readonly HashSet<string> EmailFieldsToSkip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "correlatedsubjectchanged",
            "followemailuserpreference",
            "isduplicatesenderunresolved",
            "notifications"
        };

        private static ClassicWorkflowStep ParseSendEmail(XmlNode node, int nestingLevel)
        {
            var step = new ClassicWorkflowStep
            {
                StepType = ClassicWorkflowStepType.SendEmail,
                Name = "Create Message",
                StepId = ExtractStepIdFromDisplayName(node),
                TargetEntity = "email",
                NestingLevel = nestingLevel
            };

            // Collect email field assignments from SetEntityProperty siblings
            CollectFieldAssignmentsFromSiblings(node, step.Fields);

            // Post-process: apply friendly labels and filter out internal fields
            var filtered = new List<ClassicWorkflowFieldAssignment>();
            foreach (var field in step.Fields)
            {
                if (EmailFieldsToSkip.Contains(field.FieldName))
                    continue;

                if (EmailFieldLabels.TryGetValue(field.FieldName, out string friendlyName))
                    field.FieldName = friendlyName;

                filtered.Add(field);
            }
            step.Fields.Clear();
            step.Fields.AddRange(filtered);

            return step;
        }

        private static ClassicWorkflowStep ParseAssignEntity(XmlNode node, int nestingLevel)
        {
            string entityName = node.Attributes?["EntityName"]?.Value;
            string userDesc = ExtractDisplayNameDescription(node);
            string friendlyName = "Assign";
            if (!string.IsNullOrEmpty(userDesc) && !userDesc.StartsWith("AssignStep"))
                friendlyName = userDesc;
            else if (!string.IsNullOrEmpty(entityName))
                friendlyName = "Assign " + entityName;

            var step = new ClassicWorkflowStep
            {
                StepType = ClassicWorkflowStepType.Assign,
                Name = friendlyName,
                StepId = ExtractStepIdFromDisplayName(node),
                TargetEntity = entityName,
                NestingLevel = nestingLevel
            };

            // Extract the Owner assignment
            string owner = node.Attributes?["Owner"]?.Value;
            if (!string.IsNullOrEmpty(owner))
                step.Fields.Add(new ClassicWorkflowFieldAssignment { FieldName = "Assign To", Value = CleanDynamicExpression(owner), SourceType = owner.StartsWith("[") ? "Dynamic" : "Static" });

            return step;
        }

        private static ClassicWorkflowStep ParseClientActivity(XmlNode node, ClassicWorkflowStepType stepType, int nestingLevel)
        {
            var step = new ClassicWorkflowStep
            {
                StepType = stepType,
                Name = ExtractDisplayNameDescription(node) ?? stepType.ToString(),
                StepId = ExtractStepIdFromDisplayName(node),
                TargetEntity = node.Attributes?["EntityName"]?.Value,
                NestingLevel = nestingLevel
            };

            // Capture the control/field being modified
            string controlId = node.Attributes?["ControlId"]?.Value;
            if (!string.IsNullOrEmpty(controlId))
            {
                string detail = stepType switch
                {
                    ClassicWorkflowStepType.SetVisibility =>
                        node.Attributes?["IsVisible"]?.Value,
                    ClassicWorkflowStepType.SetDisplayMode =>
                        node.Attributes?["IsReadOnly"]?.Value != null ? (node.Attributes["IsReadOnly"].Value == "True" ? "Read-Only" : "Editable") : null,
                    ClassicWorkflowStepType.SetFieldRequired =>
                        node.Attributes?["RequiredLevel"]?.Value,
                    _ => null
                };
                step.Fields.Add(new ClassicWorkflowFieldAssignment
                {
                    FieldName = controlId,
                    Value = detail ?? "",
                    SourceType = "Static"
                });
            }

            return step;
        }

        private static ClassicWorkflowStep ParseConditionSequence(XmlNode node, int nestingLevel)
        {
            // Check if this is a Wait Condition (ConditionSequence with Wait=True)
            bool isWait = false;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.LocalName == "ActivityReference.Arguments")
                {
                    foreach (XmlNode arg in child.ChildNodes)
                    {
                        string argKey = arg.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(arg, "Key");
                        if (argKey == "Wait" && arg.InnerText?.Trim() == "True")
                        {
                            isWait = true;
                            break;
                        }
                    }
                    break;
                }
            }

            var step = new ClassicWorkflowStep
            {
                StepType = isWait ? ClassicWorkflowStepType.Wait : ClassicWorkflowStepType.CheckCondition,
                Name = ExtractDisplayNameDescription(node) ?? (isWait ? "Wait Condition" : "Check Condition"),
                StepId = ExtractStepIdFromDisplayName(node),
                NestingLevel = nestingLevel
            };

            // Parse child activities to find branches and nested steps
            XmlNode activitiesCollection = FindActivitiesCollection(node);
            if (activitiesCollection != null)
            {
                ParseStepsRecursive(activitiesCollection, step.ChildSteps, nestingLevel + 1);
            }

            // Relabel ConditionBranch children to match Dataverse editor: If / Otherwise If / Otherwise
            bool firstConditionalBranch = true;
            foreach (var child in step.ChildSteps)
            {
                if (child.StepType != ClassicWorkflowStepType.ConditionBranch) continue;

                if (child.Name == "Otherwise (Default)")
                {
                    // Already labeled by ParseConditionBranch
                    continue;
                }

                if (firstConditionalBranch)
                {
                    child.Name = "If";
                    firstConditionalBranch = false;
                }
                else
                {
                    child.Name = "Otherwise If";
                }
            }

            return step;
        }

        private static ClassicWorkflowStep ParseConditionBranch(XmlNode node, int nestingLevel)
        {
            var step = new ClassicWorkflowStep
            {
                StepType = ClassicWorkflowStepType.ConditionBranch,
                Name = ExtractDisplayNameDescription(node) ?? "Condition Branch",
                StepId = ExtractStepIdFromDisplayName(node),
                NestingLevel = nestingLevel
            };

            // Resolve the condition expression tree from the Condition argument variable
            string conditionRef = null;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.LocalName == "ActivityReference.Arguments")
                {
                    foreach (XmlNode arg in child.ChildNodes)
                    {
                        string argKey = arg.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(arg, "Key");
                        if (argKey == "Condition")
                        {
                            conditionRef = arg.InnerText?.Trim()?.Trim('[', ']');
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(conditionRef))
            {
                if (conditionRef == "True")
                {
                    step.ConditionDescription = "Otherwise (Default)";
                    step.Name = "Otherwise (Default)";
                }
                else if (_conditionTreeMap != null && _conditionTreeMap.TryGetValue(conditionRef, out var tree))
                {
                    step.ConditionTree = tree;
                    step.ConditionDescription = tree.ToFlatString();
                }
            }

            // Parse the "Then" branch
            XmlNode thenBranch = FindPropertyNode(node, "Then");
            if (thenBranch != null)
            {
                ParseStepsRecursive(thenBranch, step.ChildSteps, nestingLevel + 1);
            }

            // Parse the "Else" branch
            XmlNode elseBranch = FindPropertyNode(node, "Else");
            if (elseBranch != null && elseBranch.LocalName != "Null")
            {
                ParseStepsRecursive(elseBranch, step.ChildSteps, nestingLevel + 1);
            }

            return step;
        }

        private static ClassicWorkflowStep ParseComposite(XmlNode node, int nestingLevel)
        {
            string displayName = node.Attributes?["DisplayName"]?.Value;

            // Composites wrapping custom activities — look for the inner custom ActivityReference
            if (displayName != null && (displayName.StartsWith("CustomActivityStep") || displayName.StartsWith("InvokeSdkMessageStep")))
            {
                var customStep = FindNestedCustomActivity(node, nestingLevel);
                if (customStep != null) return customStep;
                // Also search for InvokeSdkMessageActivity directly
                var actionStep = FindNestedInvokeSdkMessage(node, nestingLevel);
                if (actionStep != null) return actionStep;
            }

            // Otherwise, generic composite — recurse into children to find real steps
            var step = new ClassicWorkflowStep
            {
                StepType = ClassicWorkflowStepType.Custom,
                Name = ExtractDisplayNameDescription(node) ?? "Composite",
                StepId = ExtractStepIdFromDisplayName(node),
                NestingLevel = nestingLevel
            };

            XmlNode activitiesCollection = FindActivitiesCollection(node);
            if (activitiesCollection != null)
            {
                ParseStepsRecursive(activitiesCollection, step.ChildSteps, nestingLevel + 1);
            }

            // If composite had no user-visible content, skip it
            if (step.ChildSteps.Count == 0 && string.IsNullOrEmpty(step.Name))
                return null;

            return step;
        }

        private static ClassicWorkflowStep FindNestedCustomActivity(XmlNode compositeNode, int nestingLevel)
        {
            foreach (XmlNode child in compositeNode.ChildNodes)
            {
                if (child.LocalName == "ActivityReference")
                {
                    string aqn = child.Attributes?["AssemblyQualifiedName"]?.Value;
                    if (!string.IsNullOrEmpty(aqn) &&
                        !aqn.StartsWith("Microsoft.Crm.Workflow") &&
                        !aqn.StartsWith("Microsoft.Xrm.Sdk"))
                    {
                        return ParseCustomActivity(child, aqn, nestingLevel);
                    }
                }

                // Recurse through Properties/Activities collections
                var found = FindNestedCustomActivity(child, nestingLevel);
                if (found != null) return found;
            }
            return null;
        }

        private static ClassicWorkflowStep FindNestedInvokeSdkMessage(XmlNode compositeNode, int nestingLevel)
        {
            foreach (XmlNode child in compositeNode.ChildNodes)
            {
                if (child.LocalName == "InvokeSdkMessageActivity")
                {
                    return TryParseAsStep(child, nestingLevel);
                }

                var found = FindNestedInvokeSdkMessage(child, nestingLevel);
                if (found != null) return found;
            }
            return null;
        }

        private static ClassicWorkflowStep ParseCustomActivity(XmlNode node, string aqn, int nestingLevel)
        {
            // AQN format: "MARCY.Activities.DateTimes.ToFormattedString, MARCY.Activities, Version=1.0.0.0, Culture=neutral, PublicKeyToken=2fb77a038cccb985"
            string[] aqnParts = aqn.Split(',');
            string fullClassName = aqnParts[0].Trim();
            string assemblyName = aqnParts.Length > 1 ? aqnParts[1].Trim() : "";

            // Build a readable assembly description including version and public key token
            string assemblyDesc = assemblyName;
            for (int i = 2; i < aqnParts.Length; i++)
            {
                string part = aqnParts[i].Trim();
                if (part.StartsWith("Version=") || part.StartsWith("PublicKeyToken="))
                    assemblyDesc += ", " + part;
            }

            // Use the last segment of the full class name as a friendly name
            int lastDot = fullClassName.LastIndexOf('.');
            string shortName = lastDot >= 0 ? fullClassName.Substring(lastDot + 1) : fullClassName;

            var step = new ClassicWorkflowStep
            {
                StepType = ClassicWorkflowStepType.Custom,
                Name = ExtractDisplayNameDescription(node) ?? shortName,
                StepId = ExtractStepIdFromDisplayName(node),
                CustomActivityName = shortName,
                CustomActivityClass = fullClassName,
                CustomActivityAssembly = assemblyDesc,
                NestingLevel = nestingLevel
            };

            // Extract input arguments as field assignments
            ExtractCustomActivityArguments(node, step.Fields);

            return step;
        }

        private static void ExtractCustomActivityArguments(XmlNode node, List<ClassicWorkflowFieldAssignment> fields)
        {
            // Find Arguments child
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.LocalName == "ActivityReference.Arguments")
                {
                    foreach (XmlNode argNode in child.ChildNodes)
                    {
                        // InArgument elements have x:Key for name
                        string key = argNode.Attributes?["x:Key"]?.Value ??
                                     GetAttributeByLocalName(argNode, "Key");
                        if (!string.IsNullOrEmpty(key) &&
                            argNode.LocalName != "OutArgument" &&
                            key != "Result")
                        {
                            string rawValue = argNode.InnerText?.Trim() ?? "";
                            string cleanValue = CleanDynamicExpression(rawValue);
                            bool isDynamic = rawValue.StartsWith("[") && rawValue.EndsWith("]");

                            fields.Add(new ClassicWorkflowFieldAssignment
                            {
                                FieldName = key,
                                Value = cleanValue,
                                SourceType = isDynamic ? "Dynamic" : "Static"
                            });
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Resolves raw XAML dynamic expressions into human-readable form using the variable map.
        /// "[DirectCast(CustomActivityStep5_1_converted, System.String)]" → "{msf_reportingdate}" (resolved)
        /// "[New Object() { ... }]" → literal or expression
        /// Static values pass through unchanged.
        /// </summary>
        private static string CleanDynamicExpression(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;

            // Not a dynamic expression — return as-is
            if (!raw.StartsWith("[") || !raw.EndsWith("]"))
                return raw;

            string inner = raw.Substring(1, raw.Length - 2);

            // DirectCast(varName, System.Type) → resolve via variable map
            if (inner.StartsWith("DirectCast("))
            {
                // Extract variable name: DirectCast(CustomActivityStep5_1_converted, System.String)
                int commaIdx = inner.LastIndexOf(',');
                if (commaIdx >= 0)
                {
                    string varName = inner.Substring(11, commaIdx - 11).Trim(); // skip "DirectCast("
                    if (_variableMap != null && _variableMap.TryGetValue(varName, out string resolved))
                        return resolved;

                    // Try without _converted suffix
                    string baseVar = varName.Replace("_converted", "");
                    if (_variableMap != null && _variableMap.TryGetValue(baseVar, out string resolvedBase))
                        return resolvedBase;

                    // Fallback: show the type
                    string typePart = inner.Substring(commaIdx + 1).TrimEnd(')').Trim();
                    int dotIdx = typePart.LastIndexOf('.');
                    string shortType = dotIdx >= 0 ? typePart.Substring(dotIdx + 1) : typePart;
                    return "(Dynamic " + shortType + ")";
                }
                return "(Dynamic)";
            }

            // Simple variable reference [VariableName] — resolve
            if (!inner.Contains(' ') && !inner.Contains('('))
            {
                if (_variableMap != null && _variableMap.TryGetValue(inner, out string resolved))
                    return resolved;
            }

            // New Object() { ... } → try to extract literal
            if (inner.StartsWith("New "))
            {
                string literal = ExtractLiteralFromCreateCrmType(inner);
                if (!string.IsNullOrEmpty(literal))
                    return "\"" + literal + "\"";
                return "(Expression)";
            }

            return "(Dynamic)";
        }

        // ── Field assignment collection ──

        private static void CollectFieldAssignmentsFromSiblings(XmlNode stepNode, List<ClassicWorkflowFieldAssignment> fields)
        {
            // SetEntityProperty elements appear as siblings to UpdateEntity/CreateEntity
            // within the same Sequence parent
            XmlNode parent = stepNode.ParentNode;
            if (parent == null) return;

            string targetEntity = stepNode.Attributes?["EntityName"]?.Value;

            foreach (XmlNode sibling in parent.ChildNodes)
            {
                if (sibling.LocalName == "SetEntityProperty")
                {
                    string attr = sibling.Attributes?["Attribute"]?.Value;
                    string entityName = sibling.Attributes?["EntityName"]?.Value;

                    if (!string.IsNullOrEmpty(attr) &&
                        (string.IsNullOrEmpty(targetEntity) || string.IsNullOrEmpty(entityName) ||
                         entityName.Equals(targetEntity, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Resolve the value from the variable map
                        string valueRef = sibling.Attributes?["Value"]?.Value;
                        string resolvedValue = "";
                        string sourceType = "Dynamic";

                        if (string.IsNullOrEmpty(valueRef))
                        {
                            resolvedValue = "(Clear)";
                            sourceType = "Static";
                        }
                        else
                        {
                            string varName = valueRef.Trim('[', ']');
                            if (_variableMap != null && _variableMap.TryGetValue(varName, out string mapped))
                            {
                                resolvedValue = mapped;
                            }
                            else
                            {
                                // Fallback: show the target type
                                string targetType = ExtractTargetTypeName(sibling);
                                resolvedValue = !string.IsNullOrEmpty(targetType) ? "(" + targetType + ")" : "(Dynamic)";
                            }
                        }

                        fields.Add(new ClassicWorkflowFieldAssignment
                        {
                            FieldName = attr,
                            Value = resolvedValue,
                            SourceType = sourceType
                        });
                    }
                }
            }
        }

        // ── Helpers ──

        private static string ExtractTypeName(string assemblyQualifiedName)
        {
            string fullType = assemblyQualifiedName.Split(',')[0].Trim();
            int lastDot = fullType.LastIndexOf('.');
            return lastDot >= 0 ? fullType.Substring(lastDot + 1) : fullType;
        }

        private static string ExtractDisplayNameDescription(XmlNode node)
        {
            string displayName = node.Attributes?["DisplayName"]?.Value;
            if (string.IsNullOrEmpty(displayName)) return null;

            // Pattern: "UpdateStep3: Update Change Request" → "Update Change Request"
            int colonIndex = displayName.IndexOf(':');
            if (colonIndex >= 0)
            {
                string desc = displayName.Substring(colonIndex + 1).Trim();
                if (!string.IsNullOrEmpty(desc)) return desc;
            }
            return displayName;
        }

        private static string ExtractStepIdFromDisplayName(XmlNode node)
        {
            string displayName = node.Attributes?["DisplayName"]?.Value;
            if (string.IsNullOrEmpty(displayName)) return null;

            int colonIndex = displayName.IndexOf(':');
            return colonIndex >= 0 ? displayName.Substring(0, colonIndex).Trim() : displayName;
        }

        /// <summary>
        /// Extracts the user-provided step description from the parent Sequence's Variables.
        /// Steps are wrapped in a Sequence element that contains Variables like:
        ///   Variable Name="stepLabelDescription" Default="User's description text"
        /// </summary>
        private static string ExtractStepDescription(XmlNode stepNode)
        {
            // The step element (e.g., TerminateWorkflow, UpdateEntity) is inside a Sequence.
            // Check the parent and grandparent for Sequence.Variables containing stepLabelDescription.
            XmlNode sequenceNode = stepNode.ParentNode;
            if (sequenceNode == null) return null;

            // The parent might be the Sequence itself, or we might need to go up one more level
            for (int depth = 0; depth < 2; depth++)
            {
                if (sequenceNode == null) break;
                if (sequenceNode.LocalName == "Sequence")
                {
                    foreach (XmlNode child in sequenceNode.ChildNodes)
                    {
                        if (child.LocalName == "Sequence.Variables")
                        {
                            foreach (XmlNode variable in child.ChildNodes)
                            {
                                if (variable.LocalName == "Variable" &&
                                    variable.Attributes?["Name"]?.Value == "stepLabelDescription")
                                {
                                    string desc = variable.Attributes?["Default"]?.Value;
                                    if (!string.IsNullOrEmpty(desc))
                                        return desc;
                                }
                            }
                        }
                    }
                    return null; // Found Sequence but no description
                }
                sequenceNode = sequenceNode.ParentNode;
            }
            return null;
        }

        private static string ExtractOptionSetValue(XmlNode node, string propertyName)
        {
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.LocalName == "SetState." + propertyName || child.LocalName.EndsWith("." + propertyName))
                {
                    return FindOptionSetValue(child);
                }
            }
            return null;
        }

        private static string FindOptionSetValue(XmlNode node)
        {
            if (node == null) return null;
            if (node.LocalName == "OptionSetValue")
                return node.Attributes?["Value"]?.Value;
            foreach (XmlNode child in node.ChildNodes)
            {
                string result = FindOptionSetValue(child);
                if (result != null) return result;
            }
            return null;
        }

        private static string ExtractTargetTypeName(XmlNode setEntityPropertyNode)
        {
            foreach (XmlNode child in setEntityPropertyNode.ChildNodes)
            {
                if (child.LocalName == "SetEntityProperty.TargetType" || child.LocalName.EndsWith(".TargetType"))
                    return FindReferenceLiteralValue(child);
            }
            return null;
        }

        private static string FindReferenceLiteralValue(XmlNode node)
        {
            if (node == null) return null;
            if (node.LocalName == "ReferenceLiteral")
            {
                string val = node.Attributes?["Value"]?.Value;
                if (!string.IsNullOrEmpty(val))
                {
                    int colonIdx = val.IndexOf(':');
                    return colonIdx >= 0 ? val.Substring(colonIdx + 1) : val;
                }
            }
            foreach (XmlNode child in node.ChildNodes)
            {
                string result = FindReferenceLiteralValue(child);
                if (result != null) return result;
            }
            return null;
        }

        private static XmlNode FindActivitiesCollection(XmlNode activityRefNode)
        {
            foreach (XmlNode child in activityRefNode.ChildNodes)
            {
                if (child.LocalName == "ActivityReference.Properties")
                {
                    foreach (XmlNode prop in child.ChildNodes)
                    {
                        string key = prop.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(prop, "Key");
                        if (key == "Activities")
                            return prop;
                    }
                }
                string directKey = child.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(child, "Key");
                if (directKey == "Activities")
                    return child;
            }
            return null;
        }

        private static XmlNode FindPropertyNode(XmlNode activityRefNode, string propertyKey)
        {
            foreach (XmlNode child in activityRefNode.ChildNodes)
            {
                if (child.LocalName == "ActivityReference.Properties")
                {
                    foreach (XmlNode prop in child.ChildNodes)
                    {
                        string key = prop.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(prop, "Key");
                        if (key == propertyKey)
                            return prop;
                    }
                }
                string directKey = child.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(child, "Key");
                if (directKey == propertyKey)
                    return child;
            }
            return null;
        }

        private static bool IsSkippableStructuralNode(XmlNode node)
        {
            string localName = node.LocalName;
            return localName == "Variable" ||
                   localName == "Members" ||
                   localName == "Property" ||
                   localName == "VisualBasic.Settings" ||
                   localName == "Persist" ||
                   localName == "GetEntityProperty" ||
                   localName == "RetrieveEntity" ||
                   localName == "SetEntityProperty";
        }

        private static string GetAttributeByLocalName(XmlNode node, string localName)
        {
            if (node.Attributes == null) return null;
            foreach (XmlAttribute attr in node.Attributes)
            {
                if (attr.LocalName == localName)
                    return attr.Value;
            }
            return null;
        }

        // ── Table reference building ──

        private static void BuildTableReferences(ClassicWorkflowEntity workflow)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(workflow.PrimaryEntity))
            {
                string key = workflow.PrimaryEntity + "|Trigger";
                if (seen.Add(key))
                {
                    workflow.TableReferences.Add(new ClassicWorkflowTableReference
                    {
                        TableLogicalName = workflow.PrimaryEntity,
                        ReferenceType = ClassicWorkflowReferenceType.Trigger
                    });
                }
            }

            CollectTableReferencesFromSteps(workflow.Steps, workflow.TableReferences, seen);

            // Extract related entity references from the variable map
            // Variables like "{systemuser (Related).address1_composite}" indicate a Read on systemuser
            if (_variableMap != null)
            {
                foreach (var entry in _variableMap.Values)
                {
                    if (string.IsNullOrEmpty(entry) || !entry.Contains("(Related)")) continue;

                    // Pattern: {entityname (Related).fieldname} or just entityname (Related).fieldname
                    string clean = entry.Trim('{', '}');
                    int dotIdx = clean.IndexOf('.');
                    if (dotIdx <= 0) continue;

                    string entityPart = clean.Substring(0, dotIdx).Replace(" (Related)", "").Trim();
                    if (string.IsNullOrEmpty(entityPart)) continue;

                    string key = entityPart + "|Read";
                    if (seen.Add(key))
                    {
                        workflow.TableReferences.Add(new ClassicWorkflowTableReference
                        {
                            TableLogicalName = entityPart,
                            ReferenceType = ClassicWorkflowReferenceType.Read
                        });
                    }
                }
            }
        }

        private static void CollectTableReferencesFromSteps(List<ClassicWorkflowStep> steps,
            List<ClassicWorkflowTableReference> references, HashSet<string> seen)
        {
            foreach (var step in steps)
            {
                if (!string.IsNullOrEmpty(step.TargetEntity))
                {
                    ClassicWorkflowReferenceType refType = step.StepType switch
                    {
                        ClassicWorkflowStepType.CreateRecord => ClassicWorkflowReferenceType.Create,
                        ClassicWorkflowStepType.UpdateRecord => ClassicWorkflowReferenceType.Update,
                        ClassicWorkflowStepType.Assign => ClassicWorkflowReferenceType.Assign,
                        ClassicWorkflowStepType.ChangeStatus => ClassicWorkflowReferenceType.ChangeStatus,
                        ClassicWorkflowStepType.SendEmail => ClassicWorkflowReferenceType.SendEmail,
                        _ => ClassicWorkflowReferenceType.Read
                    };

                    string key = step.TargetEntity + "|" + refType;
                    if (seen.Add(key))
                    {
                        references.Add(new ClassicWorkflowTableReference
                        {
                            TableLogicalName = step.TargetEntity,
                            ReferenceType = refType
                        });
                    }
                }

                if (step.ChildSteps.Count > 0)
                    CollectTableReferencesFromSteps(step.ChildSteps, references, seen);
            }
        }
    }
}
