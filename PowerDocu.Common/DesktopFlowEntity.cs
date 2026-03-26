using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PowerDocu.Common
{
    public class DesktopFlowEntity
    {
        public string ID;
        public string Name;
        public string Description;
        public int Category = 6;
        public int UIFlowType;
        public int StateCode;
        public int StatusCode;
        public string IntroducedVersion;
        public bool IsCustomizable;
        public string SchemaVersion;
        public string RobinScript;
        public string MetadataJson;
        public string DependenciesJson;
        public string ConnectionReferencesJson;
        public Dictionary<string, string> LocalizedNames = new Dictionary<string, string>();

        // Parsed from Robin Script
        public List<RobinActionStep> ActionSteps = new List<RobinActionStep>();
        public List<RobinVariable> Variables = new List<RobinVariable>();
        public List<RobinImport> Imports = new List<RobinImport>();
        public List<RobinControlFlowBlock> ControlFlowBlocks = new List<RobinControlFlowBlock>();
        public Dictionary<string, string> ConnectionStrings = new Dictionary<string, string>();
        public List<DesktopFlowSubflow> Subflows = new List<DesktopFlowSubflow>();

        // Parsed from ManifestFile (V2 schema)
        public DesktopFlowEngineVersion CreatedEngineVersion;
        public DesktopFlowEngineVersion EngineVersion;
        public List<DesktopFlowModuleReference> Modules = new List<DesktopFlowModuleReference>();
        public bool PowerFxEnabled;
        public string PowerFxVersion;

        // Parsed from DependenciesFile
        public List<DesktopFlowEnvironmentVariable> EnvironmentVariables = new List<DesktopFlowEnvironmentVariable>();

        // Parsed from ConnectorDefinition (optional)
        public List<DesktopFlowConnector> Connectors = new List<DesktopFlowConnector>();

        // Parsed from ControlRepository
        public List<string> ScreenNames = new List<string>();

        public string GetDisplayName()
        {
            if (LocalizedNames.ContainsKey("1033"))
                return LocalizedNames["1033"];
            if (!string.IsNullOrEmpty(Name))
                return Name;
            return ID;
        }

        public string GetStateLabel()
        {
            return StateCode switch
            {
                0 => "Draft",
                1 => "Active",
                2 => "Suspended",
                _ => "Unknown"
            };
        }

        public string GetEngineVersionString()
        {
            if (EngineVersion != null)
                return $"{EngineVersion.Major}.{EngineVersion.Minor}.{EngineVersion.Build}.{EngineVersion.Revision}";
            return "";
        }
    }

    public class DesktopFlowEngineVersion
    {
        public int Major;
        public int Minor;
        public int Build;
        public int Revision;
    }

    public class DesktopFlowModuleReference
    {
        public string Name;
        public string AssemblyName;
        public string Version;
    }

    public class DesktopFlowEnvironmentVariable
    {
        public string Name;
        public string Type;
        public string Value;
    }

    public class DesktopFlowConnector
    {
        public string ConnectorId;
        public string Name;
        public string Title;
        public JObject SwaggerDefinition;
    }

    public class RobinActionStep
    {
        public int Order;
        public string ModuleName;
        public string ActionName;
        public string SubActionName;
        public string FullActionName;
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public List<string> OutputVariables = new List<string>();
        public string RawScript;
        public int NestingLevel;
    }

    public class RobinVariable
    {
        public string Name;
        public string Type;
        public string InitialValue;
        public bool IsSensitive;
        public bool IsOutput;
        public bool IsInput;
    }

    public class RobinImport
    {
        public string Path;
        public string Alias;
    }

    public class RobinControlFlowBlock
    {
        public string Type;
        public string Condition;
        public int StartLine;
        public int EndLine;
        public int NestingLevel;
    }

    public class DesktopFlowSubflow
    {
        public string Name;
        public bool IsGlobal;
        public string RobinScript;
        public List<RobinActionStep> ActionSteps = new List<RobinActionStep>();
        public List<RobinVariable> Variables = new List<RobinVariable>();
        public List<RobinControlFlowBlock> ControlFlowBlocks = new List<RobinControlFlowBlock>();
    }
}
