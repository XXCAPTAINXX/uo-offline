using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.HavenPrototype
{
    public enum CompanionMission { Supply, Mining, Lumber, Leather, Malas, Abyss }
    public partial class HavenCompanion
    {
        private CompanionMission _missionKind;
        private HavenResourceLedger _resourceLedger;
        private readonly Dictionary<int,int> _scheduledResources = new Dictionary<int,int>();
        private readonly Dictionary<int,int> _pendingResources = new Dictionary<int,int>();
        public CompanionMission MissionKind { get { return _missionKind; } }
        public long PendingResources { get { return _pendingResources.Values.Sum(value => (long)value); } }
        public HavenResourceLedger ResourceLedger
        {
            get { return _resourceLedger != null && !_resourceLedger.Deleted && Backpack != null && _resourceLedger.IsChildOf(Backpack) ? _resourceLedger : null; }
        }
        private HavenResourceLedger EnsureResourceLedger()
        {
            if (ResourceLedger != null) return ResourceLedger;
            if (Backpack == null) return null;
            _resourceLedger = Backpack.FindItemByType(typeof(HavenResourceLedger),true) as HavenResourceLedger;
            if (ResourceLedger != null) return ResourceLedger;
            var ledger = new HavenResourceLedger();
            if (!Backpack.CheckHold(this,ledger,false)) { ledger.Delete(); return null; }
            Backpack.DropItem(ledger); _resourceLedger = ledger; return ledger;
        }
        public void OpenResourceLedger(Mobile from)
        {
            if (!CanOpenPack(from)) { from.SendMessage("Recall your companion and stand beside him to open his ledger."); return; }
            DeliverResourceRewards();
            var ledger = EnsureResourceLedger();
            if (ledger == null) { from.SendMessage("Make one space in the companion pack for a ledger. Pending resources are retained."); return; }
            ledger.Show(from,0,1000,true);
        }
        // Snapshot rewards when dispatched: skill or equipment changes during a run cannot reroll them.
        private bool PrepareResourceMission(CompanionMission kind,int minutes)
        {
            if (kind < CompanionMission.Supply || kind > CompanionMission.Abyss) return false;
            var rewards = new Dictionary<int,int>();
            switch (kind)
            {
                case CompanionMission.Mining:
                    double mining = Skills.Mining.Base;
                    int metal = mining >= 99 ? 8 : mining >= 95 ? 7 : mining >= 90 ? 6 : mining >= 85 ? 5 : mining >= 80 ? 4 : mining >= 75 ? 3 : mining >= 70 ? 2 : mining >= 65 ? 1 : 0;
                    rewards.Add(metal,minutes*20); break;
                case CompanionMission.Lumber:
                    double lumber = Skills.Lumberjacking.Base;
                    int wood = lumber >= 100 ? 15 : lumber >= 95 ? 14 : lumber >= 90 ? 13 : lumber >= 85 ? 12 : lumber >= 80 ? 11 : lumber >= 65 ? 10 : 9;
                    rewards.Add(wood,minutes*20); break;
                case CompanionMission.Leather:
                    double lore = Skills.AnimalLore.Base;
                    rewards.Add(lore >= 100 ? 19 : lore >= 90 ? 18 : lore >= 70 ? 17 : 16,minutes*10); break;
                case CompanionMission.Malas:
                    if (Math.Max(Skills.Magery.Base,Skills.Tactics.Base) < 60) return false;
                    for (int id=29;id<=35;id++) rewards.Add(id,minutes*2);
                    break;
                case CompanionMission.Abyss:
                    if (Math.Max(Skills.Magery.Base,Skills.Tactics.Base) < 80) return false;
                    for (int id=36;id<47;id++) rewards.Add(id,minutes);
                    break;
            }
            foreach (var pair in rewards)
            {
                int pending; _pendingResources.TryGetValue(pair.Key,out pending);
                if (pair.Value > HavenResourceLedger.MaxBalance-pending) return false;
            }
            _missionKind = kind; _scheduledResources.Clear();
            foreach (var pair in rewards) _scheduledResources.Add(pair.Key,pair.Value);
            return true;
        }
        private string FinishResourceMission()
        {
            var report = new List<string>();
            foreach (var pair in _scheduledResources)
            {
                int existing; _pendingResources.TryGetValue(pair.Key,out existing);
                _pendingResources[pair.Key] = checked(existing + pair.Value);
                report.Add(pair.Value + " " + HavenResources.Names[pair.Key]);
            }
            _scheduledResources.Clear();
            Skill skill = _missionKind == CompanionMission.Mining ? Skills.Mining : _missionKind == CompanionMission.Lumber ? Skills.Lumberjacking : _missionKind == CompanionMission.Leather ? Skills.AnimalLore : null;
            if (skill != null && skill.Base < skill.Cap) skill.BaseFixedPoint = Math.Min(skill.CapFixedPoint,skill.BaseFixedPoint + HavenCompanionProgression.Amount(this,skill,_missionMinutes*2));
            return _missionKind + " run completed." + (report.Count==0 ? "" : " Resources: " + String.Join(", ",report) + ". Stored in his ledger; overflow waits for space.");
        }
        public override void OnSubItemAdded(Item item)
        {
            base.OnSubItemAdded(item);
            if (Backpack == null || item == null || item.Parent != Backpack) return;
            Timer.DelayCall(TimeSpan.Zero, () => {
                if (Deleted || Backpack == null || item.Deleted || item.Parent != Backpack) return;
                var ledger = EnsureResourceLedger();
                if (ledger != null) ledger.AbsorbCarriedResource(this,item);
            });
        }
        private void DeliverResourceRewards()
        {
            if (_pendingResources.Count==0) return;
            var ledger = EnsureResourceLedger(); if (ledger==null) return;
            foreach (var pair in _pendingResources.ToArray())
            {
                int amount=Math.Min(pair.Value,HavenResourceLedger.MaxBalance-ledger.Balance(pair.Key));
                if(amount<=0 || !ledger.Credit(pair.Key,amount)) continue;
                if(amount==pair.Value) _pendingResources.Remove(pair.Key); else _pendingResources[pair.Key]=pair.Value-amount;
            }
        }
        private void SerializeResourceMissions(GenericWriter writer)
        {
            writer.Write((int)_missionKind); writer.Write(_resourceLedger);
            WriteResources(writer,_scheduledResources); WriteResources(writer,_pendingResources);
        }
        private void DeserializeResourceMissions(GenericReader reader)
        {
            _missionKind=(CompanionMission)reader.ReadInt(); _resourceLedger=reader.ReadItem() as HavenResourceLedger;
            if(_missionKind<CompanionMission.Supply || _missionKind>CompanionMission.Abyss) throw new InvalidOperationException("Unknown companion mission");
            ReadResources(reader,_scheduledResources); ReadResources(reader,_pendingResources);
        }
        private static void WriteResources(GenericWriter writer,Dictionary<int,int> resources)
        {
            writer.Write(resources.Count); foreach(var pair in resources) { writer.Write(pair.Key); writer.Write(pair.Value); }
        }
        private static void ReadResources(GenericReader reader,Dictionary<int,int> resources)
        {
            int count=reader.ReadInt(); if(count<0 || count>HavenResources.Types.Length) throw new InvalidOperationException("Invalid mission resource count");
            for(int i=0;i<count;i++)
            {
                int id=reader.ReadInt(),amount=reader.ReadInt();
                if(!HavenResources.Valid(id) || amount<=0 || amount>HavenResourceLedger.MaxBalance || resources.ContainsKey(id)) throw new InvalidOperationException("Invalid mission resource balance");
                resources.Add(id,amount);
            }
        }
    }
    public class CompanionResourceMissionGump : Gump
    {
        private readonly HavenCompanion _companion; private readonly int _minutes;
        public CompanionResourceMissionGump(HavenCompanion companion,int minutes) : base(65,65)
        {
            _companion=companion; _minutes=minutes;
            AddBackground(0,0,620,535,0xA28); AddLabel(24,20,0,"Companion resource missions");
            AddLabel(24,55,0,"Duration: " + minutes + " minutes. Rewards and training scale with time.");
            Button(24,90,1,"5 minutes"); Button(220,90,2,"15 minutes"); Button(410,90,3,"30 minutes");
            string[] titles={"Mining - highest unlocked metal", "Lumber - highest unlocked wood", "Leather - highest unlocked hide", "Malas - reagents and Doom bones", "Abyss - eleven crafting essences"};
            string[] details={"20 ingots/min; Mining " + companion.Skills.Mining.Base.ToString("F1"),"20 logs/min; Lumberjacking " + companion.Skills.Lumberjacking.Base.ToString("F1"),"10 leather/min; Animal Lore " + companion.Skills.AnimalLore.Base.ToString("F1"),"2 of each/min; needs 60 Magery or Tactics", "1 of each/min; needs 80 Magery or Tactics"};
            for(int i=0;i<titles.Length;i++) { Button(24,140+i*57,10+i,titles[i]); AddLabel(58,163+i*57,0,details[i]); }
            AddLabel(24,435,0,"Also earns 100 gold/min. Materials go into his ledger.");
            AddLabel(24,460,0,"Pending resource units: " + companion.PendingResources);
            Button(24,497,0,"Back"); Button(380,497,20,"Open his ledger");
        }
        private void Button(int x,int y,int id,string label) { AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0); AddLabel(x+34,y,0,label); }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var from=sender.Mobile; if(!_companion.IsOwner(from)) return;
            if(info.ButtonID>=1 && info.ButtonID<=3) { from.SendGump(new CompanionResourceMissionGump(_companion,info.ButtonID==1?5:info.ButtonID==2?15:30)); return; }
            if(info.ButtonID==20) { _companion.OpenResourceLedger(from); return; }
            if(info.ButtonID>=10 && info.ButtonID<=14 && !_companion.StartMission(from,_minutes,(CompanionMission)(info.ButtonID-9))) from.SendMessage("Cannot start: check distance, combat, casting, skill and pending rewards.");
            _companion.Show(from);
        }
    }
}
