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
            string name;
            switch (component.Type)
            {
                case "Canvas App":
                    name = Customizations.getAppNameBySchemaName(component.SchemaName); //content.apps.Find(a => a.Filename.Equals(component.SchemaName))?.Name;
                    break;
                case "Workflow":
                    name = Customizations.getFlowNameById(component.ID);
                    break;
                default:
                    name = String.IsNullOrEmpty(component.SchemaName) ? component.ID : component.SchemaName;
                    break;
            }
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
}