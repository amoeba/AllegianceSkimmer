using System;

using Decal.Adapter;
using UtilityBelt.Common.Messages.Events;
using UtilityBelt.Scripting.Interop;

namespace AllegianceSkimmer
{
    [FriendlyName("AllegianceSkimmer")]
    public class PluginCore : PluginBase
    {
        private PluginUI ui;
        private static Game _game;
        public static Scan currentScan;
        public static System.Windows.Forms.Timer globalTimer;
        public static Queue globalQueue;

        public static string Website { get => "https://github.com/amoeba/AllegianceSkimmer"; }

        public static Game Game()
        {
            return _game;
        }

        protected override void Startup()
        {
            try
            {
                // Globals
                Globals.Init("AllegianceSkimmer");

                // Events
                CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
                CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
                
                // UI
                ui = new PluginUI();

                // UB
                _game = new Game();
                _game.Messages.Incoming.Allegiance_AllegianceInfoResponseEvent += Incoming_Allegiance_AllegianceInfoResponseEvent;


                // Timer
                globalTimer = new System.Windows.Forms.Timer();
                globalTimer.Tick += GlobalTimer_Tick;
                globalTimer.Interval = 20;
                globalTimer.Start();

                // Queue
                globalQueue = new Queue();

            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }

        protected override void Shutdown()
        {
            try
            {
                // Queue
                globalQueue = null;

                // Timer
                globalTimer.Stop();
                globalTimer.Dispose();
                globalTimer = null;

                // UB
                _game.Messages.Incoming.Allegiance_AllegianceInfoResponseEvent -= Incoming_Allegiance_AllegianceInfoResponseEvent;
                _game = null;

                // UI
                ui.Dispose();

                // Events
                CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;

                // Globals
                Globals.Destroy();

            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }

        protected void FilterSetup(string assemblyDirectory)
        {
            Utilities.AssemblyDirectory = assemblyDirectory;
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            try
            {
                // TODO
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }

        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            if (currentScan == null || !currentScan.IsActive)
            {
                return;
            }

            if (e.Text.StartsWith("Allegiance information for") || e.Text.StartsWith("Note: An a") || e.Text.StartsWith("   "))
            {
                e.Eat = true;
            }
        }

        private void Incoming_Allegiance_AllegianceInfoResponseEvent(object sender, Allegiance_AllegianceInfoResponseEvent_S2C_EventArgs e)
        {
            // If there's no current scan, ignore
            if (currentScan == null)
            {
                return;
            }

            currentScan.HandleInfo(e);
        }

        public static void StartScan()
        {
            if (!_game.Character.Allegiance.Exists)
            {
                Utilities.Message("This character isn't in an allegiance. Not starting scan.");
                return;
            }

            uint monarch_id = _game.Character.Allegiance.Monarch.Id;
            string monarch_name = _game.Character.Allegiance.Monarch.Name;
            uint expected = _game.Character.Allegiance.TotalMembers;

            currentScan = new Scan(new ScanItem(monarch_id, monarch_name, /*is_root*/true), expected);
            currentScan.Begin();
        }

        public static void StopScan()
        {
            if (currentScan == null)
            {
                Utilities.Message("Not currently scanning. Doing nothing.");
                return;
            }

            currentScan.End();
            currentScan = null;

            if (globalQueue != null)
            {
                globalQueue.Clear();

            }
        }

        public static void IterateScan()
        {
            if (currentScan == null)
            {
                Utilities.Message("No active scan. Done.");
                return;
            }

            currentScan.Next();
        }

        private void GlobalTimer_Tick(object sender, EventArgs e)
        {
            if (globalQueue == null)
            {
                return;
            }

            globalQueue.OnTick();
        }
    }
}
