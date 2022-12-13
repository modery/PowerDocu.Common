using System.Linq;
using System.Collections.Generic;

namespace PowerDocu.Common
{
    public class SolutionEntity
    {
        public string UniqueName;
        //public string LocalizedName;
        public string Version;
        public bool isManaged;
        public SolutionPublisher Publisher;
        public List<SolutionComponent> Components = new List<SolutionComponent>();
        public List<SolutionComponent> Dependencies = new List<SolutionComponent>();

        public List<string> GetComponentTypes()
        {
            return Components.GroupBy(p => p.Type).Select(g => g.First()).OrderBy(t => t.Type).Select(t => t.Type).ToList();
        }
    }

    public class SolutionPublisher
    {
        public string UniqueName;
        public string EMailAddress;
        public string SupportingWebsiteUrl;
        public string CustomizationPrefix;
        public string CustomizationOptionValuePrefix;
        //Descriptions
        //LocalizedNames
        //addresses
    }

    public class SolutionComponent
    {
        public string Type;
        public string SchemaName;
        public string ID;
        public string DisplayName;
        public string Solution;
    }

    public class SolutionDependency
    {
        public SolutionComponent Required;
        public SolutionComponent Dependent;
    }
}