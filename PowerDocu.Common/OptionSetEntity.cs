using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace PowerDocu.Common
{
    public class OptionSetEntity
    {
        public string Name;
        public string LocalizedName;
        public string Description;
        public string OptionSetType;
        public bool IsGlobal;
        public bool IsCustomizable;
        public List<OptionSetOption> Options = new List<OptionSetOption>();
        public Dictionary<string, string> LocalizedNames = new Dictionary<string, string>();
        public Dictionary<string, string> Descriptions = new Dictionary<string, string>();

        public OptionSetEntity(XmlNode xmlOptionSet)
        {
            Name = xmlOptionSet.Attributes?.GetNamedItem("Name")?.InnerText;
            LocalizedName = xmlOptionSet.Attributes?.GetNamedItem("localizedName")?.InnerText;

            // Read child node properties
            OptionSetType = xmlOptionSet.SelectSingleNode("OptionSetType")?.InnerText;
            IsGlobal = xmlOptionSet.SelectSingleNode("IsGlobal")?.InnerText == "1";
            IsCustomizable = xmlOptionSet.SelectSingleNode("IsCustomizable")?.InnerText == "1";

            // Parse DisplayNames
            XmlNode displayNames = xmlOptionSet.SelectSingleNode("displaynames");
            if (displayNames != null)
            {
                foreach (XmlNode displayName in displayNames.ChildNodes)
                {
                    string languageCode = displayName.Attributes?.GetNamedItem("languagecode")?.InnerText;
                    string description = displayName.Attributes?.GetNamedItem("description")?.InnerText;
                    if (!string.IsNullOrEmpty(languageCode) && !string.IsNullOrEmpty(description))
                    {
                        LocalizedNames[languageCode] = description;
                    }
                }
            }

            // Parse Descriptions
            XmlNode descriptions = xmlOptionSet.SelectSingleNode("Descriptions");
            if (descriptions != null)
            {
                foreach (XmlNode description in descriptions.ChildNodes)
                {
                    string languageCode = description.Attributes?.GetNamedItem("languagecode")?.InnerText;
                    string desc = description.Attributes?.GetNamedItem("description")?.InnerText;
                    if (!string.IsNullOrEmpty(languageCode) && !string.IsNullOrEmpty(desc))
                    {
                        Descriptions[languageCode] = desc;
                        if (string.IsNullOrEmpty(Description))
                            Description = desc;
                    }
                }
            }

            // Parse Options
            XmlNode options = xmlOptionSet.SelectSingleNode("options");
            if (options != null)
            {
                foreach (XmlNode option in options.ChildNodes)
                {
                    Options.Add(new OptionSetOption(option));
                }
            }
        }

        public string GetDisplayName()
        {
            // First try LocalizedName
            if (!string.IsNullOrEmpty(LocalizedName))
            {
                return LocalizedName;
            }
            // Try English (1033) from LocalizedNames dictionary
            if (LocalizedNames.ContainsKey("1033"))
            {
                string englishName = LocalizedNames["1033"];
                if (!string.IsNullOrEmpty(englishName))
                {
                    return englishName;
                }
            }
            // Get the first non-empty value from LocalizedNames
            string anyName = LocalizedNames.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
            if (!string.IsNullOrEmpty(anyName))
            {
                return anyName;
            }
            return string.Empty;
        }
    }

    public class OptionSetOption
    {
        public string Value;
        public string Label;
        public Dictionary<string, string> Labels = new Dictionary<string, string>();

        public OptionSetOption(XmlNode xmlOption)
        {
            Value = xmlOption.Attributes?.GetNamedItem("value")?.InnerText;

            // Get labels for all languages
            XmlNode labels = xmlOption.SelectSingleNode("labels");
            if (labels != null)
            {
                foreach (XmlNode label in labels.ChildNodes)
                {
                    string languageCode = label.Attributes?.GetNamedItem("languagecode")?.InnerText;
                    string description = label.Attributes?.GetNamedItem("description")?.InnerText;
                    if (!string.IsNullOrEmpty(languageCode) && !string.IsNullOrEmpty(description))
                    {
                        Labels[languageCode] = description;
                        if (string.IsNullOrEmpty(Label))
                            Label = description;
                    }
                }
            }
        }
    }
}
