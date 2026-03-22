namespace PowerDocu.Common
{
    /// <summary>
    /// Provides standardised relative paths between generated documentation folders.
    /// Each method returns a path relative to the output root (e.g. "FlowDoc SafeName/index-safename.html").
    /// Callers in subfolders must prepend "../" themselves.
    /// </summary>
    public static class CrossDocLinkHelper
    {
        // ── HTML paths ─────────────────────────────────────────────

        public static string GetFlowDocHtmlPath(string flowName)
        {
            string safeName = CharsetHelper.GetSafeName(flowName);
            string folder = "FlowDoc " + safeName;
            string file = ("index-" + safeName + ".html").Replace(" ", "-");
            return folder + "/" + file;
        }

        public static string GetAppDocHtmlPath(string appName)
        {
            string safeName = CharsetHelper.GetSafeName(appName);
            string folder = "AppDoc " + safeName;
            string file = ("index-" + safeName + ".html").Replace(" ", "-");
            return folder + "/" + file;
        }

        public static string GetAgentDocHtmlPath(string agentName)
        {
            string safeName = CharsetHelper.GetSafeName(agentName);
            string folder = "AgentDoc " + safeName;
            string file = ("index-" + safeName + ".html").Replace(" ", "-");
            return folder + "/" + file;
        }

        public static string GetMDADocHtmlPath(string mdaDisplayName)
        {
            string safeName = CharsetHelper.GetSafeName(mdaDisplayName);
            string folder = "MDADoc " + safeName;
            string file = ("mda-" + safeName + ".html").Replace(" ", "-");
            return folder + "/" + file;
        }

        public static string GetAIModelDocHtmlPath(string aiModelName)
        {
            string safeName = CharsetHelper.GetSafeName(aiModelName);
            string folder = "AIModelDoc " + safeName;
            string file = ("aimodel-" + safeName + ".html").Replace(" ", "-");
            return folder + "/" + file;
        }

        public static string GetWebResourceDocHtmlPath(string solutionUniqueName)
        {
            string safeName = CharsetHelper.GetSafeName(solutionUniqueName);
            return ("webresources-" + safeName + ".html").Replace(" ", "-");
        }

        public static string GetWebResourceDetailHtmlPath(string solutionUniqueName, string webResourceName)
        {
            string safeSolution = CharsetHelper.GetSafeName(solutionUniqueName);
            string safeWr = CharsetHelper.GetSafeName(webResourceName);
            return "WebResources/" + ("wr-" + safeWr + ".html").Replace(" ", "-");
        }

        public static string GetSolutionDocHtmlPath(string solutionUniqueName)
        {
            string safeName = CharsetHelper.GetSafeName(solutionUniqueName);
            return ("solution-" + safeName + ".html").Replace(" ", "-");
        }

        // ── Word paths ─────────────────────────────────────────────

        public static string GetFlowDocWordPath(string flowName)
        {
            string safeName = CharsetHelper.GetSafeName(flowName);
            string folder = "FlowDoc " + safeName;
            return folder + "/" + safeName + ".docx";
        }

        public static string GetAppDocWordPath(string appName)
        {
            string safeName = CharsetHelper.GetSafeName(appName);
            string folder = "AppDoc " + safeName;
            return folder + "/" + safeName + ".docx";
        }

        public static string GetAgentDocWordPath(string agentName)
        {
            string safeName = CharsetHelper.GetSafeName(agentName);
            string folder = "AgentDoc " + safeName;
            return folder + "/" + safeName + ".docx";
        }

        public static string GetMDADocWordPath(string mdaDisplayName)
        {
            string safeName = CharsetHelper.GetSafeName(mdaDisplayName);
            string folder = "MDADoc " + safeName;
            return folder + "/" + safeName + ".docx";
        }

        public static string GetAIModelDocWordPath(string aiModelName)
        {
            string safeName = CharsetHelper.GetSafeName(aiModelName);
            string folder = "AIModelDoc " + safeName;
            return folder + "/" + safeName + ".docx";
        }

        public static string GetWebResourceDocWordPath(string solutionUniqueName)
        {
            string safeName = CharsetHelper.GetSafeName(solutionUniqueName);
            return "WebResources - " + safeName + ".docx";
        }

        public static string GetSolutionDocWordPath(string solutionUniqueName)
        {
            string safeName = CharsetHelper.GetSafeName(solutionUniqueName);
            return "Solution - " + safeName + ".docx";
        }

        // ── Markdown paths ─────────────────────────────────────────
        // Markdown [text](url) syntax breaks on unencoded spaces, so all
        // spaces in the path portion are percent-encoded.

        public static string GetFlowDocMdPath(string flowName)
        {
            string safeName = CharsetHelper.GetSafeName(flowName);
            string folder = "FlowDoc " + safeName;
            string file = ("index-" + safeName + ".md").Replace(" ", "-");
            return (folder + "/" + file).Replace(" ", "%20");
        }

        public static string GetAppDocMdPath(string appName)
        {
            string safeName = CharsetHelper.GetSafeName(appName);
            string folder = "AppDoc " + safeName;
            string file = ("index-" + safeName + ".md").Replace(" ", "-");
            return (folder + "/" + file).Replace(" ", "%20");
        }

        public static string GetAgentDocMdPath(string agentName)
        {
            string safeName = CharsetHelper.GetSafeName(agentName);
            string folder = "AgentDoc " + safeName;
            string file = ("index-" + safeName + ".md").Replace(" ", "-");
            return (folder + "/" + file).Replace(" ", "%20");
        }

        public static string GetMDADocMdPath(string mdaDisplayName)
        {
            string safeName = CharsetHelper.GetSafeName(mdaDisplayName);
            string folder = "MDADoc " + safeName;
            string file = ("mda-" + safeName + ".md").Replace(" ", "-");
            return (folder + "/" + file).Replace(" ", "%20");
        }

        public static string GetAIModelDocMdPath(string aiModelName)
        {
            string safeName = CharsetHelper.GetSafeName(aiModelName);
            string folder = "AIModelDoc " + safeName;
            string file = ("aimodel-" + safeName + ".md").Replace(" ", "-");
            return (folder + "/" + file).Replace(" ", "%20");
        }

        public static string GetWebResourceDocMdPath(string solutionUniqueName)
        {
            string safeName = CharsetHelper.GetSafeName(solutionUniqueName);
            return ("webresources-" + safeName + ".md").Replace(" ", "-");
        }

        public static string GetWebResourceDetailMdPath(string solutionUniqueName, string webResourceName)
        {
            string safeWr = CharsetHelper.GetSafeName(webResourceName);
            return ("WebResources/" + "wr-" + safeWr + ".md").Replace(" ", "-");
        }

        public static string GetSolutionDocMdPath(string solutionUniqueName)
        {
            string safeName = CharsetHelper.GetSafeName(solutionUniqueName);
            return ("solution-" + safeName + ".md").Replace(" ", "-");
        }

        // ── HTML anchor IDs (matching SolutionHtmlBuilder conventions) ──

        public static string SanitizeAnchorId(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return System.Text.RegularExpressions.Regex.Replace(
                name.ToLowerInvariant().Replace(" ", "-"), "[^a-z0-9_-]", "");
        }

        public static string GetSolutionTableHtmlAnchor(string tableSchemaName)
        {
            return "#" + SanitizeAnchorId("table-" + tableSchemaName);
        }

        public static string GetSolutionRoleHtmlAnchor(string roleName)
        {
            return "#" + SanitizeAnchorId("role-" + roleName);
        }

        // ── Markdown anchor IDs (derived from heading text, GitHub-style) ──

        /// <summary>
        /// Markdown table heading: "{localizedName} ({schemaName})" → anchor
        /// </summary>
        public static string GetSolutionTableMdAnchor(string localizedName, string schemaName)
        {
            return "#" + SanitizeAnchorId(localizedName + " (" + schemaName + ")");
        }

        /// <summary>
        /// Markdown role heading: "{roleName} ({roleId})" → anchor
        /// </summary>
        public static string GetSolutionRoleMdAnchor(string roleName, string roleId)
        {
            return "#" + SanitizeAnchorId(roleName + " (" + roleId + ")");
        }
    }
}
