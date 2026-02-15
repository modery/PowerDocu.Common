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
        private List<FormEntity> forms;
        private List<ViewEntity> views;


        public TableEntity(XmlNode xmlEntity)
        {
            this.xmlEntity = xmlEntity;
        }

        public string getLocalizedName()
        {
            return xmlEntity.SelectSingleNode("Name")?.Attributes.GetNamedItem("LocalizedName")?.InnerText ?? "";
        }

        public string getName()
        {
            return xmlEntity.SelectSingleNode("Name")?.InnerText ?? "";
        }

        public string getPrimaryColumn()
        {
            ColumnEntity primaryColumn = getPrimaryColumnEntity();
            return (primaryColumn != null)
                ? primaryColumn.getDisplayName()
                : "";
        }

        public ColumnEntity getPrimaryColumnEntity()
        {
            return GetColumns().Find(o => o.getDisplayMask().Contains("PrimaryName"));
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

        public bool containsNonDefaultLookupColumns()
        {
            return GetColumns().Count(o => o.isNonDefaultLookUpColumn()) > 0;
        }

        public bool IsAuditEnabled()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IsAuditEnabled")?.InnerText.Equals("1") ?? false;
        }

        public string GetOwnershipType()
        {
            string ownershipMask = xmlEntity.SelectSingleNode("EntityInfo/entity/OwnershipTypeMask")?.InnerText ?? "";
            return ownershipMask switch
            {
                "UserOwned" => "User or Team",
                "OrgOwned" => "Organization",
                _ => ownershipMask
            };
        }

        public List<FormEntity> GetForms()
        {
            if (forms == null)
            {
                forms = new List<FormEntity>();
                foreach (XmlNode form in xmlEntity.SelectNodes("FormXml/forms/systemform"))
                {
                    forms.Add(new FormEntity(form));
                }
            }
            return forms;
        }

        public List<FormEntity> GetFormsByType(string formType)
        {
            return GetForms().Where(form => form.FormXml.SelectSingleNode("type")?.InnerText == formType).ToList();
        }

        public FormEntity GetDefaultForm()
        {
            return GetForms().FirstOrDefault(form => form.FormXml.SelectSingleNode("isdefault")?.InnerText == "1");
        }

        public List<ViewEntity> GetViews()
        {
            if (views == null)
            {
                views = new List<ViewEntity>();
                foreach (XmlNode view in xmlEntity.SelectNodes("SavedQueries/savedqueries/savedquery"))
                {
                    views.Add(new ViewEntity(view));
                }
            }
            return views;
        }

        public ViewEntity GetDefaultView()
        {
            return GetViews().FirstOrDefault(view => view.GetFetchXml().Contains("<isdefault>1</isdefault>"));
        }
    }

    public class ColumnEntity
    {
        private readonly XmlNode xmlColumn;
        public static List<string> defaultLookupColumns = new List<string> { "createdby", "createdonbehalfby", "modifiedby", "modifiedonbehalfby", "ownerid", "owningbusinessunit", "owningteam", "owninguser" };

        public ColumnEntity(XmlNode xmlColumn)
        {
            this.xmlColumn = xmlColumn;
        }

        public string getDisplayName()
        {
            return xmlColumn.SelectSingleNode("displaynames/displayname")?.Attributes.GetNamedItem("description")?.InnerText ?? "";
        }

        public string getName()
        {
            return xmlColumn.Attributes.GetNamedItem("PhysicalName")?.InnerText ?? "";
        }

        public bool isDefaultLookUpColumn()
        {
            return getDataType().Equals("Lookup") && defaultLookupColumns.Contains(getLogicalName());
        }

        public bool isNonDefaultLookUpColumn()
        {
            return getDataType().Equals("Lookup") && !defaultLookupColumns.Contains(getLogicalName());
        }


        public string getLogicalName()
        {
            return xmlColumn.SelectSingleNode("LogicalName")?.InnerText ?? "";
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
                return xmlColumn.SelectSingleNode("IsCustomizable")?.InnerText.Equals("1") ?? false;
            return false;
        }

        public bool isRequired()
        {
            return xmlColumn.SelectSingleNode("RequiredLevel")?.InnerText.Equals("required") ?? false;
        }

        public bool isSearchable()
        {
            //todo this might not be the right field? Discrepancy in Let's Learn
            return xmlColumn.SelectSingleNode("IsSearchable").InnerText?.Equals("1") ?? false;
        }

        public string getDisplayMask()
        {
            return xmlColumn.SelectSingleNode("DisplayMask")?.InnerText ?? "";
        }

        public bool IsAuditEnabled()
        {
            return xmlColumn.SelectSingleNode("IsAuditEnabled")?.InnerText.Equals("1") ?? false;
        }
    }

    public class FormEntity
    {
        private readonly XmlNode xmlForm;

        public FormEntity(XmlNode xmlForm)
        {
            this.xmlForm = xmlForm;
        }

        public string GetFormId()
        {
            return xmlForm.SelectSingleNode("formid")?.InnerText ?? "";
        }

        public string GetFormName()
        {
            return xmlForm.SelectSingleNode("LocalizedNames/LocalizedName")?.Attributes.GetNamedItem("description")?.InnerText ?? "";
        }

        public XmlNode FormXml
        {
            get { return xmlForm.SelectSingleNode("form"); }
        }

        // Add more methods as needed to retrieve other form details
    }

    public class ViewEntity
    {
        private readonly XmlNode xmlView;

        public ViewEntity(XmlNode xmlView)
        {
            this.xmlView = xmlView;
        }

        public string GetViewId()
        {
            return xmlView.SelectSingleNode("savedqueryid")?.InnerText ?? "";
        }

        public string GetViewName()
        {
            return xmlView.SelectSingleNode("LocalizedNames/LocalizedName")?.Attributes.GetNamedItem("description")?.InnerText ?? "";
        }

        public string GetFetchXml()
        {
            return xmlView.SelectSingleNode("fetchxml")?.InnerText ?? "";
        }

        public string GetLayoutXml()
        {
            return xmlView.SelectSingleNode("layoutxml")?.InnerText ?? "";
        }

        // Add more methods as needed to retrieve other view details
    }
}