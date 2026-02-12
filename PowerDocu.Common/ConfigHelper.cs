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
        public bool documentSolution = false;
        public bool documentFlows = false;
        public bool documentApps = false;
        public bool documentAppProperties = false;
        public bool documentAppVariables = false;
        public bool documentAppDataSources = false;
        public bool documentAppResources = false;
        public bool documentAppControls = false;
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

                    // Load newly added properties
                    documentSolution = config.documentSolution;
                    documentFlows = config.documentFlows;
                    documentApps = config.documentApps;
                    documentAppProperties = config.documentAppProperties;
                    documentAppVariables = config.documentAppVariables;
                    documentAppDataSources = config.documentAppDataSources;
                    documentAppResources = config.documentAppResources;
                    documentAppControls = config.documentAppControls;
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