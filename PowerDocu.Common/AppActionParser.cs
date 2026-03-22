using System.IO;
using System.Xml;

namespace PowerDocu.Common
{
    public class AppActionParser
    {
        internal static AppActionEntity parseAppActionDefinition(FileStream stream)
        {
            using StreamReader reader = new StreamReader(stream);
            string xml = reader.ReadToEnd();
            XmlDocument doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(xml);

            XmlNode root = doc.DocumentElement;
            var entity = new AppActionEntity
            {
                UniqueName = root.Attributes?["uniquename"]?.Value,
                Name = root.SelectSingleNode("name")?.InnerText,
                ButtonLabel = root.SelectSingleNode("buttonlabeltext")?.Attributes?["default"]?.Value,
                ContextEntity = root.SelectSingleNode("contextentity/logicalname")?.InnerText,
                AppModuleName = root.SelectSingleNode("appmoduleid/uniquename")?.InnerText,
                FontIcon = root.SelectSingleNode("fonticon")?.InnerText,
                IsHidden = root.SelectSingleNode("hidden")?.InnerText == "1",
                OnClickEventType = root.SelectSingleNode("onclickeventtype")?.InnerText,
                OnClickFunctionName = root.SelectSingleNode("onclickeventjavascriptfunctionname")?.InnerText,
                VisibilityType = root.SelectSingleNode("visibilitytype")?.InnerText
            };
            return entity;
        }
    }
}
