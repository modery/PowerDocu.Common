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

        public string GetEntitySetName()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/EntitySetName")?.InnerText ?? "";
        }

        public bool IsCustomizable()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IsCustomizable")?.InnerText.Equals("1") ?? false;
        }

        public string GetIntroducedVersion()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IntroducedVersion")?.InnerText ?? "";
        }

        public bool IsChangeTrackingEnabled()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/ChangeTrackingEnabled")?.InnerText.Equals("1") ?? false;
        }

        public bool IsActivity()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IsActivity")?.InnerText.Equals("1") ?? false;
        }

        public bool IsQuickCreateEnabled()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IsQuickCreateEnabled")?.InnerText.Equals("1") ?? false;
        }

        public bool IsConnectionsEnabled()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IsConnectionsEnabled")?.InnerText.Equals("1") ?? false;
        }

        public bool IsDuplicateCheckSupported()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IsDuplicateCheckSupported")?.InnerText.Equals("1") ?? false;
        }

        public bool IsVisibleInMobile()
        {
            return xmlEntity.SelectSingleNode("EntityInfo/entity/IsVisibleInMobile")?.InnerText.Equals("1") ?? false;
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
        public static List<string> defaultColumns = new List<string> { "createdby", "createdonbehalfby", "createdon", "importsequencenumber", "modifiedby", "modifiedonbehalfby", "modifiedon", "ownerid", "owningbusinessunit", "owningteam", "owninguser", "overriddencreatedon", "statecode", "statuscode", "timezoneruleversionnumber", "utcconversiontimezonecode" };

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

        public bool isDefaultColumn()
        {
            return defaultColumns.Contains(getLogicalName());
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

        public bool IsSecured()
        {
            return xmlColumn.SelectSingleNode("IsSecured")?.InnerText.Equals("1") ?? false;
        }

        public bool IsCustomField()
        {
            return xmlColumn.SelectSingleNode("IsCustomField")?.InnerText.Equals("1") ?? false;
        }

        public bool IsFilterable()
        {
            return xmlColumn.SelectSingleNode("IsFilterable")?.InnerText.Equals("1") ?? false;
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

        public string GetFormType()
        {
            return FormXml?.SelectSingleNode("type")?.InnerText ?? "";
        }

        public string GetFormTypeDisplayName()
        {
            return GetFormType() switch
            {
                "2" => "Main",
                "5" => "Quick Create",
                "6" => "Quick View",
                "7" => "Card",
                "0" => "Dashboard",
                "4" => "Task Flow",
                "11" => "Main - Interactive Experience",
                _ => GetFormType()
            };
        }

        public bool IsDefault()
        {
            return FormXml?.SelectSingleNode("isdefault")?.InnerText.Equals("1") ?? false;
        }

        public bool IsActive()
        {
            return xmlForm.SelectSingleNode("FormActivationState")?.InnerText.Equals("1") ?? true;
        }

        public bool IsCustomizable()
        {
            return xmlForm.SelectSingleNode("IsCustomizable")?.InnerText.Equals("1") ?? false;
        }

        public string GetIntroducedVersion()
        {
            return xmlForm.SelectSingleNode("IntroducedVersion")?.InnerText ?? "";
        }

        public string GetDescription()
        {
            return xmlForm.SelectSingleNode("Descriptions/Description")?.Attributes.GetNamedItem("description")?.InnerText ?? "";
        }

        public List<FormTab> GetTabs()
        {
            var tabs = new List<FormTab>();
            XmlNodeList tabNodes = FormXml?.SelectNodes("tabs/tab");
            if (tabNodes != null)
            {
                foreach (XmlNode tabNode in tabNodes)
                {
                    tabs.Add(new FormTab(tabNode));
                }
            }
            return tabs;
        }
    }

    public class FormTab
    {
        private readonly XmlNode xmlTab;

        public FormTab(XmlNode xmlTab)
        {
            this.xmlTab = xmlTab;
        }

        public string GetName()
        {
            return xmlTab.SelectSingleNode("labels/label")?.Attributes.GetNamedItem("description")?.InnerText ?? "(unnamed)";
        }

        public bool IsVisible()
        {
            string visible = xmlTab.Attributes?["visible"]?.Value ?? "true";
            return visible != "false";
        }

        public List<FormSection> GetSections()
        {
            var sections = new List<FormSection>();
            XmlNodeList sectionNodes = xmlTab.SelectNodes("columns/column/sections/section");
            if (sectionNodes != null)
            {
                foreach (XmlNode sectionNode in sectionNodes)
                {
                    sections.Add(new FormSection(sectionNode));
                }
            }
            return sections;
        }
    }

    public class FormSection
    {
        private readonly XmlNode xmlSection;

        public FormSection(XmlNode xmlSection)
        {
            this.xmlSection = xmlSection;
        }

        public string GetName()
        {
            return xmlSection.SelectSingleNode("labels/label")?.Attributes.GetNamedItem("description")?.InnerText ?? "(unnamed)";
        }

        public bool IsVisible()
        {
            string visible = xmlSection.Attributes?["visible"]?.Value ?? "true";
            return visible != "false";
        }

        public List<FormControl> GetControls()
        {
            var controls = new List<FormControl>();
            XmlNodeList controlNodes = xmlSection.SelectNodes("rows/row/cell/control");
            if (controlNodes != null)
            {
                foreach (XmlNode controlNode in controlNodes)
                {
                    controls.Add(new FormControl(controlNode));
                }
            }
            return controls;
        }
    }

    public class FormControl
    {
        private readonly XmlNode xmlControl;

        public FormControl(XmlNode xmlControl)
        {
            this.xmlControl = xmlControl;
        }

        public string GetId()
        {
            return xmlControl.Attributes?["id"]?.Value ?? "";
        }

        public string GetDataFieldName()
        {
            return xmlControl.Attributes?["datafieldname"]?.Value ?? "";
        }

        public string GetClassId()
        {
            return xmlControl.Attributes?["classid"]?.Value ?? "";
        }
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

        public string GetQueryType()
        {
            return xmlView.SelectSingleNode("querytype")?.InnerText ?? "";
        }

        public string GetQueryTypeDisplayName()
        {
            return GetQueryType() switch
            {
                "0" => "Public",
                "1" => "Advanced Find",
                "2" => "Associated",
                "4" => "Quick Find",
                "64" => "Lookup",
                "8192" => "My",
                _ => GetQueryType()
            };
        }

        public bool IsDefault()
        {
            return xmlView.SelectSingleNode("isdefault")?.InnerText.Equals("1") ?? false;
        }

        public bool IsCustomizable()
        {
            return xmlView.SelectSingleNode("IsCustomizable")?.InnerText.Equals("1") ?? false;
        }

        public string GetIntroducedVersion()
        {
            return xmlView.SelectSingleNode("IntroducedVersion")?.InnerText ?? "";
        }

        public List<ViewColumn> GetColumns()
        {
            var columns = new List<ViewColumn>();
            XmlNode layoutNode = xmlView.SelectSingleNode("layoutxml");
            if (layoutNode != null && !String.IsNullOrEmpty(layoutNode.InnerXml))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(layoutNode.InnerXml);
                XmlNodeList cellNodes = doc.SelectNodes("grid/row/cell");
                if (cellNodes != null)
                {
                    int order = 1;
                    foreach (XmlNode cellNode in cellNodes)
                    {
                        columns.Add(new ViewColumn(cellNode, order++));
                    }
                }
            }
            return columns;
        }
    }

    public class ViewColumn
    {
        private readonly XmlNode xmlCell;
        public int Order { get; }

        public ViewColumn(XmlNode xmlCell, int order)
        {
            this.xmlCell = xmlCell;
            Order = order;
        }

        public string GetName()
        {
            return xmlCell.Attributes?["name"]?.Value ?? "";
        }

        public string GetWidth()
        {
            return xmlCell.Attributes?["width"]?.Value ?? "";
        }
    }
}