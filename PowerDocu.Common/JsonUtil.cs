using System;
using System.IO;
using Newtonsoft.Json;

namespace PowerDocu.Common
{
    static class JsonUtil
    {
        public static string JsonPrettify(string json)
        {
            using StringReader stringReader = new StringReader(json);
            using var stringWriter = new StringWriter();
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            return stringWriter.ToString();
        }
    }
}