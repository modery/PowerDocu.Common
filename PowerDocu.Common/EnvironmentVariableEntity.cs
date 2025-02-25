using System.Collections.Generic;

namespace PowerDocu.Common
{
    public class EnvironmentVariableEntity
    {
        public string Name;
        public string DisplayName;
        public string DefaultValue;
        public string IntroducedVersion;
        public bool IsCustomizable;
        public bool IsRequired;
        public string Type;
        public string DescriptionDefault;
        public Dictionary<string, string> Descriptions = new Dictionary<string, string>();
        public Dictionary<string, string> LocalizedNames = new Dictionary<string, string>();

        public string getTypeDisplayName()
        {
            //https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/environmentvariabledefinition#type-choicesoptions
            switch (Type)
            {
                case "100000000":
                    return "String";
                case "100000001":
                    return "Number";
                case "100000002":
                    return "Boolean";
                case "100000003":
                    return "JSON";
                case "100000004":
                    return "Data Source";
                case "100000005":
                    return "Secret";
                default:
                    return Type;
            }
        }
    }
}
