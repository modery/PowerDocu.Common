using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerDocu.Common
{
    public static class RobinScriptParser
    {
        public static void ParseRobinScript(string robinScript, DesktopFlowEntity flow)
        {
            if (string.IsNullOrEmpty(robinScript)) return;

            // Unescape the script (stored as a JSON-style quoted string with escaped chars and XML entities)
            string script = robinScript.Trim('"');
            script = script.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\t", "\t");
            script = script.Replace("\\'", "'");
            script = script.Replace("\\\\", "\\");
            script = script.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&#39;", "'").Replace("&apos;", "'").Replace("&amp;", "&");
            flow.RobinScript = script;

            string[] lines = script.Split('\n');
            int actionOrder = 0;
            int nestingLevel = 0;
            var sensitiveVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Current scope: main flow or a subflow
            List<RobinActionStep> currentActionSteps = flow.ActionSteps;
            List<RobinVariable> currentVariables = flow.Variables;
            List<RobinControlFlowBlock> currentControlFlowBlocks = flow.ControlFlowBlocks;
            DesktopFlowSubflow currentSubflow = null;
            StringBuilder subflowScriptBuilder = null;
            // Collect main flow script lines (everything outside FUNCTION blocks)
            StringBuilder mainFlowScriptBuilder = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string rawLine = lines[i].TrimEnd('\r');
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // FUNCTION block start: FUNCTION SubflowName [GLOBAL]
                if (line.StartsWith("FUNCTION "))
                {
                    var funcMatch = Regex.Match(line, @"^FUNCTION\s+(\w+)(\s+GLOBAL)?");
                    if (funcMatch.Success)
                    {
                        currentSubflow = new DesktopFlowSubflow
                        {
                            Name = funcMatch.Groups[1].Value,
                            IsGlobal = funcMatch.Groups[2].Success
                        };
                        flow.Subflows.Add(currentSubflow);
                        currentActionSteps = currentSubflow.ActionSteps;
                        currentVariables = currentSubflow.Variables;
                        currentControlFlowBlocks = currentSubflow.ControlFlowBlocks;
                        subflowScriptBuilder = new StringBuilder();
                        actionOrder = 0;
                        nestingLevel = 0;
                        continue;
                    }
                }

                // END FUNCTION: return to main scope
                if (line == "END FUNCTION")
                {
                    if (currentSubflow != null)
                    {
                        currentSubflow.RobinScript = subflowScriptBuilder.ToString();
                        currentSubflow = null;
                        subflowScriptBuilder = null;
                        currentActionSteps = flow.ActionSteps;
                        currentVariables = flow.Variables;
                        currentControlFlowBlocks = flow.ControlFlowBlocks;
                        actionOrder = flow.ActionSteps.Count;
                        nestingLevel = 0;
                    }
                    continue;
                }

                // Collect script lines for the current scope
                if (currentSubflow != null)
                    subflowScriptBuilder.AppendLine(rawLine);
                else
                    mainFlowScriptBuilder.AppendLine(rawLine);

                // EXIT FUNCTION: treat as an action step within the subflow
                if (line == "EXIT FUNCTION")
                {
                    actionOrder++;
                    currentActionSteps.Add(new RobinActionStep
                    {
                        Order = actionOrder,
                        ModuleName = "Flow",
                        ActionName = "ExitFunction",
                        SubActionName = "",
                        FullActionName = "EXIT FUNCTION",
                        RawScript = line,
                        NestingLevel = nestingLevel
                    });
                    continue;
                }

                // Connection string directives (@@Key: 'Value')
                if (line.StartsWith("@@"))
                {
                    ParseDirective(line, flow);
                    continue;
                }

                // @SENSITIVE tag
                if (line.StartsWith("@SENSITIVE:"))
                {
                    ParseSensitiveTag(line, sensitiveVars);
                    continue;
                }

                // @OUTPUT declaration
                if (line.StartsWith("@OUTPUT "))
                {
                    ParseOutputVariable(line, currentVariables);
                    continue;
                }

                // @INPUT declaration
                if (line.StartsWith("@INPUT "))
                {
                    ParseInputVariable(line, currentVariables);
                    continue;
                }

                // IMPORT statement
                if (line.StartsWith("IMPORT "))
                {
                    ParseImport(line, flow);
                    continue;
                }

                // Comments
                if (line.StartsWith("#"))
                    continue;

                // Source annotations
                if (line.StartsWith("@@source:"))
                    continue;

                // Control flow: END (reduce nesting)
                if (line == "END")
                {
                    nestingLevel = Math.Max(0, nestingLevel - 1);
                    continue;
                }

                // Control flow: LOOP
                if (line.StartsWith("LOOP "))
                {
                    currentControlFlowBlocks.Add(new RobinControlFlowBlock
                    {
                        Type = "LOOP",
                        Condition = line.Substring(5).Trim(),
                        StartLine = i,
                        NestingLevel = nestingLevel
                    });
                    nestingLevel++;
                    continue;
                }

                // Control flow: IF
                if (line.StartsWith("IF "))
                {
                    currentControlFlowBlocks.Add(new RobinControlFlowBlock
                    {
                        Type = "IF",
                        Condition = line.StartsWith("IF ") ? line.Substring(3).TrimEnd() : line,
                        StartLine = i,
                        NestingLevel = nestingLevel
                    });
                    nestingLevel++;
                    continue;
                }

                // Control flow: ELSE
                if (line == "ELSE")
                {
                    currentControlFlowBlocks.Add(new RobinControlFlowBlock
                    {
                        Type = "ELSE",
                        StartLine = i,
                        NestingLevel = Math.Max(0, nestingLevel - 1)
                    });
                    continue;
                }

                // Control flow: ON ERROR
                if (line.StartsWith("ON ERROR"))
                {
                    currentControlFlowBlocks.Add(new RobinControlFlowBlock
                    {
                        Type = "ON ERROR",
                        Condition = line.Length > 8 ? line.Substring(8).Trim() : "",
                        StartLine = i,
                        NestingLevel = nestingLevel
                    });
                    nestingLevel++;
                    continue;
                }

                // LABEL statement
                if (line.StartsWith("LABEL "))
                    continue;

                // GOTO statement
                if (line.StartsWith("GOTO "))
                    continue;

                // WAIT command
                if (line.StartsWith("WAIT "))
                {
                    actionOrder++;
                    currentActionSteps.Add(new RobinActionStep
                    {
                        Order = actionOrder,
                        ModuleName = "System",
                        ActionName = "Wait",
                        SubActionName = "",
                        FullActionName = "WAIT",
                        RawScript = line,
                        NestingLevel = nestingLevel,
                        Parameters = new Dictionary<string, string> { { "Duration", line.Substring(5).Trim() } }
                    });
                    continue;
                }

                // SET variable assignment
                if (line.StartsWith("SET "))
                {
                    actionOrder++;
                    ParseSetStatement(line, actionOrder, nestingLevel, currentActionSteps, currentVariables);
                    continue;
                }

                // CALL statement (subflow calls)
                if (line.StartsWith("CALL "))
                {
                    actionOrder++;
                    currentActionSteps.Add(new RobinActionStep
                    {
                        Order = actionOrder,
                        ModuleName = "Flow",
                        ActionName = "Call",
                        SubActionName = line.Substring(5).Trim(),
                        FullActionName = line,
                        RawScript = line,
                        NestingLevel = nestingLevel
                    });
                    continue;
                }

                // DISABLE prefix (disabled actions)
                string activeLine = line;
                bool isDisabled = false;
                if (line.StartsWith("DISABLE "))
                {
                    activeLine = line.Substring(8);
                    isDisabled = true;
                }

                // Module.Action.SubAction pattern (the main action lines)
                if (Regex.IsMatch(activeLine, @"^[A-Za-z]+\.[A-Za-z]"))
                {
                    actionOrder++;
                    ParseModuleAction(activeLine, actionOrder, nestingLevel, isDisabled, currentActionSteps, currentVariables);
                    continue;
                }

                // Variables.IncreaseVariable and similar
                if (activeLine.StartsWith("Variables."))
                {
                    actionOrder++;
                    ParseModuleAction(activeLine, actionOrder, nestingLevel, isDisabled, currentActionSteps, currentVariables);
                    continue;
                }
            }

            // Store the main flow's own script (excluding subflow FUNCTION blocks)
            // flow.RobinScript is the full script; we keep it as-is for backward compatibility

            // Mark sensitive variables (main flow)
            foreach (var variable in flow.Variables)
            {
                if (sensitiveVars.Contains(variable.Name))
                    variable.IsSensitive = true;
            }
            // Mark sensitive variables in subflows
            foreach (var subflow in flow.Subflows)
            {
                foreach (var variable in subflow.Variables)
                {
                    if (sensitiveVars.Contains(variable.Name))
                        variable.IsSensitive = true;
                }
            }

            // Extract unique module names from action steps (for flows without ManifestFile)
            var moduleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in flow.ActionSteps)
            {
                if (!string.IsNullOrEmpty(action.ModuleName) && action.ModuleName != "System" && action.ModuleName != "Flow")
                    moduleNames.Add(action.ModuleName);
            }
            foreach (var subflow in flow.Subflows)
            {
                foreach (var action in subflow.ActionSteps)
                {
                    if (!string.IsNullOrEmpty(action.ModuleName) && action.ModuleName != "System" && action.ModuleName != "Flow")
                        moduleNames.Add(action.ModuleName);
                }
            }
            // Only add modules not already in the manifest list
            var existingModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in flow.Modules)
                existingModules.Add(m.Name);
            foreach (string moduleName in moduleNames)
            {
                if (!existingModules.Contains(moduleName))
                {
                    flow.Modules.Add(new DesktopFlowModuleReference
                    {
                        Name = moduleName,
                        AssemblyName = "",
                        Version = ""
                    });
                }
            }
        }

        private static void ParseDirective(string line, DesktopFlowEntity flow)
        {
            // @@Key: 'Value'
            int colonIndex = line.IndexOf(':');
            if (colonIndex > 2)
            {
                string key = line.Substring(2, colonIndex - 2).Trim();
                string value = line.Substring(colonIndex + 1).Trim().Trim('\'');
                flow.ConnectionStrings[key] = value;
            }
        }

        private static void ParseSensitiveTag(string line, HashSet<string> sensitiveVars)
        {
            // @SENSITIVE: [Var1, Var2]
            var match = Regex.Match(line, @"\[(.+?)\]");
            if (match.Success)
            {
                foreach (string varName in match.Groups[1].Value.Split(','))
                {
                    string trimmed = varName.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        sensitiveVars.Add(trimmed);
                }
            }
        }

        private static void ParseOutputVariable(string line, List<RobinVariable> variables)
        {
            // @OUTPUT VarName : { 'Description': '', 'FriendlyName': 'VarName', 'Type': 'String' }
            var match = Regex.Match(line, @"@OUTPUT\s+(\w+)\s*:\s*\{(.+)\}");
            if (match.Success)
            {
                string varName = match.Groups[1].Value;
                string propsJson = match.Groups[2].Value;
                string type = ExtractJsonProperty(propsJson, "Type");
                string friendlyName = ExtractJsonProperty(propsJson, "FriendlyName");

                var existing = variables.Find(v => v.Name == varName);
                if (existing != null)
                {
                    existing.IsOutput = true;
                    if (!string.IsNullOrEmpty(type)) existing.Type = type;
                }
                else
                {
                    variables.Add(new RobinVariable
                    {
                        Name = varName,
                        Type = type,
                        IsOutput = true
                    });
                }
            }
        }

        private static void ParseInputVariable(string line, List<RobinVariable> variables)
        {
            // @INPUT VarName : { 'Description': '', 'FriendlyName': 'VarName', 'Type': 'String' }
            var match = Regex.Match(line, @"@INPUT\s+(\w+)\s*:\s*\{(.+)\}");
            if (match.Success)
            {
                string varName = match.Groups[1].Value;
                string propsJson = match.Groups[2].Value;
                string type = ExtractJsonProperty(propsJson, "Type");

                var existing = variables.Find(v => v.Name == varName);
                if (existing != null)
                {
                    existing.IsInput = true;
                    if (!string.IsNullOrEmpty(type)) existing.Type = type;
                }
                else
                {
                    variables.Add(new RobinVariable
                    {
                        Name = varName,
                        Type = type,
                        IsInput = true
                    });
                }
            }
        }

        private static void ParseImport(string line, DesktopFlowEntity flow)
        {
            // IMPORT 'controlRepo.appmask' AS appmask
            var match = Regex.Match(line, @"IMPORT\s+'(.+?)'\s+AS\s+(\w+)");
            if (match.Success)
            {
                flow.Imports.Add(new RobinImport
                {
                    Path = match.Groups[1].Value,
                    Alias = match.Groups[2].Value
                });
            }
        }

        private static void ParseSetStatement(string line, int order, int nestingLevel, List<RobinActionStep> actionSteps, List<RobinVariable> variables)
        {
            // SET VarName TO value
            var match = Regex.Match(line, @"SET\s+(\w+)\s+TO\s+(.+)");
            if (match.Success)
            {
                string varName = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                actionSteps.Add(new RobinActionStep
                {
                    Order = order,
                    ModuleName = "Variables",
                    ActionName = "Set",
                    SubActionName = varName,
                    FullActionName = $"SET {varName}",
                    RawScript = line,
                    NestingLevel = nestingLevel,
                    OutputVariables = new List<string> { varName }
                });

                // Track variable if not already known
                if (!variables.Exists(v => v.Name == varName))
                {
                    variables.Add(new RobinVariable { Name = varName, InitialValue = value });
                }
            }
        }

        private static void ParseModuleAction(string line, int order, int nestingLevel, bool isDisabled, List<RobinActionStep> actionSteps, List<RobinVariable> variables)
        {
            // Pattern: Module.Action.SubAction Param1: value1 Param2: value2 OutputVar=> VarName
            // Example: Excel.LaunchExcel.LaunchAndOpenUnderExistingProcess Path: $'''file.xlsx''' Instance=> ExcelInstance

            string[] parts = line.Split(new[] { ' ' }, 2);
            string actionPart = parts[0];
            string paramsPart = parts.Length > 1 ? parts[1] : "";

            string[] actionSegments = actionPart.Split('.');
            string moduleName = actionSegments.Length > 0 ? actionSegments[0] : "";
            string actionName = actionSegments.Length > 1 ? actionSegments[1] : "";
            string subActionName = actionSegments.Length > 2 ? actionSegments[2] : "";

            var step = new RobinActionStep
            {
                Order = order,
                ModuleName = moduleName,
                ActionName = actionName,
                SubActionName = subActionName,
                FullActionName = actionPart,
                RawScript = line,
                NestingLevel = nestingLevel
            };

            // Parse parameters and output variables from the params portion
            // Output variables use => pattern: VarName=> OutputVar
            var outputMatches = Regex.Matches(paramsPart, @"(\w+)=>\s*(\w+)");
            foreach (Match m in outputMatches)
            {
                step.OutputVariables.Add(m.Groups[2].Value);

                // Track output variable
                string outVarName = m.Groups[2].Value;
                if (!variables.Exists(v => v.Name == outVarName))
                {
                    variables.Add(new RobinVariable { Name = outVarName });
                }
            }

            // Parse named parameters (Key: Value pattern)
            // Robin Script parameters can have $'''...''' quoted string values, which may contain spaces and colons
            ParseParameters(paramsPart, step);

            actionSteps.Add(step);
        }

        private static string ExtractJsonProperty(string jsonFragment, string propertyName)
        {
            // Simple extraction from single-quoted JSON-like strings: 'PropertyName': 'Value'
            var match = Regex.Match(jsonFragment, $"'{propertyName}':\\s*'([^']*)'");
            return match.Success ? match.Groups[1].Value : "";
        }

        /// <summary>
        /// Parses Robin Script parameter string respecting $'''...''' quoted values.
        /// Parameters are in the format: Key1: value1 Key2: $'''complex value''' Key3: value3 OutputVar=> Var
        /// </summary>
        private static void ParseParameters(string paramsPart, RobinActionStep step)
        {
            if (string.IsNullOrWhiteSpace(paramsPart)) return;

            // Remove output variable assignments (already parsed) so they don't interfere
            string cleaned = Regex.Replace(paramsPart, @"\w+=>\s*\w+", " ").Trim();
            if (string.IsNullOrWhiteSpace(cleaned)) return;

            // Tokenize: split on parameter boundaries while respecting $'''...''' and [...] and {...} blocks
            int i = 0;
            string currentKey = null;
            int valueStart = -1;

            while (i < cleaned.Length)
            {
                // Check for a parameter key pattern: word followed by :
                // But only at a token boundary (start of string or after whitespace)
                if (currentKey == null)
                {
                    var keyMatch = Regex.Match(cleaned.Substring(i), @"^(\w+):\s*");
                    if (keyMatch.Success)
                    {
                        currentKey = keyMatch.Groups[1].Value;
                        i += keyMatch.Length;
                        valueStart = i;
                        continue;
                    }
                    i++;
                    continue;
                }

                // We're inside a value - look for the end
                // Skip $'''...''' blocks
                if (i + 3 <= cleaned.Length && cleaned.Substring(i).StartsWith("$'''"))
                {
                    int endQuote = cleaned.IndexOf("'''", i + 4, StringComparison.Ordinal);
                    if (endQuote >= 0)
                    {
                        i = endQuote + 3;
                        continue;
                    }
                }

                // Skip [...] blocks
                if (cleaned[i] == '[')
                {
                    i = SkipBracketBlock(cleaned, i, '[', ']');
                    continue;
                }

                // Skip {...} blocks
                if (cleaned[i] == '{')
                {
                    i = SkipBracketBlock(cleaned, i, '{', '}');
                    continue;
                }

                // Check if the next token is a new parameter key
                if (cleaned[i] == ' ')
                {
                    var nextKeyMatch = Regex.Match(cleaned.Substring(i), @"^\s+(\w+):\s");
                    if (nextKeyMatch.Success)
                    {
                        // Save current parameter
                        string value = cleaned.Substring(valueStart, i - valueStart).Trim();
                        if (!step.Parameters.ContainsKey(currentKey))
                            step.Parameters[currentKey] = value;
                        currentKey = null;
                        continue;
                    }
                }

                i++;
            }

            // Save the last parameter
            if (currentKey != null && valueStart >= 0)
            {
                string value = cleaned.Substring(valueStart).Trim();
                if (!step.Parameters.ContainsKey(currentKey))
                    step.Parameters[currentKey] = value;
            }
        }

        private static int SkipBracketBlock(string text, int start, char open, char close)
        {
            int depth = 0;
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] == open) depth++;
                else if (text[i] == close)
                {
                    depth--;
                    if (depth == 0) return i + 1;
                }
            }
            return text.Length;
        }
    }
}
