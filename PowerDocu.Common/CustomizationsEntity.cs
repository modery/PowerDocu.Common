using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace PowerDocu.Common
{
    //this class is complementary to the SolutionEntity class. At some point in the future, the information from the customizations.xml should move into SolutionEntity
    public class CustomizationsEntity
    {
        public XmlNode customizationsXml;

        public string getAppNameBySchemaName(string schemaName)
        {
            return customizationsXml.SelectSingleNode("/ImportExportXml/CanvasApps/CanvasApp[Name='" + schemaName + "']/DisplayName")?.InnerText;
        }

        public string getFlowNameById(string ID)
        {
            return customizationsXml.SelectSingleNode("/ImportExportXml/Workflows/Workflow[@WorkflowId='" + ID + "']")?.Attributes.GetNamedItem("Name").InnerText;
        }

        public List<RoleEntity> getRoles()
        {
            List<RoleEntity> roles = new List<RoleEntity>();
            foreach (XmlNode role in customizationsXml.SelectNodes("/ImportExportXml/Roles/Role"))
            {
                RoleEntity roleEntity = new RoleEntity
                {
                    Name = role.Attributes.GetNamedItem("name")?.InnerText,
                    ID = role.Attributes.GetNamedItem("id")?.InnerText
                };
                foreach (XmlNode privilege in role.SelectNodes("RolePrivileges/RolePrivilege"))
                {
                    //<RolePrivilege name="prvAppendadmin_App" level="Local" />
                    //<RolePrivilege name="prvAppendadmin_PVA" level="Local" />
                    //<RolePrivilege name="prvAppendNote" level="Global" />
                    //<RolePrivilege name="prvAppendToadmin_App" level="Local" />
                    //<RolePrivilege name="prvAppendToadmin_PVA" level="Local" />
                    string privilegeName = privilege.Attributes.GetNamedItem("name").InnerText;
                    if (Privileges.GetMiscellaneousPrivileges().ContainsKey(privilegeName))
                    {
                        roleEntity.miscellaneousPrivileges.Add(privilegeName, privilege.Attributes.GetNamedItem("level").InnerText);
                    }
                    else
                    {
                        privilegeName = privilegeName[3..];
                        string priv = "";
                        if (privilegeName.StartsWith("Create"))
                        {
                            priv = "Create";
                        }
                        else if (privilegeName.StartsWith("Read"))
                        {
                            priv = "Read";
                        }
                        else if (privilegeName.StartsWith("Write"))
                        {
                            priv = "Write";
                        }
                        else if (privilegeName.StartsWith("Delete"))
                        {
                            priv = "Delete";
                        }
                        else if (privilegeName.StartsWith("AppendTo"))
                        {
                            priv = "AppendTo";
                        }
                        else if (privilegeName.StartsWith("Append"))
                        {
                            priv = "Append";
                        }
                        else if (privilegeName.StartsWith("Assign"))
                        {
                            priv = "Assign";
                        }
                        else if (privilegeName.StartsWith("Share"))
                        {
                            priv = "Share";
                        }
                        parseAccessLevel(roleEntity, privilegeName.Replace(priv, ""), priv, privilege.Attributes.GetNamedItem("level").InnerText);
                    }
                }
                roles.Add(roleEntity);
            }
            return roles;
        }

        private void parseAccessLevel(RoleEntity roleEntity, string tableName, string privilege, string accessLevel)
        {
            TableAccess tableAccess = roleEntity.Tables.Find(o => o.Name == tableName);
            if (tableAccess == null)
            {
                tableAccess = new TableAccess
                {
                    Name = tableName
                };
                roleEntity.Tables.Add(tableAccess);
            }
            AccessLevel level = accessLevel switch
            {
                "Basic" => AccessLevel.Basic,
                "Deep" => AccessLevel.Deep,
                "Global" => AccessLevel.Global,
                "Local" => AccessLevel.Local,
                _ => AccessLevel.None
            };
            switch (privilege)
            {
                case "Create":
                    tableAccess.Create = level;
                    break;
                case "Read":
                    tableAccess.Read = level;
                    break;
                case "Write":
                    tableAccess.Write = level;
                    break;
                case "Delete":
                    tableAccess.Delete = level;
                    break;
                case "Append":
                    tableAccess.Append = level;
                    break;
                case "AppendTo":
                    tableAccess.AppendTo = level;
                    break;
                case "Assign":
                    tableAccess.Assign = level;
                    break;
                case "Share":
                    tableAccess.Share = level;
                    break;
            }
        }
    }
}