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
            //TODO descriptions and displaynames localised versions
            envVar.DescriptionDefault = root.SelectSingleNode("description")?.Attributes["default"]?.Value;
            envVar.DisplayName = root.SelectSingleNode("displayname")?.Attributes["default"]?.Value;
            envVar.IntroducedVersion = root.SelectSingleNode("introducedversion").InnerText;
            envVar.IsCustomizable = root.SelectSingleNode("iscustomizable").InnerText == "1";
            envVar.IsRequired = root.SelectSingleNode("isrequired").InnerText == "1";
            //envVar.SecretStore = Convert.ToBoolean(root.SelectSingleNode("secretstore").InnerText);
            envVar.Type = root.SelectSingleNode("type").InnerText;

            return envVar;
        }
    }
}
