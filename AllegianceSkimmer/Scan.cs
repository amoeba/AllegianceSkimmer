using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
        private List<ScanItem> children;
        private bool is_root;

        public ScanItem() { }
        public ScanItem(uint _id, string _name)
        {
            resolved = false;
            inflight = false;
            id = _id;
            name = _name;
            children = new List<ScanItem>();
            is_root = false;
        }

        public ScanItem(uint _id, string _name, bool _is_root)
        {
            resolved = false;
            inflight = false;
            id = _id;
            name = _name;
            children = new List<ScanItem>();
            is_root = _is_root;
        }

        public uint ObjectId { get => id; }
        public string Name { get => name; internal set => name = value; }
        public List<ScanItem> Children { get => children; internal set => children = value; }
        public bool Resolved { get { return resolved; } internal set { resolved = value; } }
        public bool InFlight { get { return inflight; } internal set { inflight = value; } }
        public bool IsRoot { get => is_root; }

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
                Utilities.Message("No scan active.");
                return;
            }

            // ---
            Utilities.Message("Finding first unresolved Scanitem...");
            var next = characters.Find(c => !c.Resolved);
            if (next == null)
            {
                Utilities.Message("Couldn't find any unresolved characters.");
                End();
                return;
            }
            Utilities.Message($"Next to be scanned is {next.Name}");

            // ---
            Utilities.Message($"Enqueueing @allegiance info {next.Name}");
            PluginCore.globalQueue.Enqueue(new InvokeChatTask($"@allegiance info {next.Name}"));
        }

        public void End() 
        {
            if (!active)
            {
                Utilities.Message("No scan active.");
                return;
            }

            active = false;
            ended_at = DateTime.Now;
            TimeSpan elapsed = ended_at - started_at;
            var nresolved = characters.FindAll(c => c.Resolved);
            var nunresolved = characters.FindAll(c => !c.Resolved);
            Utilities.Message($"Scan took {elapsed.TotalSeconds:F2} seconds and resolved {nresolved.Count} of {characters.Count} characters.");
        }

        public void HandleInfo(Allegiance_AllegianceInfoResponseEvent_S2C_EventArgs e)
        {
            uint id = e.Data.ObjectId;
            var records = e.Data.Profile.Hierarchy.Records;

            Utilities.Message($"HandleInfo for 0x{id:X2} with {records.Count} record(s).");

            // Find an inflight ScanItem for this objectId???
            var target = characters.Find(c => c.ObjectId == id);

            if (target == null)
            {
                Utilities.Message($"Didn't find matching ScanItem for 0x{id:X2}.");
                return;
            } else
            {
                Utilities.Message($"Found matching ScanItem for 0x{id:X2}. Continuing with work.");
            }

            // We can now mark the scan item as resolved and no longer in flight
            target.Resolved = true;
            target.InFlight = false;

            foreach (var record in records)
            {
                // Skip current target
                if (record.AllegianceData.ObjectId == target.ObjectId)
                {
                    Utilities.Message($"Skipping adding record for {record.AllegianceData.ObjectId:X2} {record.AllegianceData.Name}");
                    continue;
                }

                // Skip patron
                if (record.TreeParent != target.ObjectId)
                {
                    Utilities.Message($"Current record {record.AllegianceData.Name} is a patron of {target.Name}. Skipping.");
                    continue;
                }

                // Finally handle the new item to be scanned
                string name = record.AllegianceData.Name;
                Utilities.Message($"Adding {name} to scan queue.");
                var si = new ScanItem(record.AllegianceData.ObjectId, record.AllegianceData.Name);

                // We add the new ScanItem to two places:
                // 
                // 1. The target ScanItem's Children list. This makes building a JSON tree easier an
                //    isn't used for scanning purposes.
                // 2. Onto the scan queue. This is what's used by scanning.
                target.Children.Add( si );
                characters.Add(si);
            }

            Next();
        }
    }
}
