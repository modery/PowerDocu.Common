using System.Collections.Generic;

namespace PowerDocu.Common
{
    public static class Privileges
    {
        public static Dictionary<string, string> GetMiscellaneousPrivileges()
        {
            return new Dictionary<string, string>
            {
                { "prvPublishRSReport", "Add Reporting Services Reports" },
                { "prvBulkDelete", "Bulk Delete" },
                { "prvDeleteAuditPartitions", "Delete Audit Partitions" },
                { "prvDeleteRecordChangeHistory", "Delete Audit Record Change History" },
                { "prvRestoreSqlEncryptionKey", "Manage Data Encryption key - Activate" },
                { "prvChangeSqlEncryptionKey", "Manage Data Encryption key - Change" },
                { "prvReadSqlEncryptionKey", "Manage Data Encryption key - Read" },
                { "prvAdminFilter", "Manage User Synchronization Filters" },
                //miscPrivileges.Add("", "Promote User to Microsoft Dynamics 365 Administrator Role");
                { "prvPublishDuplicateRule", "Publish Duplicate Detection Rules" },
                { "prvCreateOrgEmailTemplates", "Publish Email Templates" },
                { "prvPublishOrgMailMergeTemplate", "Publish Mail Merge Templates to Organization" },
                { "prvPublishOrgReport", "Publish Reports" },
                { "prvConfigureSharePoint", "Run SharePoint Integration Wizard" },
                //{ "", "Turn On Tracing" },
                { "prvReadRecordAuditHistory", "View Audit History" },
                { "prvReadAuditPartitions", "View Audit Partitions" },
                { "prvReadAuditSummary", "View Audit Summary" },
                { "prvConfigureInternetMarketing", "Configure Internet Marketing module" },
                { "prvAllowQuickCampaign", "Create Quick Campaign" },
                { "prvUseInternetMarketing", "Use internet marketing module" },
                { "prvOverridePriceEngineInvoice", "Override Invoice Pricing" },
                { "prvOverridePriceEngineOpportunity", "Override Opportunity Pricing" },
                { "prvOverridePriceEngineOrder", "Override Order Pricing" },
                { "prvQOIOverrideDelete", "Override Quote Order Invoice Delete" },
                { "prvOverridePriceEngineQuote", "Override Quote Pricing" },
                { "prvApproveKnowledgeArticle", "Approve Knowledge Articles" },
                { "prvPublishArticle", "Publish Articles" },
                { "prvPublishKnowledgeArticle", "Publish Knowledge Articles" },
                { "prvActOnBehalfOfAnotherUser", "Act on Behalf of Another User" },
                { "prvApproveRejectEmailAddress", "Approve Email Addresses for Users or Queues" },
                { "prvAssignManager", "Assign manager for a user" },
                { "prvAssignPosition", "Assign position for a user" },
                { "prvAssignTerritory", "Assign Territory to User" },
                { "prvBulkEdit", "Bulk Edit" },
                { "prvWriteHierarchicalSecurityConfiguration", "Change Hierarchy Security Settings" },
                { "prvAddressBook", "Dynamics 365 Address Book" },
                { "prvDisableBusinessUnit", "Enable or Disable a Business Unit" },
                { "prvDisableUser", "Enable or Disable User" },
                { "prvLanguageSettings", "Language Settings" },
                { "prvMerge", "Merge" },
                { "prvOverrideCreatedOnCreatedBy", "Override Created on or Created by for Records during Data Import" },
                { "prvRollupGoal", "Perform in sync rollups on goals" },
                { "prvReadLicense", "Read License info" },
                { "prvReparentBusinessUnit", "Reparent Business unit" },
                { "prvReparentTeam", "Reparent team" },
                { "prvReparentUser", "Reparent user" },
                { "prvSendAsUser", "Send Email as Another User" },
                { "prvSendInviteForLive", "Send Invitation" },
                { "prvWebMailMerge", "Web Mail Merge" },
                { "prvBrowseAvailability", "Browse availability" },
                { "prvCreateOwnCalendar", "Create own calendar" },
                { "prvDeleteOwnCalendar", "Delete own calendar" },
                { "prvReadOwnCalendar", "Read own calendar" },
                { "prvSearchAvailability", "Search Availability" },
                { "prvWriteOwnCalendar", "Write own calendar" },
                { "prvActivateBusinessProcessFlow", "Activate Business Process Flows" },
                { "prvActivateBusinessRule", "Activate Business Rules" },
                { "prvActivateSynchronousWorkflow", "Activate Real-time Processes" },
                { "prvConfigureYammer", "Configure Yammer" },
                { "prvWorkflowExecution", "Execute Workflow Job" },
                { "prvExportCustomization", "Export Customizations" },
                { "prvImportCustomization", "Import Customizations" },
                { "prvISVExtensions", "ISV Extensions" },
                //{ "", "Learning Path Authoring" },
                { "prvPublishCustomization", "Publish Customizations" },
                { "prvRetrieveMultipleSocialInsights", "Retrieve Multiple Social Insights" }
                //{ "", "Run Flows" }
            };
        }
    }
}