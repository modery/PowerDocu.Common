using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace PowerDocu.Common
{
    public class AgentEntity
    {
        public string SchemaName { get; set; }
        public int AuthenticationMode { get; set; }
        public int AuthenticationTrigger { get; set; }
        public string IconBase64 { get; set; }
        public int IsCustomizable { get; set; }
        public int Language { get; set; }
        public string Name { get; set; }
        public int RuntimeProvider { get; set; }
        public string SynchronizationStatus { get; set; }
        public string Template { get; set; }
        public int TimeZoneRuleVersionNumber { get; set; }
        public AgentConfiguration Configuration { get; set; }
        public List<BotComponent> BotComponents { get; set; } = new List<BotComponent>();

        public AgentEntity() { }
        public List<BotComponent> GetTopics()
        {
            return GetBotComponents(9, "topic");
        }

        public List<BotComponent> GetKnowledge()
        {
            //websites are stored as knowledge folders
            //Dataverse tables are stored differently.  dvtablesearchentities ?
            return GetBotComponents(16, "knowledge");
        }

        public List<BotComponent> GetTools()
        {
            return GetBotComponents(9, "component");
        }

        public List<BotComponent> GetGptDefault()
        {
            return GetBotComponents(15, "gpt");
        }

        private List<BotComponent> GetBotComponents(int type, string subType)
        {
            return BotComponents.Where(bc => bc.ComponentType == type && bc.SchemaName.StartsWith($"{SchemaName}.{subType}.")).ToList();
        }

        public string GetDescription()
        {
            return GetGptDefault().FirstOrDefault()?.Description;
        }

        public string GetInstructions()
        {
            var mapping = GetGptDefault().FirstOrDefault()?.GetYamlMappingNode();
            return ((YamlScalarNode)mapping.Children[new YamlScalarNode("instructions")]).Value ?? string.Empty;
        }

        public Dictionary<string, string> GetSuggestedPrompts()
        {
            Dictionary<string, string> conversationStarters = new Dictionary<string, string>();
            var mapping = GetGptDefault().FirstOrDefault()?.GetYamlMappingNode();
            if (mapping.Children.TryGetValue(new YamlScalarNode("conversationStarters"), out var conversationsStartsNode) && conversationsStartsNode is YamlSequenceNode conversationsStartersSequence)
            {
                foreach (var conversationsStarter in conversationsStartersSequence)
                {
                    //var text = ((YamlScalarNode)action.Children[new YamlScalarNode("text")]).Value ?? string.Empty;
                    //var image = ((YamlScalarNode)action.Children[new YamlScalarNode("image")]).Value ?? string.Empty;
                    //conversationStarters[text] = image;
                    conversationStarters[conversationsStarter["title"].ToString()] = conversationsStarter["text"].ToString();
                }
            }
            return conversationStarters;
        }

        public string GetOrchestration()
        {
            return Configuration.settings.GenerativeActionsEnabled ? "Enabled" : "Disabled";
        }

        public string GetResponseModel()
        {
            return "TODO";
        }

    }

    public class BotComponent
    {
        public string SchemaName { get; set; }
        public int ComponentType { get; set; }
        public int IsCustomizable { get; set; }
        public string Name { get; set; }
        public string ParentBotSchemaName { get; set; }
        public int StateCode { get; set; }
        public int StatusCode { get; set; }
        public string YamlData { get; set; }
        public string Description { get; set; }


        public string GetTriggerTypeForTopic()
        {
            var mapping = GetYamlMappingNode();
            var yaml = new YamlStream();
            //pasrse the topic YAML data
            var input = new StringReader(YamlData);
            yaml.Load(input);
            var triggerYaml = (YamlMappingNode)mapping.Children[new YamlScalarNode("beginDialog")];
            return GetTriggerTypeDisplayName(triggerYaml.Children[new YamlScalarNode("kind")].ToString());
        }

        public YamlMappingNode GetYamlMappingNode()
        {
            var input = new StringReader(YamlData.Replace("@odata", "odata"));
            var yaml = new YamlStream();
            yaml.Load(input);
            return (YamlMappingNode)yaml.Documents[0].RootNode;
        }

        private string GetTriggerTypeDisplayName(string triggerType)
        {
            switch (triggerType)
            {
                case "OnRecognizedIntent":
                    return "By agent";
                case "OnEscalate":
                    return "On Talk to Representative";
                case "OnUnknownIntent":
                    return "Unknown topic";
                case "OnConversationStart":
                    return "On Conversation Start";
                case "OnSystemRedirect":
                    return "Redirect";
                case "OnError":
                    return "On Error";
                case "OnSignIn":
                    return "On Sign In";
                case "OnSelectIntent":
                    return "Selection";
                default:
                    return triggerType;
            }
        }
    }

    public class AgentConfiguration
    {
        public string kind { get; set; }
        public Settings settings { get; set; }
        public GPTSettings gPTSettings { get; set; }
        public AISettings aISettings { get; set; }
        public Recognizer recognizer { get; set; }

        public class Settings
        {
            public bool GenerativeActionsEnabled { get; set; }
        }

        public class GPTSettings
        {
            public string kind { get; set; }
            public string defaultSchemaName { get; set; }
        }

        public class AISettings
        {
            public string kind { get; set; }
            public bool useModelKnowledge { get; set; }
            public bool isFileAnalysisEnabled { get; set; }
            public bool isSemanticSearchEnabled { get; set; }
            public string contentModeration { get; set; }
            public bool optInUseLatestModels { get; set; }
        }

        public class Recognizer
        {
            public string kind { get; set; }
        }
    }

}
