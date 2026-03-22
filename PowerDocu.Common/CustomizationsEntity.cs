using System;
using System.Collections.Generic;
using System.Xml;

namespace PowerDocu.Common
{
    //this class is complementary to the SolutionEntity class. At some point in the future, the information from the customizations.xml should move into SolutionEntity
    public class CustomizationsEntity
    {
        public XmlNode customizationsXml;

        private List<TableEntity> tableEntities;
        private List<EntityRelationship> entityRelationships;
        private List<AIModel> AIModels;
        private List<OptionSetEntity> optionSets;
        private List<AppModuleEntity> appModules;
        private List<WebResourceEntity> webResources;
        private List<RibbonCustomizationEntity> ribbonCustomizations;

        public string getAppNameBySchemaName(string schemaName)
        {
            return customizationsXml.SelectSingleNode("/ImportExportXml/CanvasApps/CanvasApp[Name='" + schemaName + "']/DisplayName")?.InnerText;
        }

        public string getFlowNameById(string ID)
        {
            string normalizedId = "{" + ID.Trim('{', '}').ToLowerInvariant() + "}";
            return customizationsXml.SelectSingleNode("/ImportExportXml/Workflows/Workflow[@WorkflowId='" + normalizedId + "']")?.Attributes.GetNamedItem("Name").InnerText;
        }

        public FlowEntity.ModernFlowType getModernFlowTypeById(string ID)
        {
            string normalizedId = "{" + ID.Trim('{', '}').ToLowerInvariant() + "}";
            XmlNode node = customizationsXml.SelectSingleNode("/ImportExportXml/Workflows/Workflow[@WorkflowId='" + normalizedId + "']/ModernFlowType");
            if (node != null && int.TryParse(node.InnerText, out int value))
            {
                return (FlowEntity.ModernFlowType)value;
            }
            return FlowEntity.ModernFlowType.CloudFlow;
        }

        public List<TableEntity> getEntities()
        {
            if (tableEntities == null)
            {
                tableEntities = new List<TableEntity>();
                foreach (XmlNode xmlEntity in customizationsXml.SelectNodes("/ImportExportXml/Entities/Entity"))
                {
                    tableEntities.Add(new TableEntity(xmlEntity));
                }
                tableEntities.Sort((a, b) => a.getLocalizedName().CompareTo(b.getLocalizedName()));
            }
            return tableEntities;
        }


        public List<AIModel> getAIModels()
        {
            if (AIModels == null)
            {
                AIModels = new List<AIModel>();
                foreach (XmlNode xmlEntity in customizationsXml.SelectNodes("/ImportExportXml/AIModels/AIModel"))
                {
                    AIModels.Add(new AIModel(xmlEntity));
                }
                AIModels.Sort((a, b) => a.getLocalizedName().CompareTo(b.getLocalizedName()));
            }
            return AIModels;
        }

        public List<ConnectorDefinition> getConnectors()
        {
            var connectors = new List<ConnectorDefinition>();
            XmlNodeList connectorNodes = customizationsXml.SelectNodes("/ImportExportXml/Connectors/Connector");
            if (connectorNodes != null)
            {
                foreach (XmlNode node in connectorNodes)
                {
                    connectors.Add(new ConnectorDefinition
                    {
                        Id = node.SelectSingleNode("connectorid")?.InnerText,
                        Name = node.SelectSingleNode("name")?.InnerText,
                        DisplayName = node.SelectSingleNode("displayname")?.InnerText,
                        Description = node.SelectSingleNode("description")?.InnerText,
                        ConnectorType = int.TryParse(node.SelectSingleNode("connectortype")?.InnerText, out var ct) ? ct : 0,
                        IconBrandColor = node.SelectSingleNode("iconbrandcolor")?.InnerText,
                        OpenApiDefinitionJson = null,
                        ConnectionParametersJson = null,
                        PolicyTemplateInstancesJson = null,
                        IconBlobBase64 = null
                    });
                }
            }
            return connectors;
        }

