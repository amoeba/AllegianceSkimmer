using System;
using System.IO;

namespace AllegianceSkimmer
{
    public static class Export
    {
        private static string JsonString(string s)
        {
            if (s == null) return "null";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";
        }

        public static string Serialize(ScanItem item)
        {
            using (var sw = new StringWriter())
            {
                WriteItem(sw, item);
                return sw.ToString();
            }
        }

        private static void WriteItem(TextWriter writer, ScanItem item)
        {
            writer.Write("{\"name\":");
            writer.Write(JsonString(item.Name));
            writer.Write(",\"is_online\":");
            writer.Write(item.IsOnline ? "true" : "false");
            writer.Write(",\"children\":[");
            if (item.Children != null)
            {
                for (int i = 0; i < item.Children.Count; i++)
                {
                    if (i > 0) writer.Write(",");
                    WriteItem(writer, item.Children[i]);
                }
            }
            writer.Write("]}");
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
                    WriteItem(writer, root);
                }
            }
            catch (Exception ex) {
                Logging.Log(ex);
            }

            Utilities.Message($"Done exporting to {path}.");
        }
    }
}
