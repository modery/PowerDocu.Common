using System;
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
        public List<SolutionDependency> Dependencies = new List<SolutionDependency>();
        public Dictionary<string, string> LocalizedNames = new Dictionary<string, string>();
        public Dictionary<string, string> Descriptions = new Dictionary<string, string>();
        public CustomizationsEntity Customizations;

        public List<string> GetComponentTypes()
        {
            return Components.GroupBy(p => p.Type).Select(g => g.First()).OrderBy(t => t.Type).Select(t => t.Type).ToList();
        }

        public string GetDisplayNameForComponent(SolutionComponent component)
        {
            string name = component.Type switch
            {
                "Canvas App" => Customizations.getAppNameBySchemaName(component.SchemaName),
                "Workflow" => Customizations.getFlowNameById(component.ID),
                _ => String.IsNullOrEmpty(component.SchemaName) ? component.ID : component.SchemaName,
            };
            name ??= String.IsNullOrEmpty(component.SchemaName) ? component.ID : component.SchemaName;
            return name;
        }
    }

    public class SolutionPublisher
    {
        public string UniqueName;
        public string EMailAddress;
        public string SupportingWebsiteUrl;
        public string CustomizationPrefix;
        public string CustomizationOptionValuePrefix;
        public Dictionary<string, string> Descriptions = new Dictionary<string, string>();
        public Dictionary<string, string> LocalizedNames = new Dictionary<string, string>();
        public List<Address> Addresses = new List<Address>();
    }

    public class Address
    {
        public string AddressNumber;
        public string AddressTypeCode;
        public string City;
        public string County;
        public string Country;
        public string Fax;
        public string FreightTermsCode;
        public string ImportSequenceNumber;
        public string Latitude;
        public string Line1;
        public string Line2;
        public string Line3;
        public string Longitude;
        public string Name;
        public string PostalCode;
        public string PostOfficeBox;
        public string PrimaryContactName;
        public string ShippingMethodCode;
        public string StateOrProvince;
        public string Telephone1;
        public string Telephone2;
        public string Telephone3;
        public string TimeZoneRuleVersionNumber;
        public string UPSZone;
        public string UTCOffset;
        public string UTCConversionTimeZoneCode;
    }

    public class SolutionComponent
    {
        public string Type;
        public string SchemaName;
        public string ID;
        //the following properties are only used for items mentioned in MissingDependencies (Required/Dependent components)
        public string reqdepDisplayName;
        public string reqdepSolution;
        public string reqdepParentSchemaName;
        public string reqdepParentDisplayName;
        public string reqdepIdSchemaName;
    }

    public class SolutionDependency
    {
        public SolutionComponent Required;
        public SolutionComponent Dependent;

        public SolutionDependency(SolutionComponent req, SolutionComponent dep)
        {
            Required = req;
            Dependent = dep;
        }
    }

    public class RoleEntity
    {
        public string Name;
        public string ID;
        public List<TableAccess> Tables = new List<TableAccess>();
        public Dictionary<string,string> miscellaneousPrivileges = new Dictionary<string, string>();
    }

    public class TableAccess
    {
        public AccessLevel Create = AccessLevel.None;
        public AccessLevel Read = AccessLevel.None;
        public AccessLevel Write = AccessLevel.None;
        public AccessLevel Delete = AccessLevel.None;
        public AccessLevel Append = AccessLevel.None;
        public AccessLevel AppendTo = AccessLevel.None;
        public AccessLevel Assign = AccessLevel.None;
        public AccessLevel Share = AccessLevel.None;
        public string Name;
    }

    public enum AccessLevel
    {
        Global,
        Deep,
        Local,
        Basic,
        None
    }
}