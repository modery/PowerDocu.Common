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
                        }
                        File.Delete(tempFile);
                        // Load the data file in the same path as topicFile and set YamlData
                        string dataFilePath = Path.Combine(Path.GetDirectoryName(tempFile), "data");
                        tempFile = Path.GetDirectoryName(filename) + @"\data";
                        ZipArchiveEntry dataFile = ZipHelper.getFileFromZip(stream, botComponentFile.FullName.Replace("botcomponent.xml", "data"));
                        dataFile.ExtractToFile(tempFile, true);
                        botComponent.YamlData = File.ReadAllText(dataFilePath);
                        File.Delete(tempFile);
                        agent.BotComponents.Add(botComponent);
                    }
                }
                //process customizations.xml to retrieve the AI Models
                ZipArchiveEntry customizationsDefinition = ZipHelper.getCustomizationsDefinitionFileFromZip(stream);
                if (customizationsDefinition != null)
                {
                    tempFile = Path.GetDirectoryName(filename) + @"\" + customizationsDefinition.Name;
                    customizationsDefinition.ExtractToFile(tempFile, true);
                    NotificationHelper.SendNotification("  - Processing customizations.xml ");
                    using (FileStream customizations = new FileStream(tempFile, FileMode.Open))
                    {
                        // todo start processing customizations.xml
                        CustomizationsEntity customizationsEntity = CustomizationsParser.parseCustomizationsDefinition(customizations);
                        AIModels = customizationsEntity.getAIModels().ToList();
                    }
                    File.Delete(tempFile);
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
