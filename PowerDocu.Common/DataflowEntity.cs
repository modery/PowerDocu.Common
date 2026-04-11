using System.Collections.Generic;

namespace PowerDocu.Common
{
    public class DataflowEntity
    {
        // From XML attributes/elements
        public string DataflowId;
        public string Name;
        public string OriginalDataflowId;
        public string InternalVersion;
        public bool IsCustomizable;
        public int StateCode;
        public int StatusCode;
        public string MashupDocument; // raw Power Query M code

        // From msdyn_mashupsettings top-level JSON
        public string DocumentLocale;
        public bool FastCombine;
        public bool AllowNativeQueries;
        public bool SkipAutomaticTypeAndHeaderDetection;
        public bool DisableAutoAnonymousConnectionUpsert;
        public string HostContextType;
        public string HostEnvironmentId;
        public List<DataflowQuery> Queries = new List<DataflowQuery>();

        // From double-encoded DataflowMetadata JSON
        public string DataflowType;
        public string OwnerName;
        public string CreatedTime;
        public string LastUpdateTime;
        public string PublishStatus;
        public List<DataflowConnectionOverride> ConnectionOverrides = new List<DataflowConnectionOverride>();

        // From double-encoded Dataflow JSON
        public string OutputFileFormat;

        // From msdyn_refreshsettings JSON
        public DataflowRefreshSettings RefreshSettings;

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;
            return DataflowId;
        }

        public string GetStateLabel()
        {
            return StateCode switch
            {
                0 => "Active",
                1 => "Inactive",
                _ => "Unknown"
            };
        }

        public string GetStatusLabel()
        {
            return StatusCode switch
            {
                1 => "Active",
                2 => "Inactive",
                _ => "Unknown"
            };
        }
    }

    public class DataflowQuery
    {
        public string QueryId;
        public string QueryName;
        public string EntityName;
        public bool LoadEnabled;
        public bool DeleteExistingDataOnLoad;
        public bool IsCalculatedEntity;
        public bool IsLinkedEntity;
        public string ResultTypeName;
        public List<DataflowFieldMapping> FieldMappings = new List<DataflowFieldMapping>();
    }

    public class DataflowFieldMapping
    {
        public string DestinationFieldName;
        public string SourceColumnName;
        public string DestinationFieldType;
    }

    public class DataflowConnectionOverride
    {
        public string Path;
        public string Kind;
        public string Provider;
        public string EnvironmentName;
    }

    public class DataflowRefreshSettings
    {
        public string RefreshPeriod;
        public string ScheduleRefreshType;
        public string StartDateTime;
        public string TimeBasedRefreshPeriod;
        public string TimeZoneId;
    }
}
