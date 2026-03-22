using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace PowerDocu.Common
{
    public static class BPFXamlParser
    {
        public static void ParseBPFXaml(BPFEntity bpf, Stream xamlStream)
        {
            XmlDocument doc = new XmlDocument { XmlResolver = null };
            using (StreamReader reader = new StreamReader(xamlStream))
            {
                doc.LoadXml(reader.ReadToEnd());
            }
            ParseBPFXaml(bpf, doc);
        }

        public static void ParseBPFXaml(BPFEntity bpf, string xamlContent)
        {
            if (string.IsNullOrEmpty(xamlContent)) return;
            XmlDocument doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(xamlContent);
            ParseBPFXaml(bpf, doc);
        }

        private static void ParseBPFXaml(BPFEntity bpf, XmlDocument doc)
        {
            // The XAML uses CLR namespace prefixes. We need to set up a namespace manager
            // to handle XPath queries. But the namespace URIs in the XAML are CLR-based,
            // not standard URIs. We'll use recursive traversal instead of XPath for robustness.
            
            // Find all ActivityReference nodes representing EntityComposite
            List<XmlNode> entityComposites = new List<XmlNode>();
            FindActivityReferences(doc.DocumentElement, "EntityComposite", entityComposites);

            foreach (XmlNode entityNode in entityComposites)
            {
                string entityName = ExtractEntityNameFromDisplayName(entityNode);

                // Find StageComposite nodes within this EntityComposite
                List<XmlNode> stageComposites = new List<XmlNode>();
                FindActivityReferences(entityNode, "StageComposite", stageComposites);

                foreach (XmlNode stageNode in stageComposites)
                {
                    BPFStage stage = ParseStage(stageNode, entityName);
                    if (stage != null)
                    {
                        bpf.Stages.Add(stage);
                    }
                }
            }
        }

        private static BPFStage ParseStage(XmlNode stageNode, string entityName)
        {
            BPFStage stage = new BPFStage
            {
                EntityName = entityName,
                Name = ExtractStageNameFromDisplayName(stageNode),
                StageId = GetPropertyValue(stageNode, "StageId"),
                NextStageId = GetPropertyValue(stageNode, "NextStageId"),
                StageCategory = int.TryParse(GetPropertyValue(stageNode, "StageCategory"), out int cat) ? cat : 0
            };

            // If name is empty, try to get it from StepLabels at the stage level
            if (string.IsNullOrEmpty(stage.Name) || stage.Name == "New Step")
            {
                string labelName = GetStepLabelDescription(stageNode);
                if (!string.IsNullOrEmpty(labelName))
                    stage.Name = labelName;
            }

            // Parse steps within the stage
            List<XmlNode> stepComposites = new List<XmlNode>();
            FindActivityReferences(stageNode, "StepComposite", stepComposites);

            foreach (XmlNode stepNode in stepComposites)
            {
                BPFStep step = ParseStep(stepNode);
                if (step != null)
                {
                    stage.Steps.Add(step);
                }
            }

            // Parse condition branches within the stage
            List<XmlNode> conditionSequences = new List<XmlNode>();
            FindActivityReferences(stageNode, "ConditionSequence", conditionSequences);

            foreach (XmlNode condNode in conditionSequences)
            {
                ParseConditionBranches(condNode, stage);
            }

            return stage;
        }

        private static BPFStep ParseStep(XmlNode stepNode)
        {
            BPFStep step = new BPFStep
            {
                StepId = GetPropertyValue(stepNode, "ProcessStepId"),
                IsRequired = GetPropertyValue(stepNode, "IsProcessRequired")?.Equals("True", StringComparison.OrdinalIgnoreCase) == true,
                Description = GetStepLabelDescription(stepNode)
            };

            // Find Control elements within this step
            FindControls(stepNode, step.Controls);

            return step;
        }

        private static void ParseConditionBranches(XmlNode conditionSequenceNode, BPFStage stage)
        {
            // Look for ConditionBranch nodes
            List<XmlNode> conditionBranches = new List<XmlNode>();
            FindActivityReferences(conditionSequenceNode, "ConditionBranch", conditionBranches);

            foreach (XmlNode branchNode in conditionBranches)
            {
                // Look for SetNextStage elements within the branch
                FindSetNextStage(branchNode, stage, branchNode);
            }
        }

        private static void FindSetNextStage(XmlNode node, BPFStage stage, XmlNode branchNode)
        {
            if (node == null) return;

            // Check if this node is a SetNextStage element
            if (node.LocalName == "SetNextStage")
            {
                string parentStageId = node.Attributes?["ParentStageId"]?.Value;
                string targetStageId = node.Attributes?["StageId"]?.Value;
                string description = GetPropertyValue(branchNode, "Description");

                stage.ConditionBranches.Add(new BPFConditionBranch
                {
                    ParentStageId = parentStageId,
                    TargetStageId = targetStageId,
                    Description = description
                });
                return;
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                FindSetNextStage(child, stage, branchNode);
            }
        }

        private static void FindControls(XmlNode node, List<BPFControl> controls)
        {
            if (node == null) return;

            if (node.LocalName == "Control")
            {
                controls.Add(new BPFControl
                {
                    ClassId = node.Attributes?["ClassId"]?.Value,
                    DisplayName = node.Attributes?["ControlDisplayName"]?.Value,
                    ControlId = node.Attributes?["ControlId"]?.Value,
                    DataFieldName = node.Attributes?["DataFieldName"]?.Value,
                    IsSystemControl = node.Attributes?["IsSystemControl"]?.Value?.Equals("True", StringComparison.OrdinalIgnoreCase) == true,
                    IsUnbound = node.Attributes?["IsUnbound"]?.Value?.Equals("True", StringComparison.OrdinalIgnoreCase) == true
                });
                return;
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                FindControls(child, controls);
            }
        }

        private static void FindActivityReferences(XmlNode parent, string typeName, List<XmlNode> results)
        {
            if (parent == null) return;

            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child.LocalName == "ActivityReference" &&
                    child.Attributes?["AssemblyQualifiedName"]?.Value?.Contains(typeName) == true)
                {
                    results.Add(child);
                }
                else
                {
                    // Recurse but don't descend into other ActivityReference nodes of different types
                    // to avoid picking up nested items at the wrong level
                    FindActivityReferences(child, typeName, results);
                }
            }
        }

        private static string ExtractEntityNameFromDisplayName(XmlNode node)
        {
            // DisplayName pattern: "EntityStep2: admin_flow"
            string displayName = node.Attributes?["DisplayName"]?.Value;
            if (string.IsNullOrEmpty(displayName)) return null;
            int colonIndex = displayName.IndexOf(':');
            return colonIndex >= 0 ? displayName.Substring(colonIndex + 1).Trim() : displayName;
        }

        private static string ExtractStageNameFromDisplayName(XmlNode node)
        {
            // DisplayName pattern: "StageStep3: Validate Maker Business Requirements"
            string displayName = node.Attributes?["DisplayName"]?.Value;
            if (string.IsNullOrEmpty(displayName)) return null;
            int colonIndex = displayName.IndexOf(':');
            return colonIndex >= 0 ? displayName.Substring(colonIndex + 1).Trim() : displayName;
        }

        private static string GetPropertyValue(XmlNode activityRefNode, string key)
        {
            // Properties are stored as child elements with x:Key attributes
            // e.g., <x:String x:Key="StageId">af112cfd-...</x:String>
            // or <x:Null x:Key="NextStageId" />
            if (activityRefNode == null) return null;

            foreach (XmlNode child in activityRefNode.ChildNodes)
            {
                // Check direct children and Properties container children
                string result = FindPropertyByKey(child, key);
                if (result != null) return result;
            }
            return null;
        }

        private static string FindPropertyByKey(XmlNode node, string key)
        {
            if (node == null) return null;

            // Check if this node has the x:Key attribute matching our key
            string xKey = node.Attributes?["x:Key"]?.Value ?? 
                          GetAttributeByLocalName(node, "Key");
            if (xKey == key)
            {
                // x:Null means null value
                if (node.LocalName == "Null") return null;
                return node.InnerText;
            }

            // Also check Description attribute on ConditionBranch-like nodes
            if (key == "Description")
            {
                string desc = node.Attributes?["Description"]?.Value; 
                if (desc != null) return desc;
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                string result = FindPropertyByKey(child, key);
                if (result != null) return result;
            }

            return null;
        }

        private static string GetStepLabelDescription(XmlNode node)
        {
            // StepLabels are stored as:
            // <sco:Collection x:TypeArguments="mcwo:StepLabel" x:Key="StepLabels">
            //   <mcwo:StepLabel Description="..." LabelId="..." LanguageCode="1033" />
            // </sco:Collection>
            if (node == null) return null;

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.LocalName == "StepLabel")
                {
                    return child.Attributes?["Description"]?.Value;
                }

                // Look for Collection elements that might contain StepLabels
                string keyAttr = child.Attributes?["x:Key"]?.Value ?? GetAttributeByLocalName(child, "Key");
                if (keyAttr == "StepLabels")
                {
                    foreach (XmlNode labelChild in child.ChildNodes)
                    {
                        if (labelChild.LocalName == "StepLabel")
                        {
                            return labelChild.Attributes?["Description"]?.Value;
                        }
                    }
                }

                string result = GetStepLabelDescription(child);
                if (result != null) return result;
            }
            return null;
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
    }
}
