using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Syntax;

namespace PowerDocu.Common
{
    public class AppParser
    {
        public enum PackageType
        {
            AppPackage,
            SolutionPackage
        }
        private readonly List<AppEntity> apps = new List<AppEntity>();
        private readonly AppEntity currentApp;
        public PackageType packageType;
        private Engine engine = new Engine();

        public AppParser(string filename)
        {
            NotificationHelper.SendNotification(" - Processing " + filename);
            if (filename.EndsWith(".zip"))
            {
                using FileStream stream = new FileStream(filename, FileMode.Open);
                List<ZipArchiveEntry> definitions = ZipHelper.getFilesInPathFromZip(stream, "", ".msapp");
                packageType = PackageType.SolutionPackage;
                foreach (ZipArchiveEntry definition in definitions)
                {
                    string tempFile = Path.GetDirectoryName(filename) + @"\" + definition.Name;
                    definition.ExtractToFile(tempFile, true);
                    NotificationHelper.SendNotification("  - Processing app " + definition.FullName);
                    using (FileStream appDefinition = new FileStream(tempFile, FileMode.Open))
                    {
                        {
                            AppEntity app = new AppEntity();
                            currentApp = app;
                            parseAppProperties(appDefinition);
                            parseAppControls(appDefinition);
                            parseAppDataSources(appDefinition);
                            parseAppResources(appDefinition);
                            apps.Add(app);
                        }
                    }
                    File.Delete(tempFile);
                }
            }
            else if (filename.EndsWith(".msapp"))
            {
                NotificationHelper.SendNotification("  - Processing app " + filename);
                packageType = PackageType.AppPackage;
                AppEntity app = new AppEntity();
                currentApp = app;
                using FileStream stream = new FileStream(filename, FileMode.Open);
                parseAppProperties(stream);
                parseAppControls(stream);
                parseAppDataSources(stream);
                parseAppResources(stream);
                apps.Add(app);
            }
            else
            {
                NotificationHelper.SendNotification("Invalid file " + filename);
            }
        }

        private void parseAppProperties(Stream appArchive)
        {
            string[] filesToParse = new string[] { "Resources\\PublishInfo.json", "Header.json", "Properties.json" };
            foreach (string fileToParse in filesToParse)
            {
                using StreamReader reader = new StreamReader(ZipHelper.getFileFromZip(appArchive, fileToParse).Open());
                string appJSON = reader.ReadToEnd();
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 128 };
                var _jsonSerializer = JsonSerializer.Create(settings);
                dynamic propertiesDefinition = JsonConvert.DeserializeObject<JObject>(appJSON, settings).ToObject(typeof(object), _jsonSerializer);
                foreach (JToken property in propertiesDefinition.Children())
                {
                    JProperty prop = (JProperty)property;
                    currentApp.Properties.Add(Expression.parseExpressions(prop));
                    if (prop.Name.Equals("AppName"))
                    {
                        currentApp.Name = prop.Value.ToString();
                    }
                    if (prop.Name.Equals("ID"))
                    {
                        currentApp.ID = prop.Value.ToString();
                    }
                    if (prop.Name.Equals("LogoFileName") && !String.IsNullOrEmpty(prop.Value.ToString()))
                    {
                        ZipArchiveEntry resourceFile = ZipHelper.getFileFromZip(appArchive, "Resources\\" + prop.Value.ToString());
                        MemoryStream ms = new MemoryStream();
                        resourceFile.Open().CopyTo(ms);
                        currentApp.ResourceStreams.Add(prop.Value.ToString(), ms);
                    }
                }
            }
        }

