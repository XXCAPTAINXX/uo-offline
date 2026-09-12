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
        public static readonly string[] Names = { "Starter supplies", "Arcane supplies", "Companion recruitment", "Travel stone", "Repair service", "Optional test training", "Haven services guide" };
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
            int[] services={0,1,9,10,3,6};
            foreach(var oldPost in World.Items.Values.OfType<HavenServicePost>().ToArray())oldPost.Delete();
            var sites=new[]{new Point2D(3499,2574),new Point2D(3499,2577),new Point2D(3499,2580),new Point2D(3502,2584),new Point2D(3505,2584),new Point2D(3508,2584)};
            foreach(var old in World.Items.Values.OfType<HavenServiceStone>().Where(x=>x.Map==Map.Trammel&&!services.Contains(x.Service)).ToArray())old.Delete();
            for(int i=0;i<services.Length;i++){
                int service=services[i];var board=World.Items.Values.OfType<HavenServiceStone>().FirstOrDefault(x=>!x.Deleted&&x.Service==service&&x.Map==Map.Trammel);
                if(board==null||board.ItemID!=0xED4){
                    if(board!=null)board.Internalize();Point3D landing;var site=sites[i];
                    if(!HavenPreview.FindLanding(new HavenPreview.Destination("Haven services",Map.Trammel,site.X,site.Y,Map.Trammel.GetAverageZ(site.X,site.Y)),out landing)){if(board!=null)board.MoveToWorld(new Point3D(site.X,site.Y,Map.Trammel.GetAverageZ(site.X,site.Y)),Map.Trammel);continue;}
                    if(board==null)board=new HavenServiceStone(service);board.ItemID=0xED4;board.Hue=0;board.Name=service==9?"Training supplies":service==10?"Special rewards":service==1?"Arcane supplies":service==0?"Haven supplies and equipment":service==3?"Haven travel":"Haven help and companions";board.MoveToWorld(landing,Map.Trammel);
                }
                if(!World.Items.Values.OfType<HavenPlazaPlanter>().Any(x=>!x.Deleted&&x.Stone==board)){var planter=new HavenPlazaPlanter(board);var point=new Point3D(board.X+(i<3?-1:0),board.Y+(i<3?0:1),board.Z);if(board.Map.CanFit(point,16,false,true))planter.MoveToWorld(point,board.Map);else planter.Delete();}

            }
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
        public HavenServiceStone(int service):base(0xED4) { Service=service; Name=service==9?"Training supplies":service==10?"Special rewards":HavenStarterHub.Names[service]; Movable=false; Hue=service==5?0x489:0x47E; }
        public HavenServiceStone(Serial serial):base(serial) {}
        public override void OnDoubleClick(Mobile from) { if(HavenStarterHub.CanUse(from,this)){if(Service==1||Service==9||Service==10)HavenSupplyShops.Show(from,Service==1?0:Service==9?1:2);else if(Service==3)from.SendGump(new PreviewGump());else from.SendGump(new HavenServiceMenu(this));} else from.SendMessage("Stand within three tiles of the service stone."); }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(Service); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); Service=reader.ReadInt(); if(Service==3)Name="Travel stone"; }
    }
    public class HavenPlazaPlanter:Item {
        public Item Stone{get;private set;}
        public HavenPlazaPlanter(Item stone):base(0x11CA){Stone=stone;Name="Haven plaza flowers";Movable=false;}
        public HavenPlazaPlanter(Serial serial):base(serial){}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Stone);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Stone=r.ReadItem();}
    }
    public class HavenServicePost : Item {
        public HavenServiceStone Board {get;private set;}
        public HavenServicePost(HavenServiceStone board):base(0xB98){Board=board;Movable=false;Name=board.Name;}
        public HavenServicePost(Serial serial):base(serial){}
        public override void OnDoubleClick(Mobile p){if(Board!=null&&!Board.Deleted)Board.OnDoubleClick(p);}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Board);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Board=r.ReadItem() as HavenServiceStone;if(Board!=null&&Board.Service==3)Name="Travel stone";}
    }
    public class HavenServiceMenu : HavenStoneGump {
        private readonly HavenServiceStone _board;
        public HavenServiceMenu(HavenServiceStone board):base(50,50){
            _board=board;AddBackground(0,0,380,350,0xA28);
            AddLabel(24,22,0,board.Name);int y=65;
            foreach(int service in Services(board.Service)){
                string label=service==7?"Healers and recovery":service==8?"Evolving gear and upgrades":service==9?"Training supplies":service==10?"Special rewards":HavenStarterHub.Names[service];
                FlatButton(24,y,332,service+1,label);y+=36;
            }
            FlatButton(256,310,100,0,"Close");
        }
        public static int[] Services(int group){return group==0?new[]{0,1,8,9,10,4}:group==3?new[]{3}:new[]{2,6,7};}
        public override void OnResponse(NetState sender,RelayInfo info){int service=info.ButtonID-1;if(!HavenStarterHub.CanUse(sender.Mobile,_board)||!Services(_board.Service).Contains(service))return;if(service==1||service==9||service==10){HavenSupplyShops.Show(sender.Mobile,service==1?0:service==9?1:2);return;}if(service==7)sender.Mobile.SendGump(new HavenRecoveryGump());else if(service==8)sender.Mobile.SendGump(new HavenStarterGearGump(sender.Mobile));else sender.Mobile.SendGump(new HavenHubGump(_board,service));}
    }
    public class HavenHubGump : HavenStoneGump
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
                "Open travel to New Haven, Luna, Royal City, Underworld, Doom, Blackthorn, Shadowguard and the Abyss. Travel unlocks 15 seconds after your last attack.",
                "Free preview repair: restores current durability on weapons and armor you wear or carry. Does not increase maximum durability or add properties.",
                "OPTIONAL TEST BOOST: the next screen offers a one-time test kit that sets all skills to 120 and stats to 100 each. Skip this stone if you want to train a new character normally. Opening the menu alone changes nothing.",
                "The plaza offers starter supplies, arcane supplies, companion recruitment, dungeon travel, repairs and optional test training.<BR><BR>Use [c for your companion, [home for starter housing and [havenmarks for rewards. Completed companion missions earn 2 Haven Marks per minute. Preview each reward before buying it.<BR><BR>Native vendors, bankers, healers and trainers remain available. Use [minichamp for three-wave expeditions and a boss, with rewards for every damage participant. The evolving gear stone offers starter equipment and robe upgrades. Use [wallet for gold storage and tithing; [havenluck shows the restored area bonus. Custom pets, the market and home island remain upcoming. Original-server progress is separate."};
            AddHtml(24,58,520,240,"<BASEFONT COLOR=#202020>"+details[service]+"</BASEFONT>",false,true);
            if(stone!=null && service!=6) { FlatButton(24,362,215,1,service<=1?"Claim supplies":service==4?"Repair carried equipment":"Open service"); }
            if(service==0) {FlatButton(265,280,275,7,"Free Champion's Codex");FlatButton(24,315,215,5,"Get a free trash bag");FlatButton(265,315,275,6,"Open wallet");}
            if(service==6) {FlatButton(24,315,215,4,"Healers and corpse recovery");FlatButton(24,362,215,2,"Browse Haven rewards");FlatButton(245,362,180,3,"Mini champion");}
            FlatButton(440,362,100,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var from=sender.Mobile;
            if(info.ButtonID==7 && _service==0 && HavenStarterHub.CanUse(from,_stone)){HavenChampionCodex.OpenCodex(from);return;}
            if(info.ButtonID==6 && _service==0 && HavenStarterHub.CanUse(from,_stone)){HavenWallet.Open(from);return;}
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


