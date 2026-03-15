using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HtmlAgilityPack;

namespace PowerDocu.Common
{
    public enum ConnectionType
    {
        Connector,
        ConnectorReference
    };
    public static class ConnectorHelper
    {
        private static readonly string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\" + AssemblyHelper.GetApplicationName() + @"\ConnectorIcons\";
        private static readonly string defaultConnectorJsonFolderPath = AssemblyHelper.GetExecutablePath() + @"\Resources\ConnectorIcons\";
        private const string connectorList = "https://learn.microsoft.com/en-us/connectors/connector-reference/";
        private static List<ConnectorIcon> connectorIcons;

        public static string getConnectorIconFile(string connectorName)
        {
            if (File.Exists(folderPath + connectorName + ".png"))
            {
                return folderPath + connectorName + ".png";
            }
            return "";
        }
        public static int numberOfConnectorIcons()
        {
            return Directory.GetFiles(folderPath, "*.png").Length;
        }

        public static int numberOfConnectors()
        {
            loadConnectorIcons();
            return connectorIcons.Count;
        }

        public static ConnectorIcon getConnectorIcon(string uniqueName)
        {
            loadConnectorIcons();
            return connectorIcons.Find(x => x.Uniquename == uniqueName);
        }

        /// <summary>
        /// Resolves a raw connector identifier to a friendly display name using connectors.json.
        /// Handles flow connection references (e.g. "commondataserviceforapps"),
        /// shared_ prefixed names (e.g. "shared_office365"),
        /// and agent dot-separated references (e.g. "agentName.shared_xxx.connectionId").
        /// </summary>
        public static string ResolveConnectorDisplayName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return rawName;
            loadConnectorIcons();

            // 1) Direct lookup by unique name
            var icon = connectorIcons.Find(x => x.Uniquename.Equals(rawName, StringComparison.OrdinalIgnoreCase));
            if (icon != null) return icon.Name;

            // 2) Strip "shared_" prefix and try again
            string stripped = rawName.StartsWith("shared_", StringComparison.OrdinalIgnoreCase)
                ? rawName.Substring("shared_".Length)
                : null;
            if (stripped != null)
            {
                icon = connectorIcons.Find(x => x.Uniquename.Equals(stripped, StringComparison.OrdinalIgnoreCase));
                if (icon != null) return icon.Name;
            }

            // 3) Agent connection references: "prefix.shared_connectorname.connectionId"
            //    Extract segment containing "shared_"
            if (rawName.Contains("."))
            {
                var segments = rawName.Split('.');
                foreach (var seg in segments)
                {
                    if (seg.StartsWith("shared_", StringComparison.OrdinalIgnoreCase))
                    {
                        string connName = seg.Substring("shared_".Length);
                        // Remove trailing GUID-like suffixes separated by hyphens
                        // e.g. "service-now" stays, but we try the full name first
                        icon = connectorIcons.Find(x => x.Uniquename.Equals(connName, StringComparison.OrdinalIgnoreCase));
                        if (icon != null) return icon.Name;
                        // Try stripping the part after the last hyphen-separated GUID
                        int lastHyphen = connName.LastIndexOf('-');
                        if (lastHyphen > 0)
                        {
                            string shorter = connName.Substring(0, lastHyphen);
                            icon = connectorIcons.Find(x => x.Uniquename.Equals(shorter, StringComparison.OrdinalIgnoreCase));
                            if (icon != null) return icon.Name;
                        }
                        // Return cleaned name even if not found in connectors.json
                        return connName;
                    }
                }
            }

            // 4) Return the input as-is if nothing matched
            return stripped ?? rawName;
        }

        private static void loadConnectorIcons()
        {
            if (connectorIcons == null)
            {
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                if (!File.Exists(folderPath + "connectors.json")) File.Copy(defaultConnectorJsonFolderPath + "connectors.json", folderPath + "connectors.json");
                String JSONtxt = File.ReadAllText(folderPath + "connectors.json");
                connectorIcons = JsonConvert.DeserializeObject<List<ConnectorIcon>>(JSONtxt);
            }
        }

        public static async Task<bool> UpdateConnectorIcons()
        {
            try
            {
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                NotificationHelper.SendNotification("Updating Connectors list, please wait.");
                List<ConnectorIcon> connectorIcons = new List<ConnectorIcon>();
                var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (compatible; PowerDocu " + PowerDocuReleaseHelper.currentVersion.ToString() + ")"
                );

                HtmlWeb web = new HtmlWeb();
                //this isn't an ideal approach, as we rely on the second table containing all connectors
                var htmlDoc = web.Load(connectorList);
                var connectors = htmlDoc.DocumentNode.SelectNodes("//table")[1].SelectNodes(".//td");
                foreach (HtmlNode connector in connectors)
                {
                    ConnectorIcon connectorIcon = new ConnectorIcon
                    {
                        Url = connector.SelectSingleNode(".//img").GetAttributeValue("src", ""),
                        Uniquename = connector.SelectSingleNode(".//a").GetAttributeValue("href", "").Replace("../", "").Replace("/", "").Replace("connectorreference", "").Replace("en-usconnectors", ""),
                        Name = connector.SelectSingleNode(".//a/b").InnerText
                    };
                    connectorIcons.Add(connectorIcon);
                }

                // Download icons in parallel with limited concurrency
                const int maxConcurrency = 25;
                var semaphore = new SemaphoreSlim(maxConcurrency);
                var downloadTasks = connectorIcons.Select(async connectorIcon =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var response = await client.GetAsync(connectorIcon.Url);
                        response.EnsureSuccessStatusCode();
                        var bytes = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(folderPath + connectorIcon.Uniquename + ".png", bytes);
                    }
                    catch (Exception ex)
                    {
                        NotificationHelper.SendNotification($"Failed to download icon for {connectorIcon.Name}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                await Task.WhenAll(downloadTasks);

                File.WriteAllText(folderPath + "connectors.json", JsonConvert.SerializeObject(connectorIcons));
                NotificationHelper.SendNotification($"Update complete. A total of {connectorIcons.Count} connectors were found.");
            }
            catch (Exception e)
            {
                NotificationHelper.SendNotification("An error occured while trying to update the connector list");
                NotificationHelper.SendNotification(e.ToString());
            }
            finally
            {
                connectorIcons = null;
            }
            return true;
        }

    }

    public class ConnectorIcon
    {
        public string Name;
        public string Uniquename;
        public string Url;
    }
}