        private void parseAppControls(Stream appArchive)
        {
            List<ZipArchiveEntry> controlFiles = ZipHelper.getFilesInPathFromZip(appArchive, "Controls", ".json");
            //parse the controls. each controlFile represents a screen
            foreach (ZipArchiveEntry controlEntry in controlFiles)
            {
                using StreamReader reader = new StreamReader(controlEntry.Open());
                string appJSON = reader.ReadToEnd();
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 128 };
                var _jsonSerializer = JsonSerializer.Create(settings);
                dynamic controlsDefinition = JsonConvert.DeserializeObject<JObject>(appJSON, settings).ToObject(typeof(object), _jsonSerializer);
                currentApp.Controls.Add(parseControl(((JObject)controlsDefinition.TopParent).Children().ToList()));
            }
            foreach (ControlEntity control in currentApp.Controls)
            {
                CheckVariableUsage(control);
            }
        }

        private ControlEntity parseControl(List<JToken> properties)
        {
            ControlEntity controlEntity = new ControlEntity();
            foreach (JToken property in properties)
            {
                if (property.GetType().Equals(typeof(JProperty)))
                {
                    JProperty prop = (JProperty)property;
                    if (prop.Name.Equals("Children"))
                    {
                        JEnumerable<JToken> children = ((JArray)prop.Value).Children();
                        foreach (JToken child in children)
                        {
                            ControlEntity childControLEntity = parseControl(child.Children().ToList());
                            controlEntity.Children.Add(childControLEntity);
                            childControLEntity.Parent = controlEntity;
                        }
                    }
                    else if (prop.Name.Equals("Rules"))
                    {
                        JEnumerable<JToken> children = ((JArray)prop.Value).Children();
                        foreach (JObject child in children)
                        {
                            Rule rule = new Rule();
                            foreach (JProperty ruleProp in child.Children())
                            {
                                switch (ruleProp.Name)
                                {
                                    case "Property":
                                        rule.Property = ruleProp.Value.ToString();
                                        break;
                                    case "Category":
                                        rule.Category = ruleProp.Value.ToString();
                                        break;
                                    case "RuleProviderType":
                                        rule.RuleProviderType = ruleProp.Value.ToString();
                                        break;
                                    case "InvariantScript":
                                        rule.InvariantScript = ruleProp.Value.ToString();
                                        CheckForVariables(controlEntity, ruleProp.Value.ToString());
                                        break;
                                }
                            }
                            controlEntity.Rules.Add(rule);
                        }
                    }
                    else
                    {
                        controlEntity.Properties.Add(Expression.parseExpressions(prop));
                        if (prop.Name.Equals("Name"))
                        {
                            controlEntity.Name = prop.Value.ToString();
                        }
                    }
                }
                else
                {
                    ControlEntity child = parseControl(((JObject)property).Children().ToList());
                    controlEntity.Children.Add(child);
                    child.Parent = controlEntity;
                }
            }
            controlEntity.Type = controlEntity.Properties.Where(e => e.expressionOperator == "Template")?.First().expressionOperands.Cast<Expression>().First(eo => eo.expressionOperator == "Name").expressionOperands[0].ToString();
            //for containers, there are a few different types (Contaner, Horizontal Container, Vertical Container) which are defined by the "VariantName" property
            if (controlEntity.Type.Equals("groupContainer"))
            {
                controlEntity.Type = controlEntity.Properties.First(o => o.expressionOperator.Equals("VariantName")).expressionOperands[0].ToString();
            }
            //components can safely be identified through the Id of the template
            string controlId = controlEntity.Properties.Where(e => e.expressionOperator == "Template")?.First().expressionOperands.Cast<Expression>().First(eo => eo.expressionOperator == "Id").expressionOperands[0].ToString();
            if (controlId.Equals("http://microsoft.com/appmagic/Component"))
            {
                controlEntity.Type = "component";
            }
            return controlEntity;
        }

        private void CheckVariableUsage(ControlEntity control)
        {
            foreach (Rule rule in control.Rules)
            {
                var identifiers = GetIdentifiers(rule.InvariantScript);
                foreach (var ident in identifiers)
                {
                    if (currentApp.GlobalVariables.Contains(ident)
                        || currentApp.ContextVariables.Contains(ident)
                        || currentApp.Collections.Contains(ident))
                    {
                        addVariableControlMapping(ident, control, rule.Property);
                    }
                }
            }
            foreach (ControlEntity child in control.Children)
            {
                CheckVariableUsage(child);
            }
            // TODO also check  Properties ?
        }

        private HashSet<string> GetIdentifiers(string script)
        {
            var tokens = engine.Tokenize(script);
            return new HashSet<string>(
                tokens.Where(t => t.Kind == TokKind.Ident).Select(t => t.ToString()));
        }

        private void addVariableControlMapping(string globalVar, ControlEntity control, string property)
        {
            if (currentApp.VariableCollectionControlReferences.ContainsKey(globalVar))
            {
                currentApp.VariableCollectionControlReferences[globalVar].Add(new ControlPropertyReference() { Control = control, RuleProperty = property });
            }
            else
            {
                List<ControlPropertyReference> list = new List<ControlPropertyReference>
                {
                    new ControlPropertyReference() { Control = control, RuleProperty = property }
                };
                currentApp.VariableCollectionControlReferences.Add(globalVar, list);
            }
        }

        private void addScreenNavigation(ControlEntity controlEntity, string destinationScreen)
        {
            if (currentApp.ScreenNavigations.ContainsKey(controlEntity))
            {
                currentApp.ScreenNavigations[controlEntity].Add(destinationScreen);
            }
            else
            {
                List<string> list = new List<string>
                {
                    destinationScreen
                };
                currentApp.ScreenNavigations.Add(controlEntity, list);
            }
        }

        private void CheckForVariables(ControlEntity controlEntity, string input)
        {
            // Use PowerFx parser to build an AST, which correctly handles comments and nesting
            var parseResult = engine.Parse(input);
            var visitor = new FormulaVisitor();
            parseResult.Root.Accept(visitor);

            foreach (var v in visitor.GlobalVariables)
                currentApp.GlobalVariables.Add(v);
            foreach (var v in visitor.ContextVariables)
                currentApp.ContextVariables.Add(v);
            foreach (var v in visitor.Collections)
                currentApp.Collections.Add(v);
            foreach (var target in visitor.NavigationTargets)
                addScreenNavigation(controlEntity, target);
        }

        /// <summary>
        /// Walks the PowerFx AST to extract variable definitions, collection usages,
        /// and screen navigation targets from function calls.
        /// </summary>
        private class FormulaVisitor : IdentityTexlVisitor
        {
            public readonly List<string> GlobalVariables = new List<string>();
            public readonly List<string> ContextVariables = new List<string>();
            public readonly List<string> Collections = new List<string>();
            public readonly List<string> NavigationTargets = new List<string>();

            public override void PostVisit(CallNode node)
            {
                var args = node.Args.ChildNodes;
                switch (node.Head.Name.Value)
                {
                    case "Set":
                        if (args.Count > 0 && args[0] is FirstNameNode setVar)
                            GlobalVariables.Add(setVar.Ident.Name.Value);
                        break;
                    case "UpdateContext":
                        if (args.Count > 0 && args[0] is RecordNode updateRecord)
                            foreach (var id in updateRecord.Ids)
                                ContextVariables.Add(id.Name.Value);
                        break;
                    case "Navigate":
                        if (args.Count > 0)
                            NavigationTargets.Add(args[0] is FirstNameNode screenNode
                                ? screenNode.Ident.Name.Value
                                : args[0].ToString());
                        // Third argument may contain context variables
                        if (args.Count >= 3 && args[2] is RecordNode navRecord)
                            foreach (var id in navRecord.Ids)
                                ContextVariables.Add(id.Name.Value);
                        break;
                    case "Collect":
                    case "ClearCollect":
                        if (args.Count > 0 && args[0] is FirstNameNode collectionVar)
                            Collections.Add(collectionVar.Ident.Name.Value);
                        break;
                }
            }
        }

        private void parseAppDataSources(Stream appArchive)
        {
            ZipArchiveEntry dataSourceFile = ZipHelper.getFileFromZip(appArchive, "References\\DataSources.json");
            using StreamReader reader = new StreamReader(dataSourceFile.Open());
            string appJSON = reader.ReadToEnd();
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 128 };
            var _jsonSerializer = JsonSerializer.Create(settings);
            dynamic datasourceDefinition = JsonConvert.DeserializeObject<JObject>(appJSON, settings).ToObject(typeof(object), _jsonSerializer);
            foreach (JToken datasource in datasourceDefinition.DataSources.Children())
            {
                DataSource ds = new DataSource();
                foreach (JProperty prop in datasource.Children())
                {
                    switch (prop.Name)
                    {
                        case "Name":
                            ds.Name = prop.Value.ToString();
                            break;
                        case "Type":
                            ds.Type = prop.Value.ToString();
                            break;
                        default:
                            ds.Properties.Add(Expression.parseExpressions(prop));
                            break;
                    }
                }
                currentApp.DataSources.Add(ds);
            }
        }

        private void parseAppResources(Stream appArchive)
        {
            string[] ResourceExtensions = new string[] { "jpg", "jpeg", "gif", "png", "bmp", "tif", "tiff", "svg" };
            ZipArchiveEntry dataSourceFile = ZipHelper.getFileFromZip(appArchive, "References\\Resources.json");
            using StreamReader reader = new StreamReader(dataSourceFile.Open());
            string appJSON = reader.ReadToEnd();
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None, MaxDepth = 128 };
            var _jsonSerializer = JsonSerializer.Create(settings);
            dynamic resourceDefinition = JsonConvert.DeserializeObject<JObject>(appJSON, settings).ToObject(typeof(object), _jsonSerializer);
            foreach (JToken resource in resourceDefinition.Resources.Children())
            {
                Resource res = new Resource();
                string pathToResource = "";
                foreach (JProperty prop in resource.Children())
                {
                    switch (prop.Name)
                    {
                        case "Name":
                            res.Name = prop.Value.ToString();
                            break;
                        case "Content":
                            res.Content = prop.Value.ToString();
                            break;
                        case "ResourceKind":
                            res.ResourceKind = prop.Value.ToString();
                            break;
                        case "Path":
                            pathToResource = prop.Value.ToString();
                            break;
                        default:
                            res.Properties.Add(Expression.parseExpressions(prop));
                            break;
                    }
                }
                currentApp.Resources.Add(res);
                if (res.ResourceKind == "LocalFile")
                {
                    string extension = pathToResource[(pathToResource.LastIndexOf('.') + 1)..].ToLower();
                    if (ResourceExtensions.Contains(extension))
                    {
                        ZipArchiveEntry resourceFile = ZipHelper.getFileFromZip(appArchive, pathToResource);
                        MemoryStream ms = new MemoryStream();
                        resourceFile.Open().CopyTo(ms);
                        currentApp.ResourceStreams.Add(res.Name, ms);
                    }
                }
            }
        }

        public List<AppEntity> getApps()
        {
            return apps;
        }

    }
}