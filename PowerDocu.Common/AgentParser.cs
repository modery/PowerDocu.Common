using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;

namespace PowerDocu.Common
{
    public class AgentParser
    {
        private readonly List<AgentEntity> Agents = new List<AgentEntity>();
        private readonly List<AIModel> AIModels = new List<AIModel>();

        public AgentParser(string filename)
        {
            string tempFile;
            NotificationHelper.SendNotification(" - Processing " + filename);
            if (filename.EndsWith(".zip"))
            {
                using FileStream stream = new FileStream(filename, FileMode.Open);
                List<ZipArchiveEntry> agentFiles = ZipHelper.getFilesInPathFromZip(stream, "bots/", ".xml");
                //process agents - they are in subfolders in the bots directory
                foreach (ZipArchiveEntry agentFile in agentFiles)
                {
                    NotificationHelper.SendNotification("  - Processing " + agentFile.FullName);
                    tempFile = Path.GetDirectoryName(filename) + @"\" + agentFile.Name;
                    agentFile.ExtractToFile(tempFile, true);
                    using (FileStream agentStream = new FileStream(tempFile, FileMode.Open))
                    {
                        // Parse bot.xml and create AgentEntity
                        var xmlDoc = new XmlDocument();
                        xmlDoc.Load(agentStream);
                        var botNode = xmlDoc.SelectSingleNode("/bot");
                        if (botNode != null)
                        {
                            var entity = new AgentEntity
                            {
                                SchemaName = botNode.Attributes["schemaname"]?.Value,
                                AuthenticationMode = int.TryParse(botNode.SelectSingleNode("authenticationmode")?.InnerText, out var am) ? am : 0,
                                AuthenticationTrigger = int.TryParse(botNode.SelectSingleNode("authenticationtrigger")?.InnerText, out var at) ? at : 0,
                                IconBase64 = botNode.SelectSingleNode("iconbase64")?.InnerText,
                                IsCustomizable = int.TryParse(botNode.SelectSingleNode("iscustomizable")?.InnerText, out var ic) ? ic : 0,
                                Language = int.TryParse(botNode.SelectSingleNode("language")?.InnerText, out var lang) ? lang : 0,
                                Name = botNode.SelectSingleNode("name")?.InnerText,
                                RuntimeProvider = int.TryParse(botNode.SelectSingleNode("runtimeprovider")?.InnerText, out var rp) ? rp : 0,
                                SynchronizationStatus = botNode.SelectSingleNode("synchronizationstatus")?.InnerText,
                                Template = botNode.SelectSingleNode("template")?.InnerText,
                                TimeZoneRuleVersionNumber = int.TryParse(botNode.SelectSingleNode("timezoneruleversionnumber")?.InnerText, out var tz) ? tz : 0
                            };

                            // Retrieve configuration.json from the same directory as bot.xml
                            string configFile = Path.Combine(Path.GetDirectoryName(tempFile), "configuration.json");
                            ZipArchiveEntry configEntry = ZipHelper.getFileFromZip(stream, agentFile.FullName.Replace("bot.xml", "configuration.json"));
                            configEntry.ExtractToFile(configFile, true);
                            string configJson = File.ReadAllText(configFile);
                            entity.Configuration = JsonConvert.DeserializeObject<AgentConfiguration>(configJson);
                            Agents.Add(entity);
                            File.Delete(configFile);
                        }
                    }
                    File.Delete(tempFile);
                }
                //process topics. they are in subfolders in the botcomponents folder, folder names start with {agentname].topic.
                foreach (AgentEntity agent in Agents)
                {
                    string botComponentsPath = $"botcomponents/{agent.SchemaName}.";
                    List<ZipArchiveEntry> botComponentFiles = ZipHelper.getFilesInPathFromZip(stream, botComponentsPath, "botcomponent.xml");
                    foreach (ZipArchiveEntry botComponentFile in botComponentFiles)
                    {
                        NotificationHelper.SendNotification($"  - Processing {botComponentFile.FullName}");
                        tempFile = Path.GetDirectoryName(filename) + @"\" + botComponentFile.Name;
                        botComponentFile.ExtractToFile(tempFile, true);

                        // Load and parse botcomponent.xml
                        var botComponentDoc = new XmlDocument();
                        botComponentDoc.Load(tempFile);
                        var botComponentNode = botComponentDoc.SelectSingleNode("/botcomponent");
                        BotComponent botComponent = new BotComponent();
                        if (botComponentNode != null)
                        {
                            botComponent.SchemaName = botComponentNode.Attributes["schemaname"]?.Value;
                            botComponent.ComponentType = int.TryParse(botComponentNode.SelectSingleNode("componenttype")?.InnerText, out var ct) ? ct : 0;
                            botComponent.IsCustomizable = int.TryParse(botComponentNode.SelectSingleNode("iscustomizable")?.InnerText, out var ic) ? ic : 0;
                            botComponent.Name = botComponentNode.SelectSingleNode("name")?.InnerText;
                            var parentBotIdNode = botComponentNode.SelectSingleNode("parentbotid/schemaname");
                            botComponent.ParentBotSchemaName = parentBotIdNode?.InnerText;
                            botComponent.StateCode = int.TryParse(botComponentNode.SelectSingleNode("statecode")?.InnerText, out var sc) ? sc : 0;
                            botComponent.StatusCode = int.TryParse(botComponentNode.SelectSingleNode("statuscode")?.InnerText, out var stc) ? stc : 0;
                            botComponent.Description = botComponentNode.SelectSingleNode("description")?.InnerText;
                            botComponent.Category = botComponentNode.SelectSingleNode("category")?.InnerText;
                            // Parse filedata element for file-based knowledge (type 14)
                            var fileDataNode = botComponentNode.SelectSingleNode("filedata");
                            if (fileDataNode != null)
                            {
                                botComponent.FileDataMimeType = fileDataNode.Attributes?["mimetype"]?.Value;
                                botComponent.FileDataName = fileDataNode.InnerText;
                            }
                        }
                        File.Delete(tempFile);
                        // Load the data file in the same path as topicFile and set YamlData
                        string dataFilePath = Path.Combine(Path.GetDirectoryName(tempFile), "data");
                        tempFile = Path.GetDirectoryName(filename) + @"\data";
                        ZipArchiveEntry dataFile = ZipHelper.getFileFromZip(stream, botComponentFile.FullName.Replace("botcomponent.xml", "data"));
                        if (dataFile != null)
                        {
                            dataFile.ExtractToFile(tempFile, true);
                            botComponent.YamlData = File.ReadAllText(dataFilePath);
                            File.Delete(tempFile);
                        }
                        agent.BotComponents.Add(botComponent);
                    }
                }
                // Parse dvtablesearchs
                List<ZipArchiveEntry> dvTableSearchFiles = ZipHelper.getFilesInPathFromZip(stream, "dvtablesearchs/", ".xml");
                var dvTableSearches = new List<DvTableSearch>();
                foreach (ZipArchiveEntry dvtsFile in dvTableSearchFiles)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + dvtsFile.Name;
                    dvtsFile.ExtractToFile(tempFile, true);
                    var dvtsDoc = new XmlDocument();
                    dvtsDoc.Load(tempFile);
                    var dvtsNode = dvtsDoc.SelectSingleNode("/dvtablesearch");
                    if (dvtsNode != null)
                    {
                        dvTableSearches.Add(new DvTableSearch
                        {
                            Id = dvtsNode.Attributes["dvtablesearchid"]?.Value,
                            Name = dvtsNode.SelectSingleNode("name")?.InnerText,
                            SearchType = int.TryParse(dvtsNode.SelectSingleNode("searchtype")?.InnerText, out var searchType) ? searchType : 0,
                            StateCode = int.TryParse(dvtsNode.SelectSingleNode("statecode")?.InnerText, out var dvtsSc) ? dvtsSc : 0,
                            StatusCode = int.TryParse(dvtsNode.SelectSingleNode("statuscode")?.InnerText, out var dvtsStc) ? dvtsStc : 0
                        });
                    }
                    File.Delete(tempFile);
                }
                // Parse dvtablesearchentities
                List<ZipArchiveEntry> dvTableSearchEntityFiles = ZipHelper.getFilesInPathFromZip(stream, "dvtablesearchentities/", ".xml");
                var dvTableSearchEntities = new List<DvTableSearchEntity>();
                foreach (ZipArchiveEntry dvtseFile in dvTableSearchEntityFiles)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + dvtseFile.Name;
                    dvtseFile.ExtractToFile(tempFile, true);
                    var dvtseDoc = new XmlDocument();
                    dvtseDoc.Load(tempFile);
                    var dvtseNode = dvtseDoc.SelectSingleNode("/dvtablesearchentity");
                    if (dvtseNode != null)
                    {
                        dvTableSearchEntities.Add(new DvTableSearchEntity
                        {
                            Id = dvtseNode.Attributes["dvtablesearchentityid"]?.Value,
                            DvTableSearchId = dvtseNode.SelectSingleNode("dvtablesearch/dvtablesearchid")?.InnerText,
                            EntityLogicalName = dvtseNode.SelectSingleNode("entitylogicalname")?.InnerText,
                            Name = dvtseNode.SelectSingleNode("name")?.InnerText,
                            StateCode = int.TryParse(dvtseNode.SelectSingleNode("statecode")?.InnerText, out var dvtseSc) ? dvtseSc : 0,
                            StatusCode = int.TryParse(dvtseNode.SelectSingleNode("statuscode")?.InnerText, out var dvtseStc) ? dvtseStc : 0
                        });
                    }
                    File.Delete(tempFile);
                }
                // Parse copilotsynonyms
                List<ZipArchiveEntry> synonymFiles = ZipHelper.getFilesInPathFromZip(stream, "copilotsynonyms/", ".xml");
                var copilotSynonyms = new List<CopilotSynonym>();
                foreach (ZipArchiveEntry synFile in synonymFiles)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + synFile.Name;
                    synFile.ExtractToFile(tempFile, true);
                    var synDoc = new XmlDocument();
                    synDoc.Load(tempFile);
                    var synNode = synDoc.SelectSingleNode("/copilotsynonyms");
                    if (synNode != null)
                    {
                        copilotSynonyms.Add(new CopilotSynonym
                        {
                            Id = synNode.Attributes["copilotsynonymsid"]?.Value,
                            ColumnLogicalName = synNode.SelectSingleNode("columnlogicalname")?.InnerText,
                            Description = synNode.SelectSingleNode("description/label")?.Attributes?["description"]?.Value
                                         ?? synNode.SelectSingleNode("description")?.Attributes?["default"]?.Value,
                            DvTableSearchEntityId = synNode.SelectSingleNode("skillentity/dvtablesearchentityid")?.InnerText,
                            StateCode = int.TryParse(synNode.SelectSingleNode("statecode")?.InnerText, out var synSc) ? synSc : 0,
                            StatusCode = int.TryParse(synNode.SelectSingleNode("statuscode")?.InnerText, out var synStc) ? synStc : 0
                        });
                    }
                    File.Delete(tempFile);
                }
                // Assign parsed Dataverse knowledge data to all agents
                foreach (AgentEntity agent in Agents)
                {
                    agent.DvTableSearches = dvTableSearches;
                    agent.DvTableSearchEntities = dvTableSearchEntities;
                    agent.CopilotSynonyms = copilotSynonyms;
                }

