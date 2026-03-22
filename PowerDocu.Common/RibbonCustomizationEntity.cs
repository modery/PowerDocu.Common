using System.Collections.Generic;

namespace PowerDocu.Common
{
    public class RibbonCustomizationEntity
    {
        public string EntityName;
        public List<string> HiddenActions = new List<string>();
        public int CommandDefinitionCount;
        public int DisplayRuleCount;
        public int EnableRuleCount;
    }
}
