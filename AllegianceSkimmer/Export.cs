using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AllegianceSkimmer
{
    public static class Export
    {
        public class ScanItemJsonConverter : JsonConverter<ScanItem>
        {
            public override ScanItem ReadJson(JsonReader reader, Type objectType, ScanItem existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                // Create JObject from reader
                JObject jo = JObject.Load(reader);

                // Create new ScanItem
                var scanItem = new ScanItem
                {
                    Name = jo["name"]?.ToString(),
                    Children = jo["children"]?.ToObject<List<ScanItem>>()
                };

                return scanItem;
            }

            public override void WriteJson(JsonWriter writer, ScanItem value, JsonSerializer serializer)
            {
                // Create new JObject with only the properties we want to serialize
                var jo = new JObject
                {
                    ["name"] = value.Name,
                    ["children"] = JArray.FromObject(value.Children ?? new List<ScanItem>())
                };

                if (value.Children != null && value.Children.Count > 0)
                {
                    // Create a new JArray for children
                    var childrenArray = new JArray();
                    foreach (var child in value.Children)
                    {
                        // Serialize each child using the same serializer to maintain consistency
                        using (var childWriter = new JTokenWriter())
                        {
                            serializer.Serialize(childWriter, child);
                            childrenArray.Add(childWriter.Token);
                        }
                    }
                    jo["children"] = childrenArray;
                }

                jo.WriteTo(writer);
            }
        }

        public static void DoExport(string filename)
        {
            Utilities.Message("inside DoExport...");
            Utilities.EnsurePathExists(Globals.PluginDirectory);
            string path = Path.Combine(Globals.PluginDirectory, filename);

            if (PluginCore.currentScan == null)
            {
                Utilities.Message("No current scan. Not exporting.");
                return;
            }

            if (PluginCore.currentScan.characters.Count == 0)
            {
                Utilities.Message("Warning: Current scan has no characters so this export will be empty.");
            }


            // Try to find the root item so we can export it
            var scan = PluginCore.currentScan;
            var root = scan.characters.Find(x => x.IsRoot );

            if (root == null) {
                Utilities.Message("Couldn't find a root. This is a bug and should be reported.");
                return;
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(path, false))
                {
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new ScanItemJsonConverter() }
                    };
                    string json = JsonConvert.SerializeObject(root, settings);

                    writer.Write(json);
                }
            }
            catch (Exception ex) { 
                Logging.Log(ex);
            }

            Utilities.Message($"Done exporting to {path}.");
        }
    }
}
