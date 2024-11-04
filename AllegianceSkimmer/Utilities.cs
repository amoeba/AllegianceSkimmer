using System;
using System.IO;

using Decal.Adapter;

namespace AllegianceSkimmer
{
    public static class Utilities
    {
        private static string _assemblyDirectory = null;

        public static string AssemblyDirectory
        {
            get
            {
                if (_assemblyDirectory == null)
                {
                    try
                    {
                        _assemblyDirectory = System.IO.Path.GetDirectoryName(typeof(PluginCore).Assembly.Location);
                    }
                    catch
                    {
                        _assemblyDirectory = Environment.CurrentDirectory;
                    }
                }
                return _assemblyDirectory;
            }
            set
            {
                _assemblyDirectory = value;
            }
        }

        public static void Message(string message)
        {
            CoreManager.Current.Actions.AddChatText($"[{Globals.PluginName}] {message}", 1);
        }

        public static void EnsurePathExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }
    }
}
