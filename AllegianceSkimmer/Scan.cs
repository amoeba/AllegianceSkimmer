using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Decal.Adapter;
using ProtoBuf.WellKnownTypes;
using UtilityBelt.Common.Messages.Events;

namespace AllegianceSkimmer
{
    public class ScanItem
    {
        private bool resolved;
        private bool inflight;
        private uint id;
        private string name;
        private List<string> vassals;

        public ScanItem(uint _id, string _name)
        {
            resolved = false;
            inflight = false;
            id = _id;
            name = _name;
            vassals = new List<string>();
        }

        public uint ObjectId { get { return id; } }

        public string Name
        {
            get { return name; }
        }

        public bool Resolved
        {
            get { return resolved; }
            internal set { resolved = value; }
        }

        public bool InFlight
        {
            get { return inflight; }
            internal set { inflight = value; }
        }

        public List<string> Vassals
        {
            get { return vassals; }
        }

    }

    public class Scan
    {
        private bool active;
        public List<ScanItem> characters;
        private string root;
        DateTime started_at;
        DateTime ended_at;

        public Scan(ScanItem _root)
        {
            active = false;
            characters = new List<ScanItem>([_root]);
            root = _root.Name;
        }

        public void Begin()
        {
            if (!active)
            {
                active = true;
            }

            started_at = DateTime.Now;

            // Issue a @allegiance info command on Monarch
            // For each vassal, issue allegiance info
            Next();
        }

        public void Next()
        {
            if (!active)
            {
                PluginCore.Message("No scan active.");
                return;
            }

            // ---
            PluginCore.Message("Finding first unresolved Scanitem...");
            var next = characters.Find(c => !c.Resolved);
            if (next == null)
            {
                PluginCore.Message("Couldn't find any unresolved characters.");
                End();
                return;
            }
            PluginCore.Message($"Next to be scanned is {next.Name}");

            // ---
            PluginCore.Message($"Enqueueing @allegiance info {next.Name}");
            PluginCore.globalQueue.Enqueue(new InvokeChatTask($"@allegiance info {next.Name}"));
        }

        public void End() 
        {
            if (!active)
            {
                PluginCore.Message("No scan active.");
                return;
            }

            active = false;
            ended_at = DateTime.Now;
            TimeSpan elapsed = ended_at - started_at;
            var nresolved = characters.FindAll(c => c.Resolved);
            var nunresolved = characters.FindAll(c => !c.Resolved);
            PluginCore.Message($"Scan took {elapsed.TotalSeconds:F2} seconds and resolved {nresolved.Count} of {characters.Count} characters.");
        }

        public void HandleInfo(Allegiance_AllegianceInfoResponseEvent_S2C_EventArgs e)
        {
            uint id = e.Data.ObjectId;
            var records = e.Data.Profile.Hierarchy.Records;

            PluginCore.Message($"HandleInfo for 0x{id:X2} with {records.Count} record(s).");

            // Find an inflight ScanItem for this objectId???
            var target = characters.Find(c => c.ObjectId == id);

            if (target == null)
            {
                PluginCore.Message($"Didn't find matching ScanItem for 0x{id:X2}.");
                return;
            } else
            {
                PluginCore.Message($"Found matching ScanItem for 0x{id:X2}. Continuing with work.");
            }

            // We can now mark the scan item as resolved and no longer in flight
            target.Resolved = true;
            target.InFlight = false;

            foreach (var record in records)
            {
                // Skip current target
                if (record.AllegianceData.ObjectId == target.ObjectId)
                {
                    PluginCore.Message($"Skipping adding record for {record.AllegianceData.ObjectId:X2} {record.AllegianceData.Name}");
                    continue;
                }

                // Skip patron

                if (record.TreeParent != target.ObjectId)
                {
                    PluginCore.Message($"Current record {record.AllegianceData.Name} is a patron of {target.Name}. Skipping.");
                    continue;
                }

                // Set newly-found vassals on current scan item
                PluginCore.Message($"Adding vassal {record.AllegianceData.Name} to {target.Name}");
                target.Vassals.Add(record.AllegianceData.Name);

                // Then push a new Scanitem for each vassal onto the queue
                string name = record.AllegianceData.Name;
                PluginCore.Message($"Adding {name} to scan queue.");
                characters.Add(new ScanItem(record.AllegianceData.ObjectId, record.AllegianceData.Name));
            }

            Next();

        }
    }
}
