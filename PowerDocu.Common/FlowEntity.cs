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

        public string ID;
        public string Name;
        public string Description;
        public FlowType flowType;
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
