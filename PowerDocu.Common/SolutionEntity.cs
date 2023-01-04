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
        public string Descriptions;
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
        public string DisplayName;
        public string Solution;
        public string ParentSchemaName;
        public string ParentDisplayName;
        public string IdSchemaName;
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