using System;
using System.Collections.Generic;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.HavenPrototype
{
    public static class HavenStarterHub
    {
        public static readonly string[] Names = { "Starter supplies", "Arcane supplies", "Companion recruitment", "Dungeon travel", "Repair service", "Optional test training", "Haven services guide" };
        private static readonly Point2D[] Sites = { new Point2D(3501,2574),new Point2D(3504,2583),new Point2D(3508,2571),new Point2D(3499,2579),new Point2D(3502,2581),new Point2D(3508,2583),new Point2D(3504,2571) };
        public static void Initialize()
        {
            CommandSystem.Register("haven", AccessLevel.Player, e => { if(HavenPreview.Enabled) e.Mobile.SendGump(new HavenHubGump(null,6)); });
            CommandSystem.Register("?", AccessLevel.Player, e => { if(HavenPreview.Enabled) e.Mobile.SendGump(new HavenCommandHelpGump()); });
            EventSink.ServerStarted += () => { if(HavenPreview.Enabled) Timer.DelayCall(TimeSpan.FromSeconds(2),Ensure); };
        }
        public static void Ensure()
        {
            if(!HavenPreview.Enabled) return;
            for(int i=0;i<Sites.Length;i++)
            {
                int service=i;
                if(World.Items.Values.OfType<HavenServiceStone>().Any(s=>!s.Deleted && s.Service==service && s.Map==Map.Trammel)) continue;
                Point3D landing;
                var site=Sites[i];
                var destination=new HavenPreview.Destination(Names[i],Map.Trammel,site.X,site.Y,Map.Trammel.GetAverageZ(site.X,site.Y));
                if(!HavenPreview.FindLanding(destination,out landing)) { Console.WriteLine("Haven hub: no safe placement for " + Names[i]); continue; }
                new HavenServiceStone(i).MoveToWorld(landing,Map.Trammel);
            }
            Console.WriteLine("Haven starter hub: " + World.Items.Values.OfType<HavenServiceStone>().Count(s=>!s.Deleted && s.Map==Map.Trammel) + " service stones available.");
        }
        public static bool CanUse(Mobile from,HavenServiceStone stone)
        {
            return HavenPreview.Enabled && from!=null && from.Player && from.Alive && stone!=null && !stone.Deleted && from.Map==stone.Map && from.InRange(stone,3) && from.InLOS(stone);
        }
        public static bool Claim(Mobile from,int service)
        {
            var account=from==null?null:from.Account as Account;
            if(!HavenPreview.Enabled || account==null || !from.Alive || from.Backpack==null || (service!=0 && service!=1)) return false;
            string key="HavenHub.Supplies:"+service+":"+from.Serial.Value;
            if(account.GetTag(key)!=null) { from.SendMessage("This character already claimed these supplies."); return false; }
            var kit=new List<Item>();
            if(service==0)
            {
                var sword=new Broadsword(); sword.WeaponAttributes.HitLeechMana=30;
                var mace=new WarMace(); mace.WeaponAttributes.HitLeechMana=30;
                kit.AddRange(new Item[] {sword,mace,new MetalKiteShield(),new Bow(),new Arrow(250),new LeatherChest(),new LeatherLegs(),new LeatherArms(),new LeatherGloves(),new LeatherGorget(),new LeatherCap(),new Bandage(200),new BankCheck(5000),new HavenResourceLedger()});
            }
            else
            {
                var robe=new Robe(); robe.Attributes.LowerRegCost=100; robe.Attributes.RegenMana=3;
                kit.AddRange(new Item[] {robe,new Spellbook(ulong.MaxValue),new NecromancerSpellbook(ulong.MaxValue),new SpellweavingBook(ulong.MaxValue),new BookOfChivalry(ulong.MaxValue),new BookOfMasteries()});
            }
            int items=0,weight=0;
            foreach(var item in kit)
            {
                if(!from.Backpack.CheckHold(from,item,false,true,items,weight)) { foreach(var unused in kit) unused.Delete(); from.SendMessage("Make room in your backpack; nothing was claimed."); return false; }
                items+=item.TotalItems+1; weight+=item.TotalWeight+item.PileWeight;
            }
            foreach(var item in kit) from.Backpack.DropItem(item);
            account.SetTag(key,"claimed"); from.SendMessage("Supplies delivered. Your skills and stats were not changed."); return true;
        }
        public static int Repair(Mobile from)
        {
            int count=0;
            var items=new List<Item>(from.Items);
            if(from.Backpack!=null) items.AddRange(from.Backpack.FindItemsByType(typeof(Item),true));
            foreach(var item in items.Distinct())
            {
                var weapon=item as BaseWeapon; var armor=item as BaseArmor;
                if(weapon!=null && weapon.HitPoints<weapon.MaxHitPoints) { weapon.HitPoints=weapon.MaxHitPoints; count++; }
                else if(armor!=null && armor.HitPoints<armor.MaxHitPoints) { armor.HitPoints=armor.MaxHitPoints; count++; }
            }
            return count;
        }
    }
    public class HavenServiceStone : Item
    {
        public int Service { get; private set; }
        public HavenServiceStone(int service):base(0xED4) { Service=service; Name=HavenStarterHub.Names[service]; Movable=false; Hue=service==5?0x489:0x47E; }
        public HavenServiceStone(Serial serial):base(serial) {}
        public override void OnDoubleClick(Mobile from) { if(HavenStarterHub.CanUse(from,this)) from.SendGump(new HavenHubGump(this,Service)); else from.SendMessage("Stand within three tiles of the service stone."); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(Service); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); Service=reader.ReadInt(); }
    }
    public class HavenHubGump : Gump
    {
        private readonly HavenServiceStone _stone; private readonly int _service;
        public HavenHubGump(HavenServiceStone stone,int service):base(50,50)
        {
            _stone=stone; _service=service;
            AddBackground(0,0,570,420,0xA28); AddLabel(24,20,0,HavenStarterHub.Names[service]);
            string[] details={
                "One starter kit per character: leather armor, sword and mace with 30% mana leech, shield, bow/arrows, bandages, 5,000 gold check and Resource Ledger. Equip the gear yourself. Skills and stats stay unchanged.",
                "One arcane kit per character: full Magery, Necromancy, Spellweaving and Chivalry books, a Book of Masteries and a reagent-saving robe. Normal skill, mastery learning and quest requirements still apply.",
                "Recruit your permanent companion or open his orders. Try Warrior, Caster or Archer. His missions and Resource Ledger are available through his menu.",
                "Open travel to New Haven, Luna, Royal City, Underworld, Doom, Blackthorn, Shadowguard and the Abyss. Travel requires leaving combat and clearing criminal status.",
                "Free preview repair: restores current durability on weapons and armor you wear or carry. Does not increase maximum durability or add properties.",
                "OPTIONAL TEST BOOST: the next screen offers a one-time test kit that sets all skills to 120 and stats to 100 each. Skip this stone if you want to train a new character normally. Opening the menu alone changes nothing.",
                "The plaza offers starter supplies, arcane supplies, companion recruitment, dungeon travel, repairs and optional test training.<BR><BR>Use [c for your companion, [home for starter housing and [havenmarks for rewards. Completed companion missions earn 2 Haven Marks per minute. Preview each reward before buying it.<BR><BR>Native vendors, bankers, healers and trainers remain available. Use [minichamp for three-wave expeditions and a boss, with rewards for every damage participant. The evolving gear stone offers starter equipment and robe upgrades. Use [wallet for gold storage and tithing; [havenluck shows the restored area bonus. Custom pets, the market and home island remain upcoming. Original-server progress is separate."};
            AddHtml(24,58,520,240,"<BASEFONT COLOR=#202020>"+details[service]+"</BASEFONT>",false,true);
            if(stone!=null && service!=6) { AddButton(24,362,0xFA5,0xFA7,1,GumpButtonType.Reply,0); AddLabel(60,362,0,service<=1?"Claim supplies":service==4?"Repair carried equipment":"Open service"); }
            if(service==0) {AddButton(24,315,0xFA5,0xFA7,5,GumpButtonType.Reply,0);AddLabel(60,315,0,"Get a free trash bag");}
            if(service==6) {AddButton(24,315,0xFA5,0xFA7,4,GumpButtonType.Reply,0);AddLabel(60,315,0,"Healers and corpse recovery");AddButton(24,362,0xFA5,0xFA7,2,GumpButtonType.Reply,0);AddLabel(60,362,0,"Browse Haven rewards");AddButton(245,362,0xFA5,0xFA7,3,GumpButtonType.Reply,0);AddLabel(279,362,0,"Mini champion");}
            AddButton(440,362,0xFA5,0xFA7,0,GumpButtonType.Reply,0); AddLabel(476,362,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var from=sender.Mobile;
            if(info.ButtonID==5 && _service==0 && HavenStarterHub.CanUse(from,_stone)) {HavenTrash.Claim(from);return;}
            if(info.ButtonID==4 && _service==6) {from.SendGump(new HavenRecoveryGump());return;}
            if(info.ButtonID==3 && _service==6) {var camp=HavenMiniChamp.Find();if(camp!=null)camp.Show(from);return;}
            if(info.ButtonID==2 && _service==6) {HavenMarks.Show(from);return;}
            if(info.ButtonID!=1 || !HavenStarterHub.CanUse(from,_stone)) return;
            switch(_service)
            {
                case 0: case 1: HavenStarterHub.Claim(from,_service); break;
                case 2: var companion=HavenCompanion.Claim(from); if(companion!=null) companion.Show(from); break;
                case 3: case 5: from.SendGump(new PreviewGump()); break;
                case 4: from.SendMessage("Repaired "+HavenStarterHub.Repair(from)+" item(s)."); break;
            }
        }
    }
}


