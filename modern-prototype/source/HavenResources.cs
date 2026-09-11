using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public static class HavenResources
    {
        // Serialized IDs: append entries only; never reorder this catalog.
        public static readonly Type[] Types = {
            typeof(IronIngot),typeof(DullCopperIngot),typeof(ShadowIronIngot),typeof(CopperIngot),typeof(BronzeIngot),typeof(GoldIngot),typeof(AgapiteIngot),typeof(VeriteIngot),typeof(ValoriteIngot),
            typeof(Log),typeof(OakLog),typeof(AshLog),typeof(YewLog),typeof(HeartwoodLog),typeof(BloodwoodLog),typeof(FrostwoodLog),
            typeof(Leather),typeof(SpinedLeather),typeof(HornedLeather),typeof(BarbedLeather),
            typeof(Amber),typeof(Amethyst),typeof(Citrine),typeof(Diamond),typeof(Emerald),typeof(Ruby),typeof(Sapphire),typeof(StarSapphire),typeof(Tourmaline),
            typeof(BatWing),typeof(GraveDust),typeof(DaemonBlood),typeof(NoxCrystal),typeof(PigIron),typeof(Bone),typeof(DaemonBone),
            typeof(EssenceAchievement),typeof(EssenceBalance),typeof(EssenceControl),typeof(EssenceDiligence),typeof(EssenceDirection),typeof(EssenceFeeling),typeof(EssenceOrder),typeof(EssencePassion),typeof(EssencePersistence),typeof(EssencePrecision),typeof(EssenceSingularity),
            typeof(Hides),typeof(SpinedHides),typeof(HornedHides),typeof(BarbedHides),typeof(RedScales),typeof(YellowScales),typeof(BlackScales),typeof(GreenScales),typeof(WhiteScales),typeof(BlueScales),typeof(Feather),typeof(Wool),typeof(TaintedWool),typeof(RawRibs),typeof(RawBird),typeof(RawLambLeg),typeof(RawRotwormMeat),typeof(DragonBlood),typeof(Fur),typeof(Sand),typeof(Saltpeter),typeof(WhitePearl),typeof(DelicateScales),typeof(RawFishSteak),typeof(BarkFragment),typeof(LuminescentFungi),typeof(SwitchItem),typeof(ParasiticPlant),typeof(BrilliantAmber),typeof(CrystalShards),typeof(BlueDiamond),typeof(DarkSapphire),typeof(EcruCitrine),typeof(FireRuby),typeof(PerfectEmerald),typeof(Turquoise),typeof(BlackPearl),typeof(Bloodmoss),typeof(Garlic),typeof(Ginseng),typeof(MandrakeRoot),typeof(Nightshade),typeof(SulfurousAsh),typeof(SpidersSilk),typeof(DaemonClaw),typeof(LavaSerpentCrust),typeof(GoblinBlood),typeof(FaeryDust),typeof(FeyWings),typeof(VialOfVitriol),typeof(VoidOrb),typeof(UndyingFlesh),typeof(ReflectiveWolfEye),typeof(CrystallineBlackrock),typeof(ArcanicRuneStone),typeof(SeedOfRenewal),typeof(SpiderCarapace),typeof(BottleIchor),typeof(SilverSnakeSkin)
        };
        public static readonly string[] Names = {
            "Iron ingots","Dull copper ingots","Shadow iron ingots","Copper ingots","Bronze ingots","Gold ingots","Agapite ingots","Verite ingots","Valorite ingots",
            "Logs","Oak logs","Ash logs","Yew logs","Heartwood logs","Bloodwood logs","Frostwood logs",
            "Leather","Spined leather","Horned leather","Barbed leather",
            "Amber","Amethyst","Citrine","Diamond","Emerald","Ruby","Sapphire","Star sapphire","Tourmaline",
            "Bat wings","Grave dust","Daemon blood","Nox crystals","Pig iron","Bones","Daemon bones",
            "Essence of achievement","Essence of balance","Essence of control","Essence of diligence","Essence of direction","Essence of feeling","Essence of order","Essence of passion","Essence of persistence","Essence of precision","Essence of singularity",
            "Hides","Spined Hides","Horned Hides","Barbed Hides","Red Scales","Yellow Scales","Black Scales","Green Scales","White Scales","Blue Scales","Feather","Wool","Tainted Wool","Raw Ribs","Raw Bird","Raw Lamb Leg","Raw Rotworm Meat","Dragon Blood","Fur","Sand","Saltpeter","White Pearl","Delicate Scales","Raw Fish Steak","Bark Fragment","Luminescent Fungi","Switch Item","Parasitic Plant","Brilliant Amber","Crystal Shards","Blue Diamond","Dark Sapphire","Ecru Citrine","Fire Ruby","Perfect Emerald","Turquoise","BlackPearl","Bloodmoss","Garlic","Ginseng","MandrakeRoot","Nightshade","SulfurousAsh","SpidersSilk","DaemonClaw","LavaSerpentCrust","GoblinBlood","FaeryDust","FeyWings","VialOfVitriol","VoidOrb","UndyingFlesh","ReflectiveWolfEye","CrystallineBlackrock","ArcanicRuneStone","SeedOfRenewal","SpiderCarapace","BottleIchor","SilverSnakeSkin"
        };
        public static bool Valid(int id) { return id >= 0 && id < Types.Length; }
        public static Item Create(int id,int units)
        {
            if (!Valid(id) || units < 1 || units > 60000) return null;
            var item = (Item)Activator.CreateInstance(Types[id]);
            if (!item.Stackable) { item.Delete(); return null; }
            item.Amount = units; return item;
        }
        public static bool Accessible(Mobile from,Item item)
        {
            if (from == null || from.Deleted || !from.Alive || item == null || item.Deleted) return false;
            bool held = from.Backpack != null && (item == from.Backpack || item.IsChildOf(from.Backpack));
            var companion = item.RootParent as HavenCompanion;
            if (!held && (companion == null || !companion.CanOpenPack(from) || (item != companion.Backpack && !item.IsChildOf(companion.Backpack)))) return false;
            for (Item parent = item; parent != null; parent = parent.Parent as Item)
            {
                var locked = parent as LockableContainer;
                var trapped = parent as TrapableContainer;
                if ((locked != null && locked.Locked) || (trapped != null && trapped.TrapType != TrapType.None)) return false;
            }
            return true;
        }
    }

    public class HavenResourceLedger : Item
    {
        public const int MaxBalance = 1000000000;
        private readonly Dictionary<int,int> _balances = new Dictionary<int,int>();
        [Constructable]
        public HavenResourceLedger() : base(0x2259) { Name = "Haven resource ledger"; Weight = 1; LootType = LootType.Blessed; }
        public int Balance(int id) { int value; return _balances.TryGetValue(id,out value) ? value : 0; }
        public virtual bool CanUse(Mobile from) { return HavenResources.Accessible(from,this); }
        internal bool AbsorbCarriedResource(HavenCompanion carrier,Item item) { if(carrier.Backpack==null || !IsChildOf(carrier.Backpack) || item==null || item.Deleted || item.Parent!=carrier.Backpack) return false; int id=Array.IndexOf(HavenResources.Types,item.GetType()); if(!HavenResources.Valid(id)||!item.Stackable||!Credit(id,item.Amount))return false; item.Delete();return true; }
        internal bool Credit(int id,int units)
        {
            if (!HavenResources.Valid(id) || units <= 0 || units > MaxBalance - Balance(id)) return false;
            _balances[id] = Balance(id) + units; InvalidateProperties(); return true;
        }
        public int Absorb(Mobile from,Item source)
        {
            if (!CanUse(from) || !HavenResources.Accessible(from,source) || source == this) return 0;
            var bag = source as Container;
            if (bag != null)
            {
                int count = 0;
                foreach (var child in bag.Items.ToArray()) count += Absorb(from,child);
                return count;
            }
            var deed = source as HavenResourceDeed;
            var commodityDeed = source as CommodityDeed;
            var resource = commodityDeed == null ? source : commodityDeed.Commodity;
            if (resource == null || resource.Deleted) return 0;
            int id = deed != null ? deed.ResourceId : Array.IndexOf(HavenResources.Types,resource.GetType());
            if (!HavenResources.Valid(id)) return 0;
            int units = deed != null ? deed.Units : resource.Amount;
            if (!Credit(id,units)) return 0;
            source.Delete(); return 1;
        }
        public bool Withdraw(Mobile from,int id,int units,bool asDeed)
        {
            if (!CanUse(from) || !HavenResources.Valid(id) || units <= 0 || units > 60000 || Balance(id) < units || from.Backpack == null) return false;
            Item item = asDeed ? (Item)new HavenResourceDeed(id,units) : HavenResources.Create(id,units);
            if (item == null) return false;
            if (!from.Backpack.CheckHold(from,item,false)) { item.Delete(); return false; }
            from.Backpack.DropItem(item); _balances[id] = Balance(id) - units; InvalidateProperties(); return true;
        }
        public bool TransferAll(Mobile from,HavenResourceLedger target)
        {
            if (!CanUse(from) || target == null || target == this || !target.CanUse(from)) return false;
            foreach (var pair in _balances) if (pair.Value > MaxBalance - target.Balance(pair.Key)) return false;
            foreach (var pair in _balances) target._balances[pair.Key] = target.Balance(pair.Key) + pair.Value;
            _balances.Clear(); InvalidateProperties(); target.InvalidateProperties(); return true;
        }
        public override void OnDoubleClick(Mobile from) { if (CanUse(from)) Show(from,0,1000,true); else from.SendMessage("Keep the ledger in your pack or your nearby companion's pack."); }
        public void Show(Mobile from,int page,int amount,bool deeds)
        {
            if (!CanUse(from)) return;
            from.CloseGump(typeof(ResourceLedgerGump)); from.SendGump(new ResourceLedgerGump(this,page,amount,deeds));
        }
        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); list.Add("Stored resource types: {0}",_balances.Count(x=>x.Value>0)); list.Add("Balances use no extra backpack slots"); }
        public HavenResourceLedger(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer); writer.Write(0); writer.Write(_balances.Count);
            foreach (var pair in _balances) { writer.Write(pair.Key); writer.Write(pair.Value); }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader); reader.ReadInt(); int count = reader.ReadInt();
            if (count < 0 || count > HavenResources.Types.Length) throw new InvalidOperationException("Invalid ledger catalog size");
            for (int i=0;i<count;i++)
            {
                int id=reader.ReadInt(), units=reader.ReadInt();
                if (!HavenResources.Valid(id) || units<0 || units>MaxBalance || _balances.ContainsKey(id)) throw new InvalidOperationException("Invalid ledger balance");
                _balances.Add(id,units);
            }
        }
    }
    public class HavenResourceDeed : Item
    {
        private int _resourceId, _units;
        public int ResourceId { get { return _resourceId; } }
        public int Units { get { return _units; } }
        [Constructable]
        public HavenResourceDeed() : this(0,1) { }
        public HavenResourceDeed(int id,int units) : base(0x14F0)
        {
            if (!HavenResources.Valid(id) || units<1 || units>60000) throw new ArgumentOutOfRangeException();
            _resourceId=id; _units=units; Weight=1; Name="resource deed";
        }
        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); list.Add("{0}: {1}",HavenResources.Names[_resourceId],_units); list.Add("Absorb into a Haven resource ledger or double-click to redeem."); }
        public bool Redeem(Mobile from)
        {
            if (!HavenResources.Accessible(from,this) || from.Backpack==null) return false;
            var resource=HavenResources.Create(_resourceId,_units);
            if (resource==null) return false;
            if (!from.Backpack.CheckHold(from,resource,false)) { resource.Delete(); return false; }
            from.Backpack.DropItem(resource); Delete(); return true;
        }
        public override void OnDoubleClick(Mobile from) { if (!Redeem(from)) from.SendMessage("Make room for the resources, or absorb this deed into a ledger."); }
        public HavenResourceDeed(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(_resourceId); writer.Write(_units); }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader); reader.ReadInt(); _resourceId=reader.ReadInt(); _units=reader.ReadInt();
            if (!HavenResources.Valid(_resourceId) || _units<1 || _units>60000) throw new InvalidOperationException("Invalid resource deed");
        }
    }
    public class ResourceLedgerGump : Gump
    {
        private readonly HavenResourceLedger _ledger;
        private readonly int _page, _amount;
        private readonly bool _deeds;
        private const int PageSize=9;
        public ResourceLedgerGump(HavenResourceLedger ledger,int page,int amount,bool deeds) : base(60,60)
        {
            _ledger=ledger; _page=Math.Max(0,Math.Min((HavenResources.Types.Length-1)/PageSize,page)); _amount=amount; _deeds=deeds;
            AddBackground(0,0,540,585,0xA28); AddLabel(24,20,0,ledger is HavenGuildResourceLedger ? "Shared guild resource ledger (all members)" : "Resource ledger");
            AddLabel(24,55,0,"Select a resource to withdraw the amount below.");
            for(int row=0;row<PageSize;row++)
            {
                int id=_page*PageSize+row; if(id>=HavenResources.Types.Length) break;
                Button(24,95+row*31,100+id,HavenResources.Names[id]); AddLabel(385,95+row*31,0,ledger.Balance(id).ToString("N0"));
            }
            AddLabel(24,385,0,"Amount (1-60,000)"); AddBackground(185,379,108,28,0xBB8); AddTextEntry(194,384,90,22,0,1,amount.ToString());
            Button(315,384,4,deeds?"Deeds":"Loose resources");
            Button(24,430,1,"Absorb my pack"); Button(285,430,2,"Target item / bag");
            Button(24,468,3,"Transfer all..."); Button(285,468,5,"Previous"); Button(405,468,6,"Next");
            AddLabel(24,509,0,"Page "+(_page+1)+" / "+((HavenResources.Types.Length-1)/PageSize+1)); Button(405,549,0,"Close");
            Button(24,509,7,"Give to me"); Button(190,509,8,"Give to guild"); Button(355,509,9,"Guild ledger");
        }
        private void Button(int x,int y,int id,string label) { AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0); AddLabel(x+34,y,0,label); }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var from=sender.Mobile; if(info.ButtonID==0 || !_ledger.CanUse(from)) return;
            int amount; if(!Int32.TryParse(info.GetTextEntry(1)?.Text,out amount) || amount<1 || amount>60000) { from.SendMessage("Enter an amount from 1 to 60,000."); _ledger.Show(from,_page,_amount,_deeds); return; }
            int page=_page; bool deeds=_deeds;
            if(info.ButtonID==1) from.SendMessage("Absorbed "+_ledger.Absorb(from,from.Backpack)+" resource stacks/deeds. Unsupported items stayed in place.");
            else if(info.ButtonID==2 || info.ButtonID==3) { from.Target=new LedgerTarget(_ledger,info.ButtonID==3,_page,amount,deeds); return; }
            else if(info.ButtonID==7) { var personal=from.Backpack.FindItemsByType(typeof(HavenResourceLedger),true).OfType<HavenResourceLedger>().FirstOrDefault(x=>x!=_ledger); if(personal==null) { personal=new HavenResourceLedger(); if(!from.Backpack.TryDropItem(from,personal,false)){personal.Delete();personal=null;} } from.SendMessage(personal!=null&&_ledger.TransferAll(from,personal)?"Resources transferred to your ledger.":"Transfer unavailable; balances retained."); }
            else if(info.ButtonID==8) { var guild=HavenGuildResourceLedger.For(from); from.SendMessage(guild!=null&&_ledger.TransferAll(from,guild)?"Resources transferred to your guild ledger.":"Transfer unavailable: check guild membership and capacity. Balances retained."); }
            else if(info.ButtonID==9) { var guild=HavenGuildResourceLedger.For(from); if(guild!=null){guild.Show(from,0,amount,deeds);return;} from.SendMessage("Join a guild to use its shared ledger."); }
            else if(info.ButtonID==4) deeds=!deeds;
            else if(info.ButtonID==5) page--; else if(info.ButtonID==6) page++;
            else if(info.ButtonID>=100 && !_ledger.Withdraw(from,info.ButtonID-100,amount,deeds)) from.SendMessage("Withdrawal unavailable: check the balance and free pack space/weight.");
            _ledger.Show(from,page,amount,deeds);
        }
        private class LedgerTarget : Target
        {
            private readonly HavenResourceLedger _ledger; private readonly bool _transfer,_deeds; private readonly int _page,_amount;
            public LedgerTarget(HavenResourceLedger ledger,bool transfer,int page,int amount,bool deeds) : base(2,false,TargetFlags.None) { _ledger=ledger;_transfer=transfer;_page=page;_amount=amount;_deeds=deeds; }
            protected override void OnTarget(Mobile from,object target)
            {
                if(_transfer) from.SendMessage(_ledger.TransferAll(from,target as HavenResourceLedger)?"All balances transferred.":"Transfer unavailable; nothing changed.");
                else from.SendMessage("Absorbed "+_ledger.Absorb(from,target as Item)+" resource stacks/deeds.");
                _ledger.Show(from,_page,_amount,_deeds);
            }
        }
    }
}
