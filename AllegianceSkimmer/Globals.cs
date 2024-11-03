using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decal.Adapter;

namespace AllegianceSkimmer
{
    public static class Globals
    {
        public static string PluginName { get; set; }
        public static string PluginDirectory { get; set; }

        public static void Init(string name)
        {
            PluginName = name;
            SetPluginDirectory();
        }

        public static void Destroy()
        {

        }

        public static void SetPluginDirectory()
        {
            try
            {
                PluginDirectory = string.Format(@"{0}\{1}\{2}",
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Decal Plugins",
                    PluginName);
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }
    }
}
