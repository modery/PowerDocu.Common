using System.Collections.Generic;

namespace PowerDocu.Common
{
    public class FlowEntity
    {
        public enum FlowType
        {
            CloudFlow,
            DesktopFlow,
            BusinessProcessFlow,
            Unknown
        }

        // https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/workflow#BKMK_ModernFlowType
        public enum ModernFlowType
        {
            CloudFlow = 0,
            AgentFlow = 1,
            M365CopilotAgentFlow = 2
        }

        public static string GetModernFlowTypeLabel(ModernFlowType type)
        {
            return type switch
            {
                ModernFlowType.AgentFlow => "Agent Flow",
                ModernFlowType.M365CopilotAgentFlow => "M365 Copilot Agent Flow",
                _ => "Cloud Flow"
            };
        }

        public string ID;
        public string Name;
        public string Description;
        public FlowType flowType;
        public ModernFlowType modernFlowType = ModernFlowType.CloudFlow;
        public Trigger trigger;
        public ActionGraph actions = new ActionGraph();
        public List<ConnectionReference> connectionReferences = new List<ConnectionReference>();

        public FlowEntity()
        {
        }

        public void addTrigger(string name)
        {
            this.trigger = new Trigger(name);
        }
    }
}
