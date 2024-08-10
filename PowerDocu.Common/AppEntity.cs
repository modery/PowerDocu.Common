using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerDocu.Common
{
    public class AppEntity
    {
        public string ID;
        public string Name;
        public List<ControlEntity> Controls = new List<ControlEntity>();
        public List<Expression> Properties = new List<Expression>();
        public List<DataSource> DataSources = new List<DataSource>();
        public List<Resource> Resources = new List<Resource>();
        public HashSet<string> GlobalVariables = new HashSet<string>();
        public HashSet<string> ContextVariables = new HashSet<string>();
        public HashSet<string> Collections = new HashSet<string>();
        public Dictionary<string, List<ControlPropertyReference>> VariableCollectionControlReferences = new Dictionary<string, List<ControlPropertyReference>>();
        public Dictionary<ControlEntity, List<string>> ScreenNavigations = new Dictionary<ControlEntity, List<string>>();
        public Dictionary<string, MemoryStream> ResourceStreams = new Dictionary<string, MemoryStream>();
        public AppEntity()
        {
        }
    }

    public class DataSource
    {
        public string Name;
        public string Type;
        public List<Expression> Properties = new List<Expression>();

        public bool isSampleDataSource()
        {
            return ((string)Properties.FirstOrDefault(o => o.expressionOperator.Equals("IsSampleData"))?.expressionOperands[0]) == "True";
        }
    }

    public class Resource
    {
        public string Name;
        public string Content;
        public string ResourceKind;
        public List<Expression> Properties = new List<Expression>();

        public bool isSampleResource()
        {
            return ((string)Properties.First(o => o.expressionOperator.Equals("IsSampleData")).expressionOperands[0]) == "True";
        }
    }

    public class ControlPropertyReference
    {
        public ControlEntity Control;
        public string RuleProperty;
    }
}