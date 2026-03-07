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
            return TableDefinitionHelper.MapOwnershipType(ownershipMask);
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
            // Try reading from a <type> child element first (numeric form type)
            string type = FormXml?.SelectSingleNode("type")?.InnerText ?? "";
            if (string.IsNullOrEmpty(type))
            {
                // Fallback: read from parent <forms type="..."> element attribute
                type = xmlForm.ParentNode?.Attributes?["type"]?.Value ?? "";
            }
            return type;
        }

        public string GetFormTypeDisplayName()
        {
            return GetFormType().ToLowerInvariant() switch
            {
                "2" or "main" => "Main",
                "5" or "quick" => "Quick Create",
                "6" or "quickview" or "quickviewform" => "Quick View",
                "7" or "card" => "Card",
                "0" or "dashboard" => "Dashboard",
                "4" or "taskflow" => "Task Flow",
                "11" or "maininteractiveexperience" => "Main - Interactive Experience",
                "mobile" => "Mobile",
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

        /// <summary>
        /// Returns the columns defined in this tab, preserving the multi-column layout.
        /// Each FormTabColumn contains its own list of sections.
        /// </summary>
        public List<FormTabColumn> GetColumns()
        {
            var columns = new List<FormTabColumn>();
            XmlNodeList columnNodes = xmlTab.SelectNodes("columns/column");
            if (columnNodes != null)
            {
                foreach (XmlNode columnNode in columnNodes)
                {
                    columns.Add(new FormTabColumn(columnNode));
                }
            }
            return columns;
        }
    }

    public class FormTabColumn
    {
        private readonly XmlNode xmlColumn;

        public FormTabColumn(XmlNode xmlColumn)
        {
            this.xmlColumn = xmlColumn;
        }

        public List<FormSection> GetSections()
        {
            var sections = new List<FormSection>();
            XmlNodeList sectionNodes = xmlColumn.SelectNodes("sections/section");
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

        /// <summary>
        /// Returns a human-readable label for this control.
        /// Uses the datafieldname if available, otherwise the control id.
        /// </summary>
        public string GetDisplayLabel()
        {
            string field = GetDataFieldName();
            return !string.IsNullOrEmpty(field) ? field : GetId();
        }

        /// <summary>
        /// Returns true if the parent cell has showlabel set to false.
        /// </summary>
        public bool IsLabelHidden()
        {
            return xmlControl.ParentNode?.Attributes?["showlabel"]?.Value == "false";
        }

        /// <summary>
        /// Returns true if the control is marked as disabled (read-only).
        /// </summary>
        public bool IsDisabled()
        {
            return xmlControl.Attributes?["disabled"]?.Value == "true";
        }

        /// <summary>
        /// Returns true if the parent cell is marked as not visible.
        /// </summary>
        public bool IsVisible()
        {
            string visible = xmlControl.ParentNode?.Attributes?["visible"]?.Value ?? "true";
            return visible != "false";
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

        /// <summary>
        /// Parses the &lt;order&gt; elements from the view's fetchxml.
        /// Returns a list of (attribute, descending) tuples representing sort orders.
        /// </summary>
        public List<ViewSortOrder> GetSortOrders()
        {
            var sortOrders = new List<ViewSortOrder>();
            XmlNode fetchNode = xmlView.SelectSingleNode("fetchxml");
            if (fetchNode != null && !String.IsNullOrEmpty(fetchNode.InnerXml))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(fetchNode.InnerXml);
                XmlNodeList orderNodes = doc.SelectNodes("//order");
                if (orderNodes != null)
                {
                    foreach (XmlNode orderNode in orderNodes)
                    {
                        string attribute = orderNode.Attributes?["attribute"]?.Value ?? "";
                        bool descending = orderNode.Attributes?["descending"]?.Value == "true";
                        if (!string.IsNullOrEmpty(attribute))
                        {
                            sortOrders.Add(new ViewSortOrder(attribute, descending));
                        }
                    }
                }
            }
            return sortOrders;
        }

        /// <summary>
        /// Parses the &lt;filter&gt; and &lt;condition&gt; elements from the view's fetchxml.
        /// Returns a structured filter tree.
        /// </summary>
        public ViewFilter GetFilter()
        {
            XmlNode fetchNode = xmlView.SelectSingleNode("fetchxml");
            if (fetchNode != null && !String.IsNullOrEmpty(fetchNode.InnerXml))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(fetchNode.InnerXml);
                XmlNode filterNode = doc.SelectSingleNode("//entity/filter");
                if (filterNode != null)
                {
                    return ParseFilter(filterNode);
                }
            }
            return null;
        }

        private static ViewFilter ParseFilter(XmlNode filterNode)
        {
            var filter = new ViewFilter();
            filter.Type = filterNode.Attributes?["type"]?.Value ?? "and";
            filter.IsQuickFind = filterNode.Attributes?["isquickfindfields"]?.Value == "1";

            foreach (XmlNode child in filterNode.ChildNodes)
            {
                if (child.Name == "condition")
                {
                    var condition = new ViewFilterCondition();
                    condition.Attribute = child.Attributes?["attribute"]?.Value ?? "";
                    condition.Operator = child.Attributes?["operator"]?.Value ?? "";
                    condition.Value = child.Attributes?["value"]?.Value ?? "";
                    filter.Conditions.Add(condition);
                }
                else if (child.Name == "filter")
                {
                    filter.SubFilters.Add(ParseFilter(child));
                }
            }
            return filter;
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

    public class ViewSortOrder
    {
        public string Attribute { get; }
        public bool Descending { get; }

        public ViewSortOrder(string attribute, bool descending)
        {
            Attribute = attribute;
            Descending = descending;
        }

        /// <summary>
        /// Returns a human-readable string like "name (ascending)" or "createdon (descending)".
        /// </summary>
        public string ToDisplayString(Dictionary<string, string> columnDisplayNames = null)
        {
            string displayName = Attribute;
            if (columnDisplayNames != null && columnDisplayNames.TryGetValue(Attribute, out string dn) && !string.IsNullOrEmpty(dn))
            {
                displayName = dn + " (" + Attribute + ")";
            }
            return displayName + (Descending ? " descending" : " ascending");
        }
    }

    public class ViewFilter
    {
        public string Type { get; set; } = "and";
        public bool IsQuickFind { get; set; }
        public List<ViewFilterCondition> Conditions { get; set; } = new List<ViewFilterCondition>();
        public List<ViewFilter> SubFilters { get; set; } = new List<ViewFilter>();

        /// <summary>
        /// Returns a human-readable description of the filter tree.
        /// </summary>
        public string ToDisplayString(Dictionary<string, string> columnDisplayNames = null)
        {
            if (Conditions.Count == 0 && SubFilters.Count == 0)
                return "";

            var parts = new List<string>();
            foreach (var condition in Conditions)
            {
                parts.Add(condition.ToDisplayString(columnDisplayNames));
            }
            foreach (var subFilter in SubFilters)
            {
                string subStr = subFilter.ToDisplayString(columnDisplayNames);
                if (!string.IsNullOrEmpty(subStr))
                {
                    string prefix = subFilter.IsQuickFind ? "[Quick Find] " : "";
                    parts.Add(prefix + "(" + subStr + ")");
                }
            }

            string separator = Type.Equals("or", StringComparison.OrdinalIgnoreCase) ? " OR " : " AND ";
            return string.Join(separator, parts);
        }
    }

    public class ViewFilterCondition
    {
        public string Attribute { get; set; } = "";
        public string Operator { get; set; } = "";
        public string Value { get; set; } = "";

        /// <summary>
        /// Returns a human-readable string like "statecode = 0" or "name contains {0}".
        /// </summary>
        public string ToDisplayString(Dictionary<string, string> columnDisplayNames = null)
        {
            string displayAttr = Attribute;
            if (columnDisplayNames != null && columnDisplayNames.TryGetValue(Attribute, out string dn) && !string.IsNullOrEmpty(dn))
            {
                displayAttr = dn + " (" + Attribute + ")";
            }

            string op = Operator switch
            {
                "eq" => "=",
                "ne" => "≠",
                "lt" => "<",
                "le" => "≤",
                "gt" => ">",
                "ge" => "≥",
                "like" => "like",
                "not-like" => "not like",
                "null" => "is null",
                "not-null" => "is not null",
                "in" => "in",
                "not-in" => "not in",
                "eq-userid" => "= [current user]",
                "ne-userid" => "≠ [current user]",
                "eq-businessid" => "= [current business unit]",
                "contains" => "contains",
                "not-contain" => "does not contain",
                "begins-with" => "begins with",
                "not-begin-with" => "does not begin with",
                "ends-with" => "ends with",
                "not-end-with" => "does not end with",
                "on" => "on",
                "on-or-before" => "on or before",
                "on-or-after" => "on or after",
                "today" => "= today",
                "yesterday" => "= yesterday",
                "tomorrow" => "= tomorrow",
                "this-year" => "this year",
                "last-year" => "last year",
                "next-year" => "next year",
                "this-month" => "this month",
                "last-month" => "last month",
                "last-x-days" => "last " + Value + " days",
                "next-x-days" => "next " + Value + " days",
                _ => Operator
            };

            // Operators that include the value in their description or don't need a value
            if (Operator == "null" || Operator == "not-null" || Operator == "eq-userid" ||
                Operator == "ne-userid" || Operator == "eq-businessid" ||
                Operator == "today" || Operator == "yesterday" || Operator == "tomorrow" ||
                Operator == "this-year" || Operator == "last-year" || Operator == "next-year" ||
                Operator == "this-month" || Operator == "last-month" ||
                Operator == "last-x-days" || Operator == "next-x-days")
            {
                return displayAttr + " " + op;
            }

            if (!string.IsNullOrEmpty(Value))
            {
                return displayAttr + " " + op + " " + Value;
            }
            return displayAttr + " " + op;
        }
    }
}