using System;
using System.Collections.Generic;

namespace PowerDocu.Common
{
    /// <summary>
    /// Out-of-the-box Dataverse security role template IDs and their display names.
    /// Source: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/security-roles
    /// </summary>
    public static class SecurityRoles
    {
        //OOTB roles
        private static readonly Dictionary<string, string> RoleTemplates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "627090ff-40a3-4053-8790-584edc5be201", "System Administrator" },
            { "119f245c-3cc8-4b62-b31c-d1a046ced15d", "System Customizer" },
            { "2d101bb3-5ced-4122-83f1-94d5efde4e3b", "Support User" },
            { "d892cc0b-28c7-4e88-bd92-72f2c366baed", "Delegate" },
            { "236750cd-45ae-4939-ab12-b24b920ced93", "Basic User" },
            { "85937b6b-91a1-46ed-9778-929fc9f61812", "Business Manager" },
            { "29123793-6ae5-4955-9f1a-f10ceb9705f1", "VP of Sales" },
            { "c0ed2f4f-6f92-4691-92ba-78f2931e8fba", "Sales Manager" },
            { "a4be89ff-7c35-4d69-9900-999c3f603e6f", "Salesperson" },
            { "ecfd0b44-5720-45e3-ae68-417ddb0fb654", "Customer Service Representative" },
            { "1808b939-dd07-4ca7-aa99-ddd2734378f1", "CSR Manager" },
            { "09a25608-d28b-4d47-b57c-79271fe6a525", "Marketing Professional" },
            { "debec338-bca7-4882-ae04-84e6ddda2984", "Schedule Manager" },
            { "6caba073-59a8-4d6b-8e7b-4ccb50c5166b", "VP of Marketing" },
            { "d9d602db-2761-4170-877f-983494567c08", "Marketing Manager" },
            { "dcd60b89-421c-44ae-bff0-dd6323df885c", "Scheduler" },
            { "b4b40b17-cf37-4ea8-b2c5-b580f2f48654", "Knowledge Manager" },
        };

        /// <summary>
        /// Tries to resolve a role ID to a well-known display name.
        /// Accepts IDs with or without curly braces.
        /// </summary>
        public static string GetDisplayName(string roleId)
        {
            if (string.IsNullOrEmpty(roleId)) return null;
            string normalized = roleId.Trim('{', '}');
            return RoleTemplates.TryGetValue(normalized, out string name) ? name : null;
        }
    }
}
