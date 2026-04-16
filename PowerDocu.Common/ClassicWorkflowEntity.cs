using System.Collections.Generic;
using System.Text;

namespace PowerDocu.Common
{
    public class ClassicWorkflowEntity
    {
        public string ID;
        public string Name;
        public string UniqueName;
        public string Description;
        public string PrimaryEntity;
        public int Category;
        public int Mode;       // 0=Background, 1=Real-time
        public int Scope;      // 1=User, 2=Business Unit, 3=Parent-Child BU, 4=Organization
        public bool OnDemand;
        public bool TriggerOnCreate;
        public bool TriggerOnDelete;
        public string TriggerOnUpdateAttributeList;
        public int StateCode;
        public int StatusCode;
        public string IntroducedVersion;
        public bool IsCustomizable;
        public int Rank;       // 0=Calling User, 1=Owner
        public string OwnerId;
        public string XamlFileName;
        public Dictionary<string, string> LocalizedNames = new Dictionary<string, string>();
        public Dictionary<string, string> Descriptions = new Dictionary<string, string>();
        public List<ClassicWorkflowStep> Steps = new List<ClassicWorkflowStep>();
        public List<ClassicWorkflowTableReference> TableReferences = new List<ClassicWorkflowTableReference>();

        public string GetDisplayName()
        {
            if (LocalizedNames.ContainsKey("1033"))
                return LocalizedNames["1033"];
            if (!string.IsNullOrEmpty(Name))
                return Name;
            return UniqueName ?? ID;
        }

        public string GetStateLabel()
        {
            return StateCode switch
            {
                0 => "Draft",
                1 => "Active",
                2 => "Suspended",
                _ => "Unknown"
            };
        }

        public string GetModeLabel()
        {
            return Mode switch
            {
                0 => "Background",
                1 => "Real-time",
                _ => "Unknown"
            };
        }

        public string GetScopeLabel()
        {
            return Scope switch
            {
                1 => "User",
                2 => "Business Unit",
                3 => "Parent-Child Business Units",
                4 => "Organization",
                _ => "Unknown"
            };
        }

        public string GetRunAsLabel()
        {
            return Rank switch
            {
                1 => "Owner",
                _ => "Calling User"
            };
        }

        public string GetCategoryLabel()
        {
            return Category switch
            {
                0 => "Workflow",
                1 => "Dialog",
                2 => "Business Rule",
                3 => "Action",
                4 => "Business Process Flow",
                5 => "Modern Flow",
                6 => "Desktop Flow",
                _ => "Unknown (" + Category + ")"
            };
        }

        public string GetTriggerDescription()
        {
            var parts = new List<string>();
            if (OnDemand)
                parts.Add("On-Demand");
            if (TriggerOnCreate)
                parts.Add("Record Created");
            if (TriggerOnDelete)
                parts.Add("Record Deleted");
            if (!string.IsNullOrEmpty(TriggerOnUpdateAttributeList))
                parts.Add("Record Updated (" + TriggerOnUpdateAttributeList + ")");

            return parts.Count > 0 ? string.Join(", ", parts) : "None";
        }
    }

    public enum ClassicWorkflowStepType
    {
        CheckCondition,
        ConditionBranch,
        CreateRecord,
        UpdateRecord,
        SendEmail,
        Assign,
        ChangeStatus,
        Stop,
        ChildWorkflow,
        Wait,
        SetVisibility,
        SetDisplayMode,
        SetFieldRequired,
        SetAttributeValue,
        SetDefaultValue,
        SetMessage,
        Custom
    }

    public class ClassicWorkflowStep
    {
        public string StepId;
        public string Name;
        public ClassicWorkflowStepType StepType;
        public string TargetEntity;
        public string CustomActivityName;   // For Custom step type: short class name (e.g., "ToFormattedString")
        public string CustomActivityClass;  // Full class name (e.g., "MARCY.Activities.DateTimes.ToFormattedString")
        public string CustomActivityAssembly; // Assembly name (e.g., "MARCY.Activities, Version=1.0.0.0, PublicKeyToken=2fb77a038cccb985")
        public string CustomActivityFriendlyName; // Registered friendly name
        public string CustomActivityDescription;  // Registered description
        public string CustomActivityGroupName;    // Registered group name
        public string ConditionDescription; // Simple flat text fallback
        public ConditionExpression ConditionTree; // Structured condition tree for rich rendering
        public string StepDescription;  // User-provided step description from stepLabelDescription
        public List<ClassicWorkflowFieldAssignment> Fields = new List<ClassicWorkflowFieldAssignment>();
        public List<ClassicWorkflowStep> ChildSteps = new List<ClassicWorkflowStep>();
        public int NestingLevel;

