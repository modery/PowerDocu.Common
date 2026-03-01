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

        public string getIntersectEntityName() {
            return xmlEntity.SelectSingleNode("IntersectEntityName")?.InnerText ?? "";
        }

        public bool getIsCustomizable() {
            return xmlEntity.SelectSingleNode("IsCustomizable")?.InnerText.Equals("1") ?? false;
        }

        public string getIntroducedVersion() {
            return xmlEntity.SelectSingleNode("IntroducedVersion")?.InnerText ?? "";
        }

        public bool getIsHierarchical() {
            return xmlEntity.SelectSingleNode("IsHierarchical")?.InnerText.Equals("1") ?? false;
        }

        public bool getIsValidForAdvancedFind() {
            return xmlEntity.SelectSingleNode("IsValidForAdvancedFind")?.InnerText.Equals("1") ?? false;
        }

        public string getCascadeAssign() {
            return xmlEntity.SelectSingleNode("CascadeAssign")?.InnerText ?? "";
        }

        public string getCascadeDelete() {
            return xmlEntity.SelectSingleNode("CascadeDelete")?.InnerText ?? "";
        }

        public string getCascadeReparent() {
            return xmlEntity.SelectSingleNode("CascadeReparent")?.InnerText ?? "";
        }

        public string getCascadeShare() {
            return xmlEntity.SelectSingleNode("CascadeShare")?.InnerText ?? "";
        }

        public string getCascadeUnshare() {
            return xmlEntity.SelectSingleNode("CascadeUnshare")?.InnerText ?? "";
        }

        public string getCascadeArchive() {
            return xmlEntity.SelectSingleNode("CascadeArchive")?.InnerText ?? "";
        }

        public string getCascadeRollupView() {
            return xmlEntity.SelectSingleNode("CascadeRollupView")?.InnerText ?? "";
        }

        public string getDescription() {
            return xmlEntity.SelectSingleNode("RelationshipDescription/Descriptions/Description")?.Attributes.GetNamedItem("description")?.InnerText ?? "";
        }
    }
}