        public List<ConnectionReferenceDefinition> getConnectionReferences()
        {
            var refs = new List<ConnectionReferenceDefinition>();
            XmlNodeList refNodes = customizationsXml.SelectNodes("/ImportExportXml/connectionreferences/connectionreference");
            if (refNodes != null)
            {
                foreach (XmlNode node in refNodes)
                {
                    refs.Add(new ConnectionReferenceDefinition
                    {
                        LogicalName = node.Attributes?["connectionreferencelogicalname"]?.Value,
                        DisplayName = node.SelectSingleNode("connectionreferencedisplayname")?.InnerText,
                        ConnectorId = node.SelectSingleNode("connectorid")?.InnerText,
                        CustomConnectorId = node.SelectSingleNode("customconnectorid/connectorid")?.InnerText
                    });
                }
            }
            return refs;
        }

        public List<EntityRelationship> getEntityRelationships()
        {
            if (entityRelationships == null)
            {
                entityRelationships = new List<EntityRelationship>();
                foreach (XmlNode xmlEntity in customizationsXml.SelectNodes("/ImportExportXml/EntityRelationships/EntityRelationship"))
                {
                    entityRelationships.Add(new EntityRelationship(xmlEntity));
                }
                entityRelationships.Sort((a, b) => a.getName().CompareTo(b.getName()));
            }
            return entityRelationships;
        }

        public XmlNode getEntityBySchemaName(string schemaName)
        {
            return customizationsXml.SelectSingleNode("/ImportExportXml/Entities/Entity[Name='" + schemaName + "']");
        }

        public List<OptionSetEntity> getOptionSets()
        {
            if (optionSets == null)
            {
                optionSets = new List<OptionSetEntity>();
                XmlNodeList optionSetNodes = customizationsXml.SelectNodes("/ImportExportXml/optionsets/optionset");
                if (optionSetNodes != null)
                {
                    foreach (XmlNode xmlOptionSet in optionSetNodes)
                    {
                        optionSets.Add(new OptionSetEntity(xmlOptionSet));
                    }
                    optionSets.Sort((a, b) => a.GetDisplayName().CompareTo(b.GetDisplayName()));
                }
            }
            return optionSets;
        }

        public List<WebResourceEntity> getWebResources()
        {
            if (webResources == null)
            {
                webResources = new List<WebResourceEntity>();
                XmlNodeList nodes = customizationsXml.SelectNodes("/ImportExportXml/WebResources/WebResource");
                if (nodes != null)
                {
                    foreach (XmlNode node in nodes)
                    {
                        webResources.Add(new WebResourceEntity
                        {
                            Id = node.SelectSingleNode("WebResourceId")?.InnerText,
                            Name = node.SelectSingleNode("Name")?.InnerText,
                            DisplayName = node.SelectSingleNode("DisplayName")?.InnerText,
                            WebResourceType = node.SelectSingleNode("WebResourceType")?.InnerText,
                            IntroducedVersion = node.SelectSingleNode("IntroducedVersion")?.InnerText,
                            IsCustomizable = node.SelectSingleNode("IsCustomizable")?.InnerText == "1",
                            IsHidden = node.SelectSingleNode("IsHidden")?.InnerText == "1",
                            FileName = node.SelectSingleNode("FileName")?.InnerText
                        });
                    }
                    webResources.Sort((a, b) => (a.DisplayName ?? a.Name ?? "").CompareTo(b.DisplayName ?? b.Name ?? ""));
                }
            }
            return webResources;
        }

        public List<RibbonCustomizationEntity> getRibbonCustomizations()
        {
            if (ribbonCustomizations == null)
            {
                ribbonCustomizations = new List<RibbonCustomizationEntity>();
                foreach (XmlNode xmlEntity in customizationsXml.SelectNodes("/ImportExportXml/Entities/Entity"))
                {
                    XmlNode ribbonDiffXml = xmlEntity.SelectSingleNode("RibbonDiffXml");
                    if (ribbonDiffXml == null) continue;

                    XmlNodeList hideActions = ribbonDiffXml.SelectNodes("CustomActions/HideCustomAction");
                    XmlNodeList commandDefs = ribbonDiffXml.SelectNodes("CommandDefinitions/CommandDefinition");
                    XmlNodeList displayRules = ribbonDiffXml.SelectNodes("RuleDefinitions/DisplayRules/DisplayRule");
                    XmlNodeList enableRules = ribbonDiffXml.SelectNodes("RuleDefinitions/EnableRules/EnableRule");

                    int totalCustomizations = (hideActions?.Count ?? 0) + (commandDefs?.Count ?? 0) + (displayRules?.Count ?? 0) + (enableRules?.Count ?? 0);
                    if (totalCustomizations == 0) continue;

                    var ribbon = new RibbonCustomizationEntity
                    {
                        EntityName = xmlEntity.SelectSingleNode("Name")?.InnerText,
                        CommandDefinitionCount = commandDefs?.Count ?? 0,
                        DisplayRuleCount = displayRules?.Count ?? 0,
                        EnableRuleCount = enableRules?.Count ?? 0
                    };
                    if (hideActions != null)
                    {
                        foreach (XmlNode ha in hideActions)
                        {
                            string location = ha.Attributes?.GetNamedItem("Location")?.InnerText;
                            if (!string.IsNullOrEmpty(location))
                                ribbon.HiddenActions.Add(location);
                        }
                    }
                    ribbonCustomizations.Add(ribbon);
                }
                ribbonCustomizations.Sort((a, b) => (a.EntityName ?? "").CompareTo(b.EntityName ?? ""));
            }
            return ribbonCustomizations;
        }

