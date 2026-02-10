using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace PowerDocu.Common
{
    public class AIModel
    {
        private readonly XmlNode xmlEntity;
        public List<AIConfiguration> AIConfigurations { get; set; } = new List<AIConfiguration>();

        public AIModel(XmlNode xmlEntity)
        {
            this.xmlEntity = xmlEntity;
        }

        public string getLocalizedName()
        {
            return xmlEntity.SelectSingleNode("Name")?.Attributes.GetNamedItem("LocalizedName")?.InnerText ?? "";
        }

        public string getName()
        {
            return xmlEntity.SelectSingleNode("msdyn_name")?.InnerText ?? "";
        }

        public string getID()
        {
            return xmlEntity.SelectSingleNode("msdyn_aimodelid")?.InnerText ?? "";
        }

        public string getTemplateId()
        {
            return xmlEntity.SelectSingleNode("msdyn_templateid")?.InnerText ?? "";
        }

        public string getPrompt()
        {

            string promptString = xmlEntity.SelectSingleNode("AIConfigurations/AIConfiguration[msdyn_type='190690001']/msdyn_customconfiguration")?.InnerText;
            JObject cardJson = JObject.Parse(promptString);
            cardJson.TryGetValue("prompt", out JToken promptToken);
            string promptForDocumentation = "";
            foreach (JToken promptParts in promptToken.Children())
            {
                JToken promptPartType = promptParts["type"];
                switch (promptPartType.ToString())
                {
                    case "literal":
                        promptForDocumentation += promptParts["text"]?.ToString();
                        break;
                    //Variable
                    case "inputVariable":
                        promptForDocumentation += "{{" + promptParts["id"]?.ToString() + "}}";
                        break;
                    //Dataverse
                    case "data":
                        promptForDocumentation += "{{" + promptParts["text"]?.ToString() + "}}";
                        break;
                    default:
                        promptForDocumentation += "<unknown> ";
                        break;
                }

            }

            return promptForDocumentation;
        }

        public List<AIModelInput> getInputs()
        {
            JArray inputs = getDefinition()["inputs"] as JArray;
            return inputs.ToObject<List<AIModelInput>>();
        }


        public AIModelOutput getOutput()
        {
            JObject output = getDefinition()["output"] as JObject;
            if (output == null)
                return null;
            AIModelOutput aiModelOutput = new AIModelOutput();
            aiModelOutput.Formats = output["formats"]?.ToObject<string[]>();
            aiModelOutput.jsonSchema = output["jsonSchema"]?.ToString();
            aiModelOutput.jsonExamples = output["jsonExamples"]?.ToString();
            return aiModelOutput;
        }

        private JObject getDefinition()
        {
            string promptString = xmlEntity.SelectSingleNode("AIConfigurations/AIConfiguration[msdyn_type='190690001']/msdyn_customconfiguration")?.InnerText;
            JObject cardJson = JObject.Parse(promptString);
            cardJson.TryGetValue("definitions", out JToken definition);
            return (JObject)definition;
        }
    }

    public class AIConfiguration
    {
        public string Id { get; set; }
        public string CustomConfiguration { get; set; }
        public int MajorIterationNumber { get; set; }
        public int MinorIterationNumber { get; set; }
        public string Name { get; set; }
        public string ModelRunDataSpecification { get; set; }
        public string ModelData { get; set; }
        public int Type { get; set; }
        public AIModel aIModel { get; set; }
        public string msdyn_trainedmodelaiconfigurationpareid { get; set; }
        public int IsCustomizable { get; set; }
        public int TemplateVersion { get; set; }
    }

    public class AIModelInput
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public string QuickTextValue { get; set; }
    }

    public class AIModelOutput
    {
        public string[] Formats;
        public string jsonSchema;
        public string jsonExamples;
    }
}
