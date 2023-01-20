using System.IO;
using System.Xml;

namespace PowerDocu.Common
{
    public static class CustomizationsParser
    {
        public static CustomizationsEntity parseCustomizationsDefinition(Stream customizationsFile)
        {
            using StreamReader reader = new StreamReader(customizationsFile);
            string solutionXML = reader.ReadToEnd();
            XmlDocument solutionXmlDoc = new XmlDocument
            {
                XmlResolver = null
            };
            solutionXmlDoc.LoadXml(solutionXML);
            return new CustomizationsEntity()
            {
                customizationsXml = solutionXmlDoc.SelectSingleNode("/ImportExportXml")
            };
        }
    }
}