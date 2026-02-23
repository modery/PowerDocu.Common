using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace PowerDocu.Common
{
    public static class JsonUtil
    {
        public static string JsonPrettify(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            if (!IsValidJson(json))
            {
                return json;
            }

            using StringReader stringReader = new StringReader(json);
            using var stringWriter = new StringWriter();
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            return stringWriter.ToString();
        }

        // Simple method to check if a string is valid JSON, without throwing exceptions in debugger
        [DebuggerHidden]
        private static bool IsValidJson(string json)
        {
            try
            {
                using var reader = new JsonTextReader(new StringReader(json));
                while (reader.Read()) { }
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }
    }
}