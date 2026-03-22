using System.Collections.Generic;

namespace PowerDocu.Common
{
    /// <summary>
    /// Represents a Business Process Flow extracted from a solution.
    /// Metadata comes from customizations.xml (Category=4 workflows), stage/step
    /// structure comes from the companion XAML file in the Workflows folder.
    /// </summary>
    public class BPFEntity
    {
        public string ID;
        public string Name;
        public string UniqueName;
        public string Description;
        public string PrimaryEntity;
        public string XamlFileName;
        public int BusinessProcessType;
        public int StateCode;
        public int StatusCode;
        public string IntroducedVersion;
        public bool IsCustomizable;
        public bool TriggerOnCreate;
        public List<BPFStage> Stages = new List<BPFStage>();
        public Dictionary<string, string> LocalizedNames = new Dictionary<string, string>();
        public Dictionary<string, string> Descriptions = new Dictionary<string, string>();
        public Dictionary<string, string> StageLabels = new Dictionary<string, string>();

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
    }

    public class BPFStage
    {
        public string StageId;
        public string Name;
        public string EntityName;
        public int StageCategory;
        public string NextStageId;
        public List<BPFStep> Steps = new List<BPFStep>();
        public List<BPFConditionBranch> ConditionBranches = new List<BPFConditionBranch>();

        public string GetStageCategoryLabel()
        {
            return StageCategory switch
            {
                0 => "Qualify",
                1 => "Develop",
                2 => "Propose",
                3 => "Close",
                4 => "Identify",
                5 => "Research",
                6 => "Resolve",
                7 => "Approval",
                _ => "Stage " + StageCategory
            };
        }
    }

    public class BPFStep
    {
        public string StepId;
        public string Description;
        public bool IsRequired;
        public List<BPFControl> Controls = new List<BPFControl>();
    }

    public class BPFControl
    {
        public string ControlId;
        public string DisplayName;
        public string DataFieldName;
        public string ClassId;
        public bool IsSystemControl;
        public bool IsUnbound;
    }

    public class BPFConditionBranch
    {
        public string Description;
        public string ParentStageId;
        public string TargetStageId;
    }
}
