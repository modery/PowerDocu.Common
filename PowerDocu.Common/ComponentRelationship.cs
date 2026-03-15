namespace PowerDocu.Common
{
    public class ComponentRelationship
    {
        public string SourceType { get; set; }
        public string SourceName { get; set; }
        public string TargetType { get; set; }
        public string TargetName { get; set; }
        public string RelationshipLabel { get; set; }

        public ComponentRelationship(string sourceType, string sourceName, string targetType, string targetName, string relationshipLabel)
        {
            SourceType = sourceType;
            SourceName = sourceName;
            TargetType = targetType;
            TargetName = targetName;
            RelationshipLabel = relationshipLabel;
        }
    }

    public class SolutionComponentNode
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public SolutionComponentNode(string type, string name)
        {
            Type = type;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            if (obj is SolutionComponentNode other)
                return Type == other.Type && Name == other.Name;
            return false;
        }

        public override int GetHashCode()
        {
            return (Type + "::" + Name).GetHashCode();
        }
    }
}
