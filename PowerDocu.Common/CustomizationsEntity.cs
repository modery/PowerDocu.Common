using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace PowerDocu.Common
{
    //this class is complementary to the SolutionEntity class. At some point in the future, the information from the customizations.xml should move into SolutionEntity
    public class CustomizationsEntity
    {
        public XmlNode customizationsXml;

        public string getAppNameBySchemaName(string schemaName) {
            return customizationsXml.SelectSingleNode("/ImportExportXml/CanvasApps/CanvasApp[Name='"+schemaName+"']/DisplayName")?.InnerText;
        }

        public string getFlowNameById(string ID) {
            return customizationsXml.SelectSingleNode("/ImportExportXml/Workflows/Workflow[@WorkflowId='"+ID+"']")?.Attributes.GetNamedItem("Name").InnerText;
        }
    }
}