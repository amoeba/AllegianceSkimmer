using System;
using System.Collections.Generic;

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
        uint expected_size;
        DateTime started_at;
        DateTime ended_at;

        public uint ExpectedSize { get => expected_size; }
        public bool IsActive {  get => active; }

        public Scan(ScanItem _root, uint _expected_size)
        {
            active = false;
            characters = new List<ScanItem>([_root]);
            root = _root.Name;
            expected_size = _expected_size;
        }

        public void Begin()
        {
            if (!active)
            {
                active = true;
            }

            started_at = DateTime.Now;

            Next();
        }

        public void Next()
        {
            if (!active)
            {
                Utilities.Message("No scan active.");

                return;
            }

            var next = characters.Find(c => !c.Resolved);
            if (next == null)
            {
                Utilities.Message("Couldn't find any unresolved characters. Scan is complete.");
                End();

                return;
            }

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

            Utilities.Message(Summary());
        }

        public void HandleInfo(Allegiance_AllegianceInfoResponseEvent_S2C_EventArgs e)
        {
            uint id = e.Data.ObjectId;
            var records = e.Data.Profile.Hierarchy.Records;

            // First we have to find a ScanItem matching this id
            var target = characters.Find(c => c.ObjectId == id);

            if (target == null)
            {
                Utilities.Message($"Didn't find matching ScanItem for 0x{id:X2}.");

                return;
            }

            // We can now mark the scan item as resolved and no longer in flight
            target.Resolved = true;
            target.InFlight = false;

            // And update children
            foreach (var record in records)
            {
                // Skip current target
                if (record.AllegianceData.ObjectId == target.ObjectId)
                {
                    continue;
                }

                // Skip patron
                if (record.TreeParent != target.ObjectId)
                {
                    continue;
                }

                // Finally handle the new item to be scanned
                string name = record.AllegianceData.Name;
                var si = new ScanItem(record.AllegianceData.ObjectId, record.AllegianceData.Name);

                // We add the new ScanItem to two places:
                // 
                // 1. The target ScanItem's Children list. This makes building a JSON tree easier an
                //    isn't used for scanning purposes.
                // 2. Onto the scan queue. This is what's used by scanning.
                target.Children.Add(si);
                characters.Add(si);
            }

            // Last, continue working
            Next();
        }

        public string Summary()
        {
            TimeSpan elapsed = ended_at - started_at;
            var nresolved = characters.FindAll(c => c.Resolved);
            var nunresolved = characters.FindAll(c => !c.Resolved);

            return $"Scan took {elapsed.TotalSeconds:F2} seconds and resolved {nresolved.Count} of {characters.Count} characters.";
        }
    }
}
