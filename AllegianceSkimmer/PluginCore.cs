using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AcClient;
using Decal.Adapter;
using UtilityBelt.Common.Messages.Events;
using UtilityBelt.Common.Messages.Types;
using UtilityBelt.Scripting.Interop;
using UtilityBelt.Service.Lib;

namespace AllegianceSkimmer
{
    /// <summary>
    /// This is the main plugin class. When your plugin is loaded, Startup() is called, and when it's unloaded Shutdown() is called.
    /// </summary>
    [FriendlyName("AllegianceSkimmer")]
    public class PluginCore : PluginBase
    {
        private static string _assemblyDirectory = null;
        private ExampleUI ui;
        private static Game _game;
        public static Scan currentScan;
        public static System.Windows.Forms.Timer globalTimer;
        public static Queue globalQueue;

        /// <summary>
        /// Assembly directory containing the plugin dll
        /// </summary>
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

        /// <summary>
        /// Called when your plugin is first loaded.
        /// </summary>
        protected override void Startup()
        {
            try
            {
                // subscribe to CharacterFilter_LoginComplete event, make sure to unscribe later.
                // note: if the plugin was reloaded while ingame, this event will never trigger on the newly reloaded instance.
                CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;


                // this adds text to the chatbox. it's output is local only, other players do not see this.
                CoreManager.Current.Actions.AddChatText($"This is my new decal plugin. Startup was called. AllegianceSkimmer", 1);

                ui = new ExampleUI();

                // UB
                _game = new Game();
                _game.Messages.Incoming.Allegiance_AllegianceInfoResponseEvent += Incoming_Allegiance_AllegianceInfoResponseEvent;
                
                // Timer
                globalTimer = new System.Windows.Forms.Timer();
                globalTimer.Tick += GlobalTimer_Tick;
                globalTimer.Interval = 300;
                globalTimer.Start();

                // Queue
                globalQueue = new Queue();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        /// <summary>
        /// Called when your plugin is unloaded. Either when logging out, closing the client, or hot reloading.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {


                // make sure to unsubscribe from any events we were subscribed to. Not doing so
                // can cause the old plugin to stay loaded between hot reloads.
                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;

                // Queue
                globalQueue = null;

                // Timer
                globalTimer.Stop();
                globalTimer.Dispose();
                globalTimer = null;

                // UB Shutdown
                _game.Messages.Incoming.Allegiance_AllegianceInfoResponseEvent -= Incoming_Allegiance_AllegianceInfoResponseEvent;
                _game = null;

                // clean up our ui view
                ui.Dispose();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        protected void FilterSetup(string assemblyDirectory)
        {
            AssemblyDirectory = assemblyDirectory;
        }

        /// <summary>
        /// CharacterFilter_LoginComplete event handler.
        /// </summary>
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            // it's generally a good idea to use try/catch blocks inside of decal event handlers.
            // throwing an uncaught exception inside one will generally hard crash the client.
            try
            {
                CoreManager.Current.Actions.AddChatText($"This is my new decal plugin. CharacterFilter_LoginComplete", 1);
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        public static Game Game()
        {
            return _game;
        }

        public static void StartScan()
        {
            if (!_game.Character.Allegiance.Exists)
            {
                Message("This character isn't in an allegiance. Not starting scan.");
                return;
            }

            uint monarch_id = _game.Character.Allegiance.Monarch.Id;
            string monarch_name = _game.Character.Allegiance.Monarch.Name;

            Message($"Starting scan for allegiance with monarch {monarch_id:X2} {monarch_name}.");
            currentScan = new Scan(new ScanItem(monarch_id, monarch_name));
            currentScan.Begin();
        }

        public static void StopScan()
        {
            if (currentScan != null)
            {
                currentScan.End();
                currentScan = null;
            }

            if (globalQueue != null)
            {
                globalQueue.Clear();

            }
        }

        public static void IterateScan()
        {
            if (currentScan == null)
            {
                Message("No active scan. Done.");
                return;
            }

            currentScan.Next();
        }

        /// <summary>
        /// Incoming_Allegiance_AllegianceInfoResponseEvent event handler.
        /// </summary>
        private void Incoming_Allegiance_AllegianceInfoResponseEvent(object sender, Allegiance_AllegianceInfoResponseEvent_S2C_EventArgs e)
        {
            // If there's no current scan, ignore
            if (currentScan == null)
            {
                Message("Not handling AllegianceInfoResponseEvent because we're not currently scanning");
                return;
            }

            currentScan.HandleInfo(e);

        }

        private void GlobalTimer_Tick(object sender, EventArgs e)
        {
            if (globalQueue == null)
            {
                return;
            }

            globalQueue.OnTick();
        }

        #region logging
        /// <summary>
        /// Log an exception to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="ex"></param>
        internal static void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        /// <summary>
        /// Log a string to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="message"></param>
        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(System.IO.Path.Combine(AssemblyDirectory, "log.txt"), $"{message}\n");

                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }

        public static void Message(string message)
        {
            CoreManager.Current.Actions.AddChatText($"{message}", 1);
        }
        #endregion // logging
    }
}
