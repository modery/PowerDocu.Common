using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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
                    var response = await client.GetAsync(connectorIcon.Url);
                    File.WriteAllBytesAsync(folderPath + connectorIcon.Uniquename + ".png", await response.Content.ReadAsByteArrayAsync());
                }

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