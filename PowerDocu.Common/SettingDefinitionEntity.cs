namespace PowerDocu.Common
{
    public class SettingDefinitionEntity
    {
        public string UniqueName;
        public string DisplayName;
        public string Description;
        public string DataType;
        public string DefaultValue;
        public bool IsCustomizable;
        public bool IsHidden;
        public bool IsOverridable;

        public string GetDataTypeDisplayName()
        {
            return DataType switch
            {
                "0" => "String",
                "1" => "Number",
                "2" => "Boolean",
                "3" => "JSON",
                _ => DataType ?? "Unknown"
            };
        }
    }
}
