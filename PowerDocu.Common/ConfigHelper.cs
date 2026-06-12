using System;
using System.IO;
using Newtonsoft.Json;

namespace PowerDocu.Common
{
    public class ConfigHelper
    {
        public string outputFormat = OutputFormatHelper.All;
        // true to document changes only, false to document all properties
        public bool documentChangesOnlyCanvasApps = true;
        public bool documentDefaultValuesCanvasApps = true;
        public string flowActionSortOrder = FlowActionSortOrderHelper.ByName;
        public string wordTemplate = null;
        public bool documentSampleData = false;
        public bool documentSolution = true;
        public bool documentFlows = true;
        public bool documentAgents = true;
        public bool documentModelDrivenApps = true;
        public bool documentBusinessProcessFlows = true;
        public bool documentDesktopFlows = true;
        public bool documentClassicWorkflows = true;
        public bool documentApps = true;
        public bool documentAppProperties = true;
        public bool documentAppVariables = true;
        public bool documentAppDataSources = true;
        public bool documentAppResources = true;
        public bool documentAppControls = true;
        public bool documentDefaultColumns = false;
        public bool addTableOfContents = false;
        public bool showAllComponentsInGraph = true;
        public bool checkForUpdatesOnLaunch = true;
        private string configFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\" + AssemblyHelper.GetApplicationName() + @"\powerdocu.config.json";

        public ConfigHelper()
        {
        }

        public void LoadConfigurationFromFile()
        {
            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                var config = JsonConvert.DeserializeObject<ConfigHelper>(json);
                if (config != null)
                {
                    // Load existing properties
                    outputFormat = config.outputFormat;
                    documentChangesOnlyCanvasApps = config.documentChangesOnlyCanvasApps;
                    documentDefaultValuesCanvasApps = config.documentDefaultValuesCanvasApps;
                    flowActionSortOrder = config.flowActionSortOrder;
                    wordTemplate = config.wordTemplate;
                    documentSampleData = config.documentSampleData;
                    documentSolution = config.documentSolution;
                    documentAgents = config.documentAgents;
                    documentModelDrivenApps = config.documentModelDrivenApps;
                    documentBusinessProcessFlows = config.documentBusinessProcessFlows;
                    documentDesktopFlows = config.documentDesktopFlows;
                    documentClassicWorkflows = config.documentClassicWorkflows;
                    documentFlows = config.documentFlows;
                    documentApps = config.documentApps;
                    documentAppProperties = config.documentAppProperties;
                    documentAppVariables = config.documentAppVariables;
                    documentAppDataSources = config.documentAppDataSources;
                    documentAppResources = config.documentAppResources;
                    documentAppControls = config.documentAppControls;
                    documentDefaultColumns = config.documentDefaultColumns;
                    addTableOfContents = config.addTableOfContents;
                    showAllComponentsInGraph = config.showAllComponentsInGraph;
                    checkForUpdatesOnLaunch = config.checkForUpdatesOnLaunch;
                }
            }
        }

        public void SaveConfigurationToFile()
        {
            string json = JsonConvert.SerializeObject(this);
            File.WriteAllText(configFile, json);
        }
    }
}