using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace PowerDocu.Common
{
    public class TableEntity
    {
        private readonly XmlNode xmlEntity;
        private List<ColumnEntity> columns;

        public TableEntity(XmlNode xmlEntity)
        {
            this.xmlEntity = xmlEntity;
        }

        public string getLocalizedName()
        {
            return xmlEntity.SelectSingleNode("Name").Attributes.GetNamedItem("LocalizedName").InnerText;
        }

        public string getName()
        {
            return xmlEntity.SelectSingleNode("Name").InnerText;
        }

        public string getPrimaryColumn()
        {
            return GetColumns().First(o => o.getDisplayMask().Contains("PrimaryName")).getDisplayName();
        }

        public string getDescription()
        {
            //todo what if there are more?
            return xmlEntity.SelectSingleNode("EntityInfo/entity/Descriptions/Description")?.Attributes.GetNamedItem("description")?.InnerText ?? "";
        }

        public List<ColumnEntity> GetColumns()
        {
            if (columns == null)
            {
                columns = new List<ColumnEntity>();
                foreach (XmlNode xmlColumn in xmlEntity.SelectNodes("EntityInfo/entity/attributes/attribute"))
                {
                    columns.Add(new ColumnEntity(xmlColumn));
                }
                columns.Sort((a, b) => a.getDisplayName().CompareTo(b.getDisplayName()));
            }
            return columns;
        }
    }

    public class ColumnEntity
    {
        private readonly XmlNode xmlColumn;

        public ColumnEntity(XmlNode xmlColumn)
        {
            this.xmlColumn = xmlColumn;
        }

        public string getDisplayName()
        {
            return xmlColumn.SelectSingleNode("displaynames/displayname").Attributes.GetNamedItem("description").InnerText;
        }
        public string getName()
        {
            return xmlColumn.Attributes.GetNamedItem("PhysicalName").InnerText;
        }
        public string getDataType()
        {
            return xmlColumn.SelectSingleNode("Type").InnerText switch
            {
                "bit" => "Yes/No",
                "datetime" => "Date and time",
                "decimal" => "Decimal",
                "file" => "File",
                "float" => "Float",
                "int" => "Whole number",
                "lookup" => "Lookup",
                "nvarchar" => "Single line of text",
                "ntext" => "Multiple lines of text",
                "owner" => "Owner",
                "money" => "Currency",
                "picklist" => "picklist",
                "primarykey" => "Primary Key",
                "state" => "Choice",
                "status" => "Choice",
                "uniqueidentifier" => "uniqueidentifier",
                _ => xmlColumn.SelectSingleNode("Type").InnerText
            };
        }

        public bool isCustomizable()
        {
            //todo this might not be the right field? Discrepancy in Let's Learn
            if (xmlColumn.SelectSingleNode("IsCustomizable") != null)
                return xmlColumn.SelectSingleNode("IsCustomizable").InnerText.Equals("1");
            return false;
        }

        public bool isRequired()
        {
            return xmlColumn.SelectSingleNode("RequiredLevel").InnerText.Equals("required");
        }

        public bool isSearchable()
        {
            //todo this might not be the right field? Discrepancy in Let's Learn
            return xmlColumn.SelectSingleNode("IsSearchable").InnerText.Equals("1");
        }

        public string getDisplayMask()
        {
            return xmlColumn.SelectSingleNode("DisplayMask")?.InnerText ?? "";
        }
    }
}