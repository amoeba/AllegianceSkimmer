using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decal.Adapter;

namespace AllegianceSkimmer
{
    public static class Logging
    {
        internal static void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(System.IO.Path.Combine(Utilities.AssemblyDirectory, "log.txt"), $"{message}\n");

                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
    }
}