        public List<RoleEntity> getRoles()
        {
            List<RoleEntity> roles = new List<RoleEntity>();
            foreach (XmlNode role in customizationsXml.SelectNodes("/ImportExportXml/Roles/Role"))
            {
                RoleEntity roleEntity = new RoleEntity
                {
                    Name = role.Attributes.GetNamedItem("name")?.InnerText,
                    ID = role.Attributes.GetNamedItem("id")?.InnerText
                };
                foreach (XmlNode privilege in role.SelectNodes("RolePrivileges/RolePrivilege"))
                {
                    //<RolePrivilege name="prvAppendadmin_App" level="Local" />
                    //<RolePrivilege name="prvAppendadmin_PVA" level="Local" />
                    //<RolePrivilege name="prvAppendNote" level="Global" />
                    //<RolePrivilege name="prvAppendToadmin_App" level="Local" />
                    //<RolePrivilege name="prvAppendToadmin_PVA" level="Local" />
                    string privilegeName = privilege.Attributes.GetNamedItem("name").InnerText;
                    if (Privileges.GetMiscellaneousPrivileges().ContainsKey(privilegeName))
                    {
                        roleEntity.miscellaneousPrivileges.Add(privilegeName, privilege.Attributes.GetNamedItem("level").InnerText);
                    }
                    else
                    {
                        privilegeName = privilegeName[3..];
                        string priv = "";
                        if (privilegeName.StartsWith("Create"))
                        {
                            priv = "Create";
                        }
                        else if (privilegeName.StartsWith("Read"))
                        {
                            priv = "Read";
                        }
                        else if (privilegeName.StartsWith("Write"))
                        {
                            priv = "Write";
                        }
                        else if (privilegeName.StartsWith("Delete"))
                        {
                            priv = "Delete";
                        }
                        else if (privilegeName.StartsWith("AppendTo"))
                        {
                            priv = "AppendTo";
                        }
                        else if (privilegeName.StartsWith("Append"))
                        {
                            priv = "Append";
                        }
                        else if (privilegeName.StartsWith("Assign"))
                        {
                            priv = "Assign";
                        }
                        else if (privilegeName.StartsWith("Share"))
                        {
                            priv = "Share";
                        }
                        else if (privilegeName.StartsWith("Flow"))
                        {
                            priv = "Flow";
                        }
                        else if (privilegeName.StartsWith("ExportToExcel"))
                        {
                            priv = "ExportToExcel";
                        }
                        else if (privilegeName.StartsWith("DocumentGeneration"))
                        {
                            priv = "DocumentGeneration";
                        }
                        else if (privilegeName.StartsWith("GoOffline"))
                        {
                            priv = "GoOffline";
                        }
                        else if (privilegeName.StartsWith("MailMerge"))
                        {
                            priv = "MailMerge";
                        }
                        else if (privilegeName.StartsWith("Merge"))
                        {
                            priv = "Merge";
                        }
                        else if (privilegeName.StartsWith("OneDrive"))
                        {
                            priv = "OneDrive";
                        }
                        else if (privilegeName.StartsWith("Print"))
                        {
                            priv = "Print";
                        }
                        else if (privilegeName.StartsWith("SyncToOutlook"))
                        {
                            priv = "SyncToOutlook";
                        }
                        else if (privilegeName.StartsWith("UseOfficeApps"))
                        {
                            priv = "UseOfficeApps";
                        }
                        else if (privilegeName.StartsWith("UseTabletApp"))
                        {
                            priv = "UseTabletApp";
                        }
                        else if (privilegeName.StartsWith("WebMailMerge"))
                        {
                            priv = "WebMailMerge";
                        }
                        else if (privilegeName.StartsWith("WorkflowExecution"))
                        {
                            priv = "WorkflowExecution";
                        }
                        else
                        {
                            priv = "Other";
                        }
                        parseAccessLevel(roleEntity, privilegeName.Replace(priv, ""), priv, privilege.Attributes.GetNamedItem("level").InnerText);
                    }
                }
                roles.Add(roleEntity);
            }
            return roles;
        }

        /// <summary>
        /// Parses all Model-Driven Apps (AppModules) from customizations.xml,
        /// including their SiteMap navigation definitions.
        /// </summary>
        public List<AppModuleEntity> getAppModules()
        {
            if (appModules == null)
            {
                appModules = new List<AppModuleEntity>();

                // Build a lookup of SiteMaps by unique name
                Dictionary<string, AppModuleSiteMap> siteMapLookup = new Dictionary<string, AppModuleSiteMap>(StringComparer.OrdinalIgnoreCase);
                XmlNodeList siteMapNodes = customizationsXml.SelectNodes("/ImportExportXml/AppModuleSiteMaps/AppModuleSiteMap");
                if (siteMapNodes != null)
                {
                    foreach (XmlNode siteMapNode in siteMapNodes)
                    {
                        AppModuleSiteMap siteMap = parseAppModuleSiteMap(siteMapNode);
                        if (!string.IsNullOrEmpty(siteMap.UniqueName))
                        {
                            siteMapLookup[siteMap.UniqueName] = siteMap;
                        }
                    }
                }

                // Parse each AppModule
                XmlNodeList appModuleNodes = customizationsXml.SelectNodes("/ImportExportXml/AppModules/AppModule");
                if (appModuleNodes != null)
                {
                    foreach (XmlNode appModuleNode in appModuleNodes)
                    {
                        AppModuleEntity appModule = parseAppModule(appModuleNode);

                        // Link SiteMap by UniqueName
                        if (siteMapLookup.TryGetValue(appModule.UniqueName, out AppModuleSiteMap matchedSiteMap))
                        {
                            appModule.SiteMap = matchedSiteMap;
                        }

                        appModules.Add(appModule);
                    }
                }
                appModules.Sort((a, b) => a.GetDisplayName().CompareTo(b.GetDisplayName()));
            }
            return appModules;
        }