                // Parse aiplugins
                List<ZipArchiveEntry> aiPluginFiles = ZipHelper.getFilesInPathFromZip(stream, "aiplugins/", ".xml");
                var aiPlugins = new List<AIPluginEntity>();
                foreach (ZipArchiveEntry pluginFile in aiPluginFiles)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + pluginFile.Name;
                    pluginFile.ExtractToFile(tempFile, true);
                    var pluginDoc = new XmlDocument();
                    pluginDoc.Load(tempFile);
                    var pluginNode = pluginDoc.SelectSingleNode("/aiplugin");
                    if (pluginNode != null)
                    {
                        aiPlugins.Add(new AIPluginEntity
                        {
                            Name = pluginNode.Attributes["name"]?.Value,
                            HumanName = pluginNode.SelectSingleNode("humanname")?.InnerText,
                            ModelName = pluginNode.SelectSingleNode("modelname")?.InnerText,
                            PluginSubType = int.TryParse(pluginNode.SelectSingleNode("pluginsubtype")?.InnerText, out var pst) ? pst : 0,
                            PluginType = int.TryParse(pluginNode.SelectSingleNode("plugintype")?.InnerText, out var pt) ? pt : 0,
                            StateCode = int.TryParse(pluginNode.SelectSingleNode("statecode")?.InnerText, out var plugSc) ? plugSc : 0,
                            StatusCode = int.TryParse(pluginNode.SelectSingleNode("statuscode")?.InnerText, out var plugStc) ? plugStc : 0
                        });
                    }
                    File.Delete(tempFile);
                }

                // Parse aipluginoperations
                List<ZipArchiveEntry> aiPluginOpFiles = ZipHelper.getFilesInPathFromZip(stream, "aipluginoperations/", ".xml");
                var aiPluginOperations = new List<AIPluginOperationEntity>();
                foreach (ZipArchiveEntry opFile in aiPluginOpFiles)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + opFile.Name;
                    opFile.ExtractToFile(tempFile, true);
                    var opDoc = new XmlDocument();
                    opDoc.Load(tempFile);
                    var opNode = opDoc.SelectSingleNode("/aipluginoperation");
                    if (opNode != null)
                    {
                        aiPluginOperations.Add(new AIPluginOperationEntity
                        {
                            AIPluginName = opNode.Attributes["aiplugin.name"]?.Value,
                            OperationId = opNode.Attributes["operationid"]?.Value,
                            Name = opNode.SelectSingleNode("name")?.InnerText,
                            AIModelId = opNode.SelectSingleNode("msdyn_aimodel/msdyn_aimodelid")?.InnerText,
                            CustomApiUniqueName = opNode.SelectSingleNode("customapi/uniquename")?.InnerText,
                            IsConsequential = int.TryParse(opNode.SelectSingleNode("isconsequential")?.InnerText, out var ic2) ? ic2 : 0,
                            StateCode = int.TryParse(opNode.SelectSingleNode("statecode")?.InnerText, out var opSc) ? opSc : 0,
                            StatusCode = int.TryParse(opNode.SelectSingleNode("statuscode")?.InnerText, out var opStc) ? opStc : 0
                        });
                    }
                    File.Delete(tempFile);
                }

                // Parse customapis
                List<ZipArchiveEntry> customApiFiles = ZipHelper.getFilesInPathFromZip(stream, "customapis/", "customapi.xml");
                var customApis = new List<CustomApiEntity>();
                foreach (ZipArchiveEntry caFile in customApiFiles)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + caFile.Name;
                    caFile.ExtractToFile(tempFile, true);
                    var caDoc = new XmlDocument();
                    caDoc.Load(tempFile);
                    var caNode = caDoc.SelectSingleNode("/customapi");
                    if (caNode != null)
                    {
                        var customApi = new CustomApiEntity
                        {
                            UniqueName = caNode.Attributes["uniquename"]?.Value,
                            Name = caNode.SelectSingleNode("name")?.InnerText,
                            DisplayName = caNode.SelectSingleNode("displayname/label")?.Attributes?["description"]?.Value
                                          ?? caNode.SelectSingleNode("displayname")?.Attributes?["default"]?.Value,
                            Description = caNode.SelectSingleNode("description/label")?.Attributes?["description"]?.Value
                                          ?? caNode.SelectSingleNode("description")?.Attributes?["default"]?.Value
                        };

                        // Parse request parameters in the same directory
                        string caDir = caFile.FullName.Replace("customapi.xml", "");
                        List<ZipArchiveEntry> reqParamFiles = ZipHelper.getFilesInPathFromZip(stream, caDir + "customapirequestparameters/", ".xml");
                        foreach (ZipArchiveEntry rpFile in reqParamFiles)
                        {
                            tempFile = Path.GetDirectoryName(filename) + @"\" + rpFile.Name;
                            rpFile.ExtractToFile(tempFile, true);
                            var rpDoc = new XmlDocument();
                            rpDoc.Load(tempFile);
                            var rpNode = rpDoc.SelectSingleNode("/customapirequestparameter");
                            if (rpNode != null)
                            {
                                customApi.RequestParameters.Add(new CustomApiRequestParameterEntity
                                {
                                    UniqueName = rpNode.Attributes["uniquename"]?.Value,
                                    Name = rpNode.SelectSingleNode("name")?.InnerText,
                                    DisplayName = rpNode.SelectSingleNode("displayname/label")?.Attributes?["description"]?.Value
                                                  ?? rpNode.SelectSingleNode("displayname")?.Attributes?["default"]?.Value,
                                    Description = rpNode.SelectSingleNode("description/label")?.Attributes?["description"]?.Value
                                                  ?? rpNode.SelectSingleNode("description")?.Attributes?["default"]?.Value,
                                    Type = int.TryParse(rpNode.SelectSingleNode("type")?.InnerText, out var rpType) ? rpType : 0,
                                    IsOptional = rpNode.SelectSingleNode("isoptional")?.InnerText == "1"
                                });
                            }
                            File.Delete(tempFile);
                        }

                        // Parse response properties in the same directory
                        List<ZipArchiveEntry> resPropFiles = ZipHelper.getFilesInPathFromZip(stream, caDir + "customapiresponseproperties/", ".xml");
                        foreach (ZipArchiveEntry rpFile in resPropFiles)
                        {
                            tempFile = Path.GetDirectoryName(filename) + @"\" + rpFile.Name;
                            rpFile.ExtractToFile(tempFile, true);
                            var rpDoc = new XmlDocument();
                            rpDoc.Load(tempFile);
                            var rpNode = rpDoc.SelectSingleNode("/customapiresponseproperty");
                            if (rpNode != null)
                            {
                                customApi.ResponseProperties.Add(new CustomApiResponsePropertyEntity
                                {
                                    UniqueName = rpNode.Attributes["uniquename"]?.Value,
                                    Name = rpNode.SelectSingleNode("name")?.InnerText,
                                    DisplayName = rpNode.SelectSingleNode("displayname/label")?.Attributes?["description"]?.Value
                                                  ?? rpNode.SelectSingleNode("displayname")?.Attributes?["default"]?.Value,
                                    Description = rpNode.SelectSingleNode("description/label")?.Attributes?["description"]?.Value
                                                  ?? rpNode.SelectSingleNode("description")?.Attributes?["default"]?.Value,
                                    Type = int.TryParse(rpNode.SelectSingleNode("type")?.InnerText, out var rpType) ? rpType : 0
                                });
                            }
                            File.Delete(tempFile);
                        }

                        customApis.Add(customApi);
                    }
                    else
                    {
                        File.Delete(tempFile);
                    }
                }

                // Assign parsed AI plugin/API data to all agents
                foreach (AgentEntity agent in Agents)
                {
                    agent.AIPlugins = aiPlugins;
                    agent.AIPluginOperations = aiPluginOperations;
                    agent.CustomApis = customApis;
                }

                //process customizations.xml to retrieve AI Models, Connectors, and Connection References
                ZipArchiveEntry customizationsDefinition = ZipHelper.getCustomizationsDefinitionFileFromZip(stream);
                var connectors = new List<ConnectorDefinition>();
                var connectionReferences = new List<ConnectionReferenceDefinition>();
                if (customizationsDefinition != null)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + customizationsDefinition.Name;
                    customizationsDefinition.ExtractToFile(tempFile, true);
                    NotificationHelper.SendNotification("  - Processing customizations.xml ");
                    using (FileStream customizations = new FileStream(tempFile, FileMode.Open))
                    {
                        CustomizationsEntity customizationsEntity = CustomizationsParser.parseCustomizationsDefinition(customizations);
                        AIModels = customizationsEntity.getAIModels().ToList();
                        connectors = customizationsEntity.getConnectors();
                        connectionReferences = customizationsEntity.getConnectionReferences();
                    }
                    File.Delete(tempFile);
                }

                // Load OpenAPI definitions and connection parameters from Connector/ folder
                foreach (var connector in connectors)
                {
                    if (!string.IsNullOrEmpty(connector.Name))
                    {
                        string openApiPath = $"Connector/{connector.Name}_openapidefinition.json";
                        ZipArchiveEntry openApiEntry = ZipHelper.getFileFromZip(stream, openApiPath);
                        if (openApiEntry != null)
                        {
                            tempFile = Path.GetDirectoryName(filename) + @"\" + openApiEntry.Name;
                            openApiEntry.ExtractToFile(tempFile, true);
                            connector.OpenApiDefinitionJson = File.ReadAllText(tempFile);
                            File.Delete(tempFile);
                        }
                        string connParamsPath = $"Connector/{connector.Name}_connectionparameters.json";
                        ZipArchiveEntry connParamsEntry = ZipHelper.getFileFromZip(stream, connParamsPath);
                        if (connParamsEntry != null)
                        {
                            tempFile = Path.GetDirectoryName(filename) + @"\" + connParamsEntry.Name;
                            connParamsEntry.ExtractToFile(tempFile, true);
                            connector.ConnectionParametersJson = File.ReadAllText(tempFile);
                            File.Delete(tempFile);
                        }
                        string policyPath = $"Connector/{connector.Name}_policytemplateinstances.json";
                        ZipArchiveEntry policyEntry = ZipHelper.getFileFromZip(stream, policyPath);
                        if (policyEntry != null)
                        {
                            tempFile = Path.GetDirectoryName(filename) + @"\" + policyEntry.Name;
                            policyEntry.ExtractToFile(tempFile, true);
                            string policyContent = File.ReadAllText(tempFile);
                            if (!string.IsNullOrWhiteSpace(policyContent) && policyContent.Trim() != "null")
                                connector.PolicyTemplateInstancesJson = policyContent;
                            File.Delete(tempFile);
                        }
                        // Try common image extensions for the connector icon blob
                        foreach (string ext in new[] { "Png", "png", "jpg", "jpeg", "svg" })
                        {
                            string iconPath = $"Connector/{connector.Name}_iconblob.{ext}";
                            ZipArchiveEntry iconEntry = ZipHelper.getFileFromZip(stream, iconPath);
                            if (iconEntry != null)
                            {
                                using (var ms = new MemoryStream())
                                {
                                    using (var entryStream = iconEntry.Open())
                                    {
                                        entryStream.CopyTo(ms);
                                    }
                                    connector.IconBlobBase64 = Convert.ToBase64String(ms.ToArray());
                                }
                                break;
                            }
                        }
                    }
                }

                // Assign AI Models, Connectors, and Connection References to all agents
                foreach (AgentEntity agent in Agents)
                {
                    agent.AIModels = AIModels;
                    agent.Connectors = connectors;
                    agent.ConnectionReferences = connectionReferences;
                }
            }
            else
            {
                NotificationHelper.SendNotification("Invalid file " + filename);
            }
        }

        public List<AgentEntity> getAgents()
        {
            return Agents;
        }
    }
}
