using System.IO;
using System.IO.Compression;
using System.Xml;

namespace PowerDocu.Common
{
    public class SolutionParser
    {
        public SolutionEntity solution;
        public SolutionParser(string filename)
        {
            NotificationHelper.SendNotification(" - Processing " + filename);
            if (filename.EndsWith(".zip"))
            {
                using FileStream stream = new FileStream(filename, FileMode.Open);
                ZipArchiveEntry solutionDefinition = ZipHelper.getSolutionDefinitionFileFromZip(stream);
                if (solutionDefinition != null)
                {
                    string tempFile = Path.GetDirectoryName(filename) + @"\" + solutionDefinition.Name;
                    solutionDefinition.ExtractToFile(tempFile, true);
                    NotificationHelper.SendNotification("  - Processing solution ");
                    using (FileStream appDefinition = new FileStream(tempFile, FileMode.Open))
                    {
                        {
                            parseSolutionDefinition(appDefinition);
                        }
                    }
                    File.Delete(tempFile);
                }
            }
            else
            {
                NotificationHelper.SendNotification("No solution definition found in " + filename);
            }
        }

        private void parseSolutionDefinition(Stream solutionArchive)
        {
            using StreamReader reader = new StreamReader(solutionArchive);
            string solutionXML = reader.ReadToEnd();
            XmlDocument solutionXmlDoc = new XmlDocument
            {
                XmlResolver = null
            };
            solutionXmlDoc.LoadXml(solutionXML);
            XmlNode solutionManifest = solutionXmlDoc.SelectSingleNode("/ImportExportXml/SolutionManifest");
            solution = new SolutionEntity
            {
                UniqueName = solutionManifest.SelectSingleNode("UniqueName").InnerText,
                Version = solutionManifest.SelectSingleNode("Version").InnerText,
                isManaged = solutionManifest.SelectSingleNode("Managed").InnerText.Equals("1"),
                Publisher = new SolutionPublisher()
                {
                    UniqueName = solutionManifest.SelectSingleNode("Publisher/UniqueName").InnerText,
                    EMailAddress = solutionManifest.SelectSingleNode("Publisher/EMailAddress").InnerText,
                    SupportingWebsiteUrl = solutionManifest.SelectSingleNode("Publisher/SupportingWebsiteUrl").InnerText,
                    CustomizationPrefix = solutionManifest.SelectSingleNode("Publisher/CustomizationPrefix").InnerText,
                    CustomizationOptionValuePrefix = solutionManifest.SelectSingleNode("Publisher/CustomizationOptionValuePrefix").InnerText
                }
            };
            //parsing the components
            foreach (XmlNode component in solutionManifest.SelectSingleNode("RootComponents").ChildNodes)
            {
                SolutionComponent solutionComponent = new SolutionComponent()
                {
                    SchemaName = component.Attributes.GetNamedItem("schemaName")?.InnerText,
                    ID = component.Attributes.GetNamedItem("id")?.InnerText,
                    Type = SolutionComponentHelper.GetComponentType(component.Attributes.GetNamedItem("type")?.InnerText)
                };
                solution.Components.Add(solutionComponent);
            }
            //parsing the dependencies
            //"<MissingDependency>
            //<Required type=\"1\" schemaName=\"admin_app\" displayName=\"PowerApps App\" solution=\"CenterofExcellenceCoreComponents_Upgrade (2.80)\" />
            //<Dependent type=\"10\" schemaName=\"admin_ArchiveApproval_AppLookup_admin_App\" displayName=\"admin_ArchiveApproval_AppLookup_admin_App\" parentSchemaName=\"admin_archiveapproval\" parentDisplayName=\"Archive Approval \" />
            //</MissingDependency>"
            foreach (XmlNode component in solutionManifest.SelectSingleNode("MissingDependencies").ChildNodes)
            {
                SolutionComponent required = new SolutionComponent()
                {
                    SchemaName = component["Required"].Attributes.GetNamedItem("schemaName")?.InnerText,
                    DisplayName = component["Required"].Attributes.GetNamedItem("displayName")?.InnerText,
                    Solution = component["Required"].Attributes.GetNamedItem("solution")?.InnerText,
                    ID = component["Required"].Attributes.GetNamedItem("id")?.InnerText,
                    ParentDisplayName = component["Required"].Attributes.GetNamedItem("parentDisplayName")?.InnerText,
                    ParentSchemaName = component["Required"].Attributes.GetNamedItem("parentSchemaName")?.InnerText,
                    IdSchemaName = component["Required"].Attributes.GetNamedItem("id.schemaname")?.InnerText,
                    Type = SolutionComponentHelper.GetComponentType(component["Required"].Attributes.GetNamedItem("type")?.InnerText)
                };
                SolutionComponent dependent = new SolutionComponent()
                {
                    SchemaName = component["Dependent"].Attributes.GetNamedItem("schemaName")?.InnerText,
                    DisplayName = component["Dependent"].Attributes.GetNamedItem("displayName")?.InnerText,
                    Type = SolutionComponentHelper.GetComponentType(component["Dependent"].Attributes.GetNamedItem("type")?.InnerText),
                    ID = component["Dependent"].Attributes.GetNamedItem("id")?.InnerText,
                    ParentDisplayName = component["Dependent"].Attributes.GetNamedItem("parentDisplayName")?.InnerText,
                    ParentSchemaName = component["Dependent"].Attributes.GetNamedItem("parentSchemaName")?.InnerText,
                    IdSchemaName = component["Dependent"].Attributes.GetNamedItem("id.schemaname")?.InnerText,
                    Solution = component["Dependent"].Attributes.GetNamedItem("solution")?.InnerText
                };
                solution.Dependencies.Add(new SolutionDependency(required, dependent));
            }

            //todo parse XML
            // Descriptions
            // LocalizedNames
            // Publisher
            // RootComponents --> https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/reference/solutioncomponent?view=dataverse-latest
            // MissingDependencies
            string s = "";
        }
    }

}