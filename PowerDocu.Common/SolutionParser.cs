using System;
using System.Collections.Generic;
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
                //process solution.xml
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
                //process customizations.xml
                ZipArchiveEntry customizationsDefinition = ZipHelper.getCustomizationsDefinitionFileFromZip(stream);
                if (customizationsDefinition != null)
                {
                    string tempFile = Path.GetDirectoryName(filename) + @"\" + customizationsDefinition.Name;
                    customizationsDefinition.ExtractToFile(tempFile, true);
                    NotificationHelper.SendNotification("  - Processing customizations.xml ");
                    using (FileStream customizations = new FileStream(tempFile, FileMode.Open))
                    {
                        solution.Customizations = CustomizationsParser.parseCustomizationsDefinition(customizations);
                    }
                    File.Delete(tempFile);
                }
                //process environment variable definitions
                List<ZipArchiveEntry> environmentVariableDefinitionFiles = ZipHelper.getFilesInPathFromZip(stream, "environmentvariabledefinitions/", "environmentvariabledefinition.xml");
                foreach(ZipArchiveEntry environmentVariableDefinition in environmentVariableDefinitionFiles)
                {
                    string tempFile = Path.GetDirectoryName(filename) + @"\" + environmentVariableDefinition.Name;
                    environmentVariableDefinition.ExtractToFile(tempFile, true);
                    NotificationHelper.SendNotification("  - Processing environment variable definition ");
                    using (FileStream environmentVariableDefinitionStream = new FileStream(tempFile, FileMode.Open))
                    {
                        EnvironmentVariableEntity environmentVariable = EnvironmentVariableParser.parseEnvironmentVariableDefinition(environmentVariableDefinitionStream);
                        solution.EnvironmentVariables.Add(environmentVariable);
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
            XmlNode publisherLocalizedNames = solutionManifest.SelectSingleNode("Publisher/LocalizedNames");
            if (publisherLocalizedNames != null)
            {
                foreach (XmlNode localizedName in publisherLocalizedNames.ChildNodes)
                {
                    solution.Publisher.LocalizedNames.Add(localizedName.Attributes.GetNamedItem("languagecode")?.InnerText,
                                                localizedName.Attributes.GetNamedItem("description")?.InnerText);
                }
            }
            XmlNode publisherAddresses = solutionManifest.SelectSingleNode("Publisher/Addresses");
            if (publisherAddresses != null)
            {
                foreach (XmlNode xmlAddress in publisherAddresses.ChildNodes)
                {
                    Address address = new Address()
                    {
                        AddressNumber = xmlAddress.SelectSingleNode("AddressNumber")?.InnerText,
                        AddressTypeCode = xmlAddress.SelectSingleNode("AddressTypeCode")?.InnerText,
                        City = xmlAddress.SelectSingleNode("City")?.InnerText,
                        County = xmlAddress.SelectSingleNode("County")?.InnerText,
                        Country = xmlAddress.SelectSingleNode("Country")?.InnerText,
                        Fax = xmlAddress.SelectSingleNode("Fax")?.InnerText,
                        FreightTermsCode = xmlAddress.SelectSingleNode("FreightTermsCode")?.InnerText,
                        ImportSequenceNumber = xmlAddress.SelectSingleNode("ImportSequenceNumber")?.InnerText,
                        Latitude = xmlAddress.SelectSingleNode("Latitude")?.InnerText,
                        Line1 = xmlAddress.SelectSingleNode("Line1")?.InnerText,
                        Line2 = xmlAddress.SelectSingleNode("Line2")?.InnerText,
                        Line3 = xmlAddress.SelectSingleNode("Line3")?.InnerText,
                        Longitude = xmlAddress.SelectSingleNode("Longitude")?.InnerText,
                        Name = xmlAddress.SelectSingleNode("Name")?.InnerText,
                        PostalCode = xmlAddress.SelectSingleNode("PostalCode")?.InnerText,
                        PostOfficeBox = xmlAddress.SelectSingleNode("PostOfficeBox")?.InnerText,
                        PrimaryContactName = xmlAddress.SelectSingleNode("PrimaryContactName")?.InnerText,
                        ShippingMethodCode = xmlAddress.SelectSingleNode("ShippingMethodCode")?.InnerText,
                        StateOrProvince = xmlAddress.SelectSingleNode("StateOrProvince")?.InnerText,
                        Telephone1 = xmlAddress.SelectSingleNode("Telephone1")?.InnerText,
                        Telephone2 = xmlAddress.SelectSingleNode("Telephone2")?.InnerText,
                        Telephone3 = xmlAddress.SelectSingleNode("Telephone3")?.InnerText,
                        TimeZoneRuleVersionNumber = xmlAddress.SelectSingleNode("TimeZoneRuleVersionNumber")?.InnerText,
                        UPSZone = xmlAddress.SelectSingleNode("UPSZone")?.InnerText,
                        UTCOffset = xmlAddress.SelectSingleNode("UTCOffset")?.InnerText,
                        UTCConversionTimeZoneCode = xmlAddress.SelectSingleNode("UTCConversionTimeZoneCode")?.InnerText
                    };
                    solution.Publisher.Addresses.Add(address);
                }
            }
            XmlNode publisherDescriptions = solutionManifest.SelectSingleNode("Publisher/Descriptions");
            if (publisherDescriptions != null)
            {
                foreach (XmlNode description in publisherDescriptions.ChildNodes)
                {
                    solution.Publisher.Descriptions.Add(description.Attributes.GetNamedItem("languagecode")?.InnerText,
                                                description.Attributes.GetNamedItem("description")?.InnerText);
                }
            }
            //parsing the components
            XmlNode rootComponents = solutionManifest.SelectSingleNode("RootComponents");
            if (rootComponents != null)
            {
                foreach (XmlNode component in rootComponents.ChildNodes)
                {
                    SolutionComponent solutionComponent = new SolutionComponent()
                    {
                        SchemaName = component.Attributes.GetNamedItem("schemaName")?.InnerText,
                        ID = component.Attributes.GetNamedItem("id")?.InnerText.Trim('{', '}'),
                        Type = SolutionComponentHelper.GetComponentType(component.Attributes.GetNamedItem("type")?.InnerText)
                    };
                    solution.Components.Add(solutionComponent);
                }
            }
            //parsing the dependencies
            XmlNode missingDependencies = solutionManifest.SelectSingleNode("MissingDependencies");
            if (missingDependencies != null)
            {
                foreach (XmlNode component in missingDependencies.ChildNodes)
                {
                    SolutionComponent required = new SolutionComponent()
                    {
                        SchemaName = component["Required"].Attributes.GetNamedItem("schemaName")?.InnerText,
                        reqdepDisplayName = component["Required"].Attributes.GetNamedItem("displayName")?.InnerText,
                        reqdepSolution = component["Required"].Attributes.GetNamedItem("solution")?.InnerText,
                        ID = component["Required"].Attributes.GetNamedItem("id")?.InnerText.Trim('{', '}'),
                        reqdepParentDisplayName = component["Required"].Attributes.GetNamedItem("parentDisplayName")?.InnerText,
                        reqdepParentSchemaName = component["Required"].Attributes.GetNamedItem("parentSchemaName")?.InnerText,
                        reqdepIdSchemaName = component["Required"].Attributes.GetNamedItem("id.schemaname")?.InnerText,
                        Type = SolutionComponentHelper.GetComponentType(component["Required"].Attributes.GetNamedItem("type")?.InnerText)
                    };
                    SolutionComponent dependent = new SolutionComponent()
                    {
                        SchemaName = component["Dependent"].Attributes.GetNamedItem("schemaName")?.InnerText,
                        reqdepDisplayName = component["Dependent"].Attributes.GetNamedItem("displayName")?.InnerText,
                        Type = SolutionComponentHelper.GetComponentType(component["Dependent"].Attributes.GetNamedItem("type")?.InnerText),
                        ID = component["Dependent"].Attributes.GetNamedItem("id")?.InnerText.Trim('{', '}'),
                        reqdepParentDisplayName = component["Dependent"].Attributes.GetNamedItem("parentDisplayName")?.InnerText,
                        reqdepParentSchemaName = component["Dependent"].Attributes.GetNamedItem("parentSchemaName")?.InnerText,
                        reqdepIdSchemaName = component["Dependent"].Attributes.GetNamedItem("id.schemaname")?.InnerText,
                        reqdepSolution = component["Dependent"].Attributes.GetNamedItem("solution")?.InnerText
                    };
                    solution.Dependencies.Add(new SolutionDependency(required, dependent));
                }
            }

            //LocalizedNames
            XmlNode localizedNames = solutionManifest.SelectSingleNode("LocalizedNames");
            if (localizedNames != null)
            {
                foreach (XmlNode localizedName in localizedNames.ChildNodes)
                {
                    solution.LocalizedNames.Add(localizedName.Attributes.GetNamedItem("languagecode")?.InnerText,
                                                localizedName.Attributes.GetNamedItem("description")?.InnerText);
                }
            }
            //Descriptions
            XmlNode descriptions = solutionManifest.SelectSingleNode("LocalizedNames");
            if (descriptions != null)
            {
                foreach (XmlNode description in descriptions.ChildNodes)
                {
                    solution.Descriptions.Add(description.Attributes.GetNamedItem("languagecode")?.InnerText,
                                                description.Attributes.GetNamedItem("description")?.InnerText);
                }
            }
        }
    }
}