using System.IO;
using System.Xml;

namespace PowerDocu.Common
{
    public class EnvironmentVariableParser
    {
        internal static EnvironmentVariableEntity parseEnvironmentVariableDefinition(
            FileStream environmentVariableDefinitionStream
        )
        {
            using StreamReader reader = new StreamReader(environmentVariableDefinitionStream);
            string envVarXML = reader.ReadToEnd();
            XmlDocument envVarXmlDoc = new XmlDocument { XmlResolver = null };
            envVarXmlDoc.LoadXml(envVarXML);

            EnvironmentVariableEntity envVar = new EnvironmentVariableEntity();
            XmlNode root = envVarXmlDoc.DocumentElement;
            envVar.Name = root.Attributes["schemaname"].Value;
            envVar.DefaultValue = root.SelectSingleNode("defaultvalue")?.InnerText;
            envVar.DescriptionDefault = root.SelectSingleNode("description")?.Attributes["default"]?.Value;
            envVar.DisplayName = root.SelectSingleNode("displayname")?.Attributes["default"]?.Value;
            envVar.IntroducedVersion = root.SelectSingleNode("introducedversion").InnerText;
            envVar.IsCustomizable = root.SelectSingleNode("iscustomizable").InnerText == "1";
            envVar.IsRequired = root.SelectSingleNode("isrequired").InnerText == "1";
            //envVar.SecretStore = Convert.ToBoolean(root.SelectSingleNode("secretstore").InnerText);
            envVar.Type = root.SelectSingleNode("type").InnerText;

            // Parse localized descriptions
            XmlNodeList descLabels = root.SelectNodes("description/label");
            if (descLabels != null)
            {
                foreach (XmlNode label in descLabels)
                {
                    string langCode = label.Attributes?["languagecode"]?.Value;
                    string desc = label.Attributes?["description"]?.Value;
                    if (!string.IsNullOrEmpty(langCode) && !string.IsNullOrEmpty(desc) && !envVar.Descriptions.ContainsKey(langCode))
                    {
                        envVar.Descriptions.Add(langCode, desc);
                    }
                }
            }

            // Parse localized display names
            XmlNodeList nameLabels = root.SelectNodes("displayname/label");
            if (nameLabels != null)
            {
                foreach (XmlNode label in nameLabels)
                {
                    string langCode = label.Attributes?["languagecode"]?.Value;
                    string name = label.Attributes?["description"]?.Value;
                    if (!string.IsNullOrEmpty(langCode) && !string.IsNullOrEmpty(name) && !envVar.LocalizedNames.ContainsKey(langCode))
                    {
                        envVar.LocalizedNames.Add(langCode, name);
                    }
                }
            }

            return envVar;
        }
    }
}
