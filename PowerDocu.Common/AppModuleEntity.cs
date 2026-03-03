using System.Collections.Generic;
using System.Linq;

namespace PowerDocu.Common
{
    /// <summary>
    /// Represents a Model-Driven App (AppModule) as defined in customizations.xml.
    /// </summary>
    public class AppModuleEntity
    {
        public string UniqueName { get; set; } = string.Empty;
        public string IntroducedVersion { get; set; } = string.Empty;
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
        public int FormFactor { get; set; }
        public int ClientType { get; set; }
        public int NavigationType { get; set; }
        public string WebResourceId { get; set; } = string.Empty;
        public string OptimizedFor { get; set; } = string.Empty;
        public Dictionary<string, string> LocalizedNames { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Descriptions { get; set; } = new Dictionary<string, string>();
        public List<AppModuleComponent> Components { get; set; } = new List<AppModuleComponent>();
        public List<string> SecurityRoleIds { get; set; } = new List<string>();
        public List<AppModuleAppElement> AppElements { get; set; } = new List<AppModuleAppElement>();
        public List<AppModuleSetting> AppSettings { get; set; } = new List<AppModuleSetting>();
        public AppModuleSiteMap SiteMap { get; set; }

        /// <summary>
        /// Returns the display name (first localized name) or falls back to UniqueName.
        /// </summary>
        public string GetDisplayName()
        {
            if (LocalizedNames.Count > 0)
                return LocalizedNames.Values.First();
            return UniqueName;
        }

        /// <summary>
        /// Returns the description (first localized description) or empty string.
        /// </summary>
        public string GetDescription()
        {
            if (Descriptions.Count > 0)
                return Descriptions.Values.First();
            return string.Empty;
        }

        /// <summary>
        /// Whether the app is active (statecode 0).
        /// </summary>
        public bool IsActive()
        {
            return StateCode == 0;
        }

        /// <summary>
        /// Returns the human-readable form factor.
        /// </summary>
        public string GetFormFactorDisplayName()
        {
            return FormFactor switch
            {
                1 => "Web",
                2 => "Tablet",
                3 => "Phone",
                _ => FormFactor.ToString()
            };
        }

        /// <summary>
        /// Returns the human-readable client type.
        /// </summary>
        public string GetClientTypeDisplayName()
        {
            return ClientType switch
            {
                1 => "Web",
                2 => "Outlook",
                4 => "Unified Interface",
                _ => ClientType.ToString()
            };
        }

        /// <summary>
        /// Returns components of type "Entity" (tables included in the app).
        /// </summary>
        public List<AppModuleComponent> GetTables()
        {
            return Components.Where(c => c.Type == "Entity").ToList();
        }

        /// <summary>
        /// Returns components of type "Saved Query" (views included in the app).
        /// </summary>
        public List<AppModuleComponent> GetViews()
        {
            return Components.Where(c => c.Type == "Saved Query").ToList();
        }

        /// <summary>
        /// Returns components of type "Site Map".
        /// </summary>
        public List<AppModuleComponent> GetSiteMaps()
        {
            return Components.Where(c => c.Type == "Site Map").ToList();
        }

        /// <summary>
        /// Returns canvas app elements (custom pages) embedded in this app.
        /// </summary>
        public List<AppModuleAppElement> GetCustomPages()
        {
            return AppElements;
        }
    }

    /// <summary>
    /// A component included in an AppModule (entity, sitemap, view, etc.).
    /// </summary>
    public class AppModuleComponent
    {
        public string Type { get; set; } = string.Empty;
        public string SchemaName { get; set; } = string.Empty;
        public string ID { get; set; } = string.Empty;
    }

    /// <summary>
    /// An embedded canvas app page (appelement) within a Model-Driven App.
    /// </summary>
    public class AppModuleAppElement
    {
        public string UniqueName { get; set; } = string.Empty;
        public string CanvasAppName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsCustomizable { get; set; }
    }

    /// <summary>
    /// An app-level setting defined in the AppModule.
    /// </summary>
    public class AppModuleSetting
    {
        public string SettingName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsCustomizable { get; set; }
    }

    /// <summary>
    /// Represents the SiteMap (navigation definition) for a Model-Driven App.
    /// </summary>
    public class AppModuleSiteMap
    {
        public string UniqueName { get; set; } = string.Empty;
        public bool EnableCollapsibleGroups { get; set; }
        public bool ShowHome { get; set; }
        public bool ShowPinned { get; set; }
        public bool ShowRecents { get; set; }
        public Dictionary<string, string> LocalizedNames { get; set; } = new Dictionary<string, string>();
        public List<SiteMapArea> Areas { get; set; } = new List<SiteMapArea>();

        public string GetDisplayName()
        {
            if (LocalizedNames.Count > 0)
                return LocalizedNames.Values.First();
            return UniqueName;
        }
    }

    /// <summary>
    /// An area within a SiteMap (top-level navigation grouping).
    /// </summary>
    public class SiteMapArea
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool ShowGroups { get; set; }
        public List<SiteMapGroup> Groups { get; set; } = new List<SiteMapGroup>();
    }

    /// <summary>
    /// A group within a SiteMap area.
    /// </summary>
    public class SiteMapGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<SiteMapSubArea> SubAreas { get; set; } = new List<SiteMapSubArea>();
    }

    /// <summary>
    /// A sub-area (navigation item) within a SiteMap group.
    /// </summary>
    public class SiteMapSubArea
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string VectorIcon { get; set; } = string.Empty;

        /// <summary>
        /// Returns a description of what this sub-area navigates to.
        /// </summary>
        public string GetTargetDescription()
        {
            if (!string.IsNullOrEmpty(Entity))
                return $"Table: {Entity}";
            if (!string.IsNullOrEmpty(Page))
                return $"Custom Page: {Page}";
            if (!string.IsNullOrEmpty(Url))
                return $"URL: {Url}";
            return string.Empty;
        }
    }
}