        public string GetStepTypeLabel()
        {
            return StepType switch
            {
                ClassicWorkflowStepType.CheckCondition => "Check Condition",
                ClassicWorkflowStepType.ConditionBranch => "Condition Branch",
                ClassicWorkflowStepType.CreateRecord => "Create Record",
                ClassicWorkflowStepType.UpdateRecord => "Update Record",
                ClassicWorkflowStepType.SendEmail => "Send Email",
                ClassicWorkflowStepType.Assign => "Assign",
                ClassicWorkflowStepType.ChangeStatus => "Change Status",
                ClassicWorkflowStepType.Stop => "Stop",
                ClassicWorkflowStepType.ChildWorkflow => "Child Workflow",
                ClassicWorkflowStepType.Wait => "Wait",
                ClassicWorkflowStepType.SetVisibility => "Set Visibility",
                ClassicWorkflowStepType.SetDisplayMode => "Set Display Mode",
                ClassicWorkflowStepType.SetFieldRequired => "Set Field Required",
                ClassicWorkflowStepType.SetAttributeValue => "Set Attribute Value",
                ClassicWorkflowStepType.SetDefaultValue => "Set Default Value",
                ClassicWorkflowStepType.SetMessage => "Set Message",
                ClassicWorkflowStepType.Custom => !string.IsNullOrEmpty(CustomActivityName) ? CustomActivityName : "Custom",
                _ => "Unknown"
            };
        }
    }

    public class ClassicWorkflowFieldAssignment
    {
        public string FieldName;
        public string Value;
        public string SourceType;   // Static, Dynamic, Lookup
    }

    public enum ClassicWorkflowReferenceType
    {
        Trigger,
        Create,
        Update,
        Read,
        Assign,
        ChangeStatus,
        ChildWorkflow,
        SendEmail
    }

    public class ClassicWorkflowTableReference
    {
        public string TableLogicalName;
        public ClassicWorkflowReferenceType ReferenceType;
    }

    /// <summary>
    /// Tree structure representing a workflow condition expression.
    /// Leaf nodes are comparisons (field operator value).
    /// Branch nodes are AND/OR groups containing child expressions.
    /// </summary>
    public class ConditionExpression
    {
        /// <summary>Logical grouping operator for branch nodes (AND/OR). Null for leaf nodes.</summary>
        public string LogicalOperator; // "AND", "OR", or null for leaf

        /// <summary>For leaf: the entity/field being evaluated (e.g., "msf_changerequests.msf_issmdecision")</summary>
        public string Field;

        /// <summary>For leaf: the comparison operator (e.g., "Equals", "Contains Data")</summary>
        public string Operator;

        /// <summary>For leaf: the comparison value (e.g., "100000002", or null for unary ops like NotNull)</summary>
        public string Value;

        /// <summary>Child expressions for AND/OR groups</summary>
        public List<ConditionExpression> Children = new List<ConditionExpression>();

        public bool IsLeaf => string.IsNullOrEmpty(LogicalOperator);
        public bool IsGroup => !IsLeaf;

        /// <summary>Renders the condition tree as a flat human-readable string (fallback)</summary>
        public string ToFlatString()
        {
            if (IsLeaf)
            {
                return string.IsNullOrEmpty(Value)
                    ? $"{Field} {Operator}"
                    : $"{Field} {Operator} {Value}";
            }

            var parts = new List<string>();
            foreach (var child in Children)
            {
                string childStr = child.IsGroup && child.Children.Count > 1
                    ? "(" + child.ToFlatString() + ")"
                    : child.ToFlatString();
                parts.Add(childStr);
            }
            return string.Join($" {LogicalOperator} ", parts);
        }

        /// <summary>
        /// Renders the condition tree as indented lines for display.
        /// Each line is a tuple of (indentLevel, text).
        /// </summary>
        public List<(int indent, string text)> ToIndentedLines(int baseIndent = 0)
        {
            var lines = new List<(int indent, string text)>();

            if (IsLeaf)
            {
                string line = string.IsNullOrEmpty(Value)
                    ? $"{Field} {Operator}"
                    : $"{Field} {Operator} {Value}";
                lines.Add((baseIndent, line));
            }
            else
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (child.IsGroup)
                    {
                        // Group header
                        lines.Add((baseIndent, $"Group ({child.LogicalOperator}):"));
                        lines.AddRange(child.ToIndentedLines(baseIndent + 1));
                    }
                    else
                    {
                        lines.AddRange(child.ToIndentedLines(baseIndent));
                    }
                    // Add the operator between children (not after the last)
                    if (i < Children.Count - 1)
                    {
                        lines.Add((baseIndent, LogicalOperator));
                    }
                }
            }

            return lines;
        }
    }
}