        private AppModuleEntity parseAppModule(XmlNode node)
        {
            var appModule = new AppModuleEntity
            {
                UniqueName = node.SelectSingleNode("UniqueName")?.InnerText ?? string.Empty,
                IntroducedVersion = node.SelectSingleNode("IntroducedVersion")?.InnerText ?? string.Empty,
                WebResourceId = node.SelectSingleNode("WebResourceId")?.InnerText ?? string.Empty,
                OptimizedFor = node.SelectSingleNode("OptimizedFor")?.InnerText ?? string.Empty,
            };

            int.TryParse(node.SelectSingleNode("statecode")?.InnerText, out int stateCode);
            appModule.StateCode = stateCode;
            int.TryParse(node.SelectSingleNode("statuscode")?.InnerText, out int statusCode);
            appModule.StatusCode = statusCode;
            int.TryParse(node.SelectSingleNode("FormFactor")?.InnerText, out int formFactor);
            appModule.FormFactor = formFactor;
            int.TryParse(node.SelectSingleNode("ClientType")?.InnerText, out int clientType);
            appModule.ClientType = clientType;
            int.TryParse(node.SelectSingleNode("NavigationType")?.InnerText, out int navigationType);
            appModule.NavigationType = navigationType;

            // Localized Names
            XmlNodeList localizedNames = node.SelectNodes("LocalizedNames/LocalizedName");
            if (localizedNames != null)
            {
                foreach (XmlNode ln in localizedNames)
                {
                    string langCode = ln.Attributes?.GetNamedItem("languagecode")?.InnerText;
                    string desc = ln.Attributes?.GetNamedItem("description")?.InnerText;
                    if (!string.IsNullOrEmpty(langCode) && !appModule.LocalizedNames.ContainsKey(langCode))
                        appModule.LocalizedNames[langCode] = desc ?? string.Empty;
                }
            }

            // Descriptions
            XmlNodeList descriptions = node.SelectNodes("Descriptions/Description");
            if (descriptions != null)
            {
                foreach (XmlNode d in descriptions)
                {
                    string langCode = d.Attributes?.GetNamedItem("languagecode")?.InnerText;
                    string desc = d.Attributes?.GetNamedItem("description")?.InnerText;
                    if (!string.IsNullOrEmpty(langCode) && !appModule.Descriptions.ContainsKey(langCode))
                        appModule.Descriptions[langCode] = desc ?? string.Empty;
                }
            }

            // AppModuleComponents
            XmlNodeList componentNodes = node.SelectNodes("AppModuleComponents/AppModuleComponent");
            if (componentNodes != null)
            {
                foreach (XmlNode comp in componentNodes)
                {
                    appModule.Components.Add(new AppModuleComponent
                    {
                        Type = SolutionComponentHelper.GetComponentType(comp.Attributes?.GetNamedItem("type")?.InnerText),
                        SchemaName = comp.Attributes?.GetNamedItem("schemaName")?.InnerText ?? string.Empty,
                        ID = comp.Attributes?.GetNamedItem("id")?.InnerText?.Trim('{', '}') ?? string.Empty
                    });
                }
            }

            // AppModuleRoleMaps
            XmlNodeList roleNodes = node.SelectNodes("AppModuleRoleMaps/Role");
            if (roleNodes != null)
            {
                foreach (XmlNode role in roleNodes)
                {
                    string roleId = role.Attributes?.GetNamedItem("id")?.InnerText?.Trim('{', '}');
                    if (!string.IsNullOrEmpty(roleId))
                        appModule.SecurityRoleIds.Add(roleId);
                }
            }

            // AppElements (embedded canvas pages)
            XmlNodeList appElementNodes = node.SelectNodes("appelements/appelement");
            if (appElementNodes != null)
            {
                foreach (XmlNode elem in appElementNodes)
                {
                    appModule.AppElements.Add(new AppModuleAppElement
                    {
                        UniqueName = elem.Attributes?.GetNamedItem("uniquename")?.InnerText ?? string.Empty,
                        CanvasAppName = elem.SelectSingleNode("canvasappid/name")?.InnerText ?? string.Empty,
                        DisplayName = elem.SelectSingleNode("name")?.InnerText ?? string.Empty,
                        IsCustomizable = elem.SelectSingleNode("iscustomizable")?.InnerText == "1"
                    });
                }
            }

            // AppSettings
            XmlNodeList settingNodes = node.SelectNodes("appsettings/appsetting");
            if (settingNodes != null)
            {
                foreach (XmlNode setting in settingNodes)
                {
                    appModule.AppSettings.Add(new AppModuleSetting
                    {
                        SettingName = setting.Attributes?.GetNamedItem("settingdefinitionid.uniquename")?.InnerText ?? string.Empty,
                        Value = setting.SelectSingleNode("value")?.InnerText ?? string.Empty,
                        IsCustomizable = setting.SelectSingleNode("iscustomizable")?.InnerText == "1"
                    });
                }
            }

            return appModule;
        }

