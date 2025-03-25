using System.Xml;

namespace PowerDocu.Common
{
    public class EntityRelationship
    {
        private readonly XmlNode xmlEntity;

        public EntityRelationship(XmlNode xmlEntity)
        {
            this.xmlEntity = xmlEntity;
        }

        public string getName()
        {
            return xmlEntity.Attributes.GetNamedItem("Name")?.InnerText ?? "";
        }

        public string getReferencingEntityName() {
            return xmlEntity.SelectSingleNode("ReferencingEntityName")?.InnerText ?? "";
        }

        public string getReferencedEntityName() {
            return xmlEntity.SelectSingleNode("ReferencedEntityName")?.InnerText ?? "";
        }

        public string getFirstEntityName() {
            return xmlEntity.SelectSingleNode("FirstEntityName")?.InnerText ?? "";
        }

        public string getSecondEntityName() {
            return xmlEntity.SelectSingleNode("SecondEntityName")?.InnerText ?? "";
        }

        public string getRelationshipType() {
            return xmlEntity.SelectSingleNode("EntityRelationshipType")?.InnerText ?? "";
        }

        public string getReferencingAttributeName() {
            return xmlEntity.SelectSingleNode("ReferencingAttributeName")?.InnerText ?? "";

        }
    }
}
