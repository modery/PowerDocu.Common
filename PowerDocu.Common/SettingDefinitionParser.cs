using System.IO;
using System.Xml;

namespace PowerDocu.Common
{
    public class SettingDefinitionParser
    {
        internal static SettingDefinitionEntity parseSettingDefinition(FileStream stream)
        {
            using StreamReader reader = new StreamReader(stream);
            string xml = reader.ReadToEnd();
            XmlDocument doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(xml);

            XmlNode root = doc.DocumentElement;
            return new SettingDefinitionEntity
            {
                UniqueName = root.Attributes?["uniquename"]?.Value,
                DisplayName = root.SelectSingleNode("displayname")?.Attributes?["default"]?.Value,
                Description = root.SelectSingleNode("description")?.Attributes?["default"]?.Value,
                DataType = root.SelectSingleNode("datatype")?.InnerText,
                DefaultValue = root.SelectSingleNode("defaultvalue")?.InnerText,
                IsCustomizable = root.SelectSingleNode("iscustomizable")?.InnerText == "1",
                IsHidden = root.SelectSingleNode("ishidden")?.InnerText == "1",
                IsOverridable = root.SelectSingleNode("isoverridable")?.InnerText == "1"
            };
        }
    }
}