        private AppModuleSiteMap parseAppModuleSiteMap(XmlNode node)
        {
            var siteMap = new AppModuleSiteMap
            {
                UniqueName = node.SelectSingleNode("SiteMapUniqueName")?.InnerText ?? string.Empty,
                EnableCollapsibleGroups = node.SelectSingleNode("EnableCollapsibleGroups")?.InnerText?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false,
                ShowHome = node.SelectSingleNode("ShowHome")?.InnerText?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false,
                ShowPinned = node.SelectSingleNode("ShowPinned")?.InnerText?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false,
                ShowRecents = node.SelectSingleNode("ShowRecents")?.InnerText?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false,
            };

            // Localized Names
            XmlNodeList localizedNames = node.SelectNodes("LocalizedNames/LocalizedName");
            if (localizedNames != null)
            {
                foreach (XmlNode ln in localizedNames)
                {
                    string langCode = ln.Attributes?.GetNamedItem("languagecode")?.InnerText;
                    string desc = ln.Attributes?.GetNamedItem("description")?.InnerText;
                    if (!string.IsNullOrEmpty(langCode) && !siteMap.LocalizedNames.ContainsKey(langCode))
                        siteMap.LocalizedNames[langCode] = desc ?? string.Empty;
                }
            }

            // Parse Areas
            XmlNodeList areaNodes = node.SelectNodes("SiteMap/Area");
            if (areaNodes != null)
            {
                foreach (XmlNode areaNode in areaNodes)
                {
                    var area = new SiteMapArea
                    {
                        Id = areaNode.Attributes?.GetNamedItem("Id")?.InnerText ?? string.Empty,
                        ShowGroups = areaNode.Attributes?.GetNamedItem("ShowGroups")?.InnerText?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false,
                        Title = getFirstTitle(areaNode)
                    };

                    // Parse Groups
                    XmlNodeList groupNodes = areaNode.SelectNodes("Group");
                    if (groupNodes != null)
                    {
                        foreach (XmlNode groupNode in groupNodes)
                        {
                            var group = new SiteMapGroup
                            {
                                Id = groupNode.Attributes?.GetNamedItem("Id")?.InnerText ?? string.Empty,
                                Title = getFirstTitle(groupNode)
                            };

                            // Parse SubAreas
                            XmlNodeList subAreaNodes = groupNode.SelectNodes("SubArea");
                            if (subAreaNodes != null)
                            {
                                foreach (XmlNode subAreaNode in subAreaNodes)
                                {
                                    var subArea = new SiteMapSubArea
                                    {
                                        Id = subAreaNode.Attributes?.GetNamedItem("Id")?.InnerText ?? string.Empty,
                                        Entity = subAreaNode.Attributes?.GetNamedItem("Entity")?.InnerText ?? string.Empty,
                                        Page = subAreaNode.Attributes?.GetNamedItem("Page")?.InnerText ?? string.Empty,
                                        Url = subAreaNode.Attributes?.GetNamedItem("Url")?.InnerText ?? string.Empty,
                                        Icon = subAreaNode.Attributes?.GetNamedItem("Icon")?.InnerText ?? string.Empty,
                                        VectorIcon = subAreaNode.Attributes?.GetNamedItem("VectorIcon")?.InnerText ?? string.Empty,
                                        Title = getFirstTitle(subAreaNode)
                                    };
                                    group.SubAreas.Add(subArea);
                                }
                            }
                            area.Groups.Add(group);
                        }
                    }
                    siteMap.Areas.Add(area);
                }
            }

            return siteMap;
        }

        /// <summary>
        /// Gets the first Title element (LCID 1033 preferred) from a Titles child node.
        /// </summary>
        private string getFirstTitle(XmlNode parentNode)
        {
            XmlNode titles = parentNode.SelectSingleNode("Titles");
            if (titles != null)
            {
                // Prefer English (1033)
                XmlNode englishTitle = titles.SelectSingleNode("Title[@LCID='1033']");
                if (englishTitle != null)
                    return englishTitle.Attributes?.GetNamedItem("Title")?.InnerText ?? string.Empty;
                // Fallback to first title
                XmlNode firstTitle = titles.SelectSingleNode("Title");
                if (firstTitle != null)
                    return firstTitle.Attributes?.GetNamedItem("Title")?.InnerText ?? string.Empty;
            }
            return string.Empty;
        }

        private void parseAccessLevel(RoleEntity roleEntity, string tableName, string privilege, string accessLevel)
        {
            TableAccess tableAccess = roleEntity.Tables.Find(o => o.Name == tableName);
            if (tableAccess == null)
            {
                tableAccess = new TableAccess
                {
                    Name = tableName
                };
                roleEntity.Tables.Add(tableAccess);
            }
            AccessLevel level = accessLevel switch
            {
                "Basic" => AccessLevel.Basic,
                "Deep" => AccessLevel.Deep,
                "Global" => AccessLevel.Global,
                "Local" => AccessLevel.Local,
                _ => AccessLevel.None
            };
            switch (privilege)
            {
                case "Create":
                    tableAccess.Create = level;
                    break;
                case "Read":
                    tableAccess.Read = level;
                    break;
                case "Write":
                    tableAccess.Write = level;
                    break;
                case "Delete":
                    tableAccess.Delete = level;
                    break;
                case "Append":
                    tableAccess.Append = level;
                    break;
                case "AppendTo":
                    tableAccess.AppendTo = level;
                    break;
                case "Assign":
                    tableAccess.Assign = level;
                    break;
                case "Share":
                    tableAccess.Share = level;
                    break;
            }
        }
    }
}