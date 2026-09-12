using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.HavenPrototype
{
    // Explicit opt-in conveniences for the separate preview, never production defaults.
    public static class HavenPreview
    {
        public static bool Enabled { get { return File.Exists("HAVEN-INTERACTIVE-PREVIEW"); } }
        public sealed class Destination
        {
            public string Name; public Map Map; public Point3D Point;
            public Destination(string name, Map map, int x, int y, int z) { Name = name; Map = map; Point = new Point3D(x,y,z); }
        }
        public static readonly Destination[] Destinations = {
            new Destination("New Haven", Map.Trammel, 3506,2570,14),
            new Destination("Luna bank", Map.Malas, 991,519,-50),
            new Destination("Royal City", Map.TerMur, 855,3526,-43),
            new Destination("Underworld entrance", Map.TerMur, 1128,1211,-2),
            new Destination("Doom entrance", Map.Malas, 2367,1268,-85),
            new Destination("Doom Gauntlet", Map.Malas, 433,331,-2),
            new Destination("Blackthorn - Trammel", Map.Trammel, 6432,2677,0),
            new Destination("Blackthorn - Felucca", Map.Felucca, 6441,2677,20),
            new Destination("Shadowguard lobby", Map.TerMur, 505,2192,25),
            new Destination("Abyss - Silver Sapling", Map.TerMur, 341,619,26),
            new Destination("Britain moongate",Map.Trammel,1336,1997,5),
            new Destination("Moonglow moongate",Map.Trammel,4467,1283,5),
            new Destination("Yew moongate",Map.Trammel,771,752,5),
            new Destination("Minoc moongate",Map.Trammel,2701,692,5),
            new Destination("Trinsic moongate",Map.Trammel,1828,2948,-20),
            new Destination("Skara Brae moongate",Map.Trammel,643,2067,5),
            new Destination("Jhelom moongate",Map.Trammel,1499,3771,5),
            new Destination("Umbra moongate",Map.Malas,1997,1386,-85),
            new Destination("Makoto-Jima",Map.Tokuno,802,1204,25),
            new Destination("Ilshenar Compassion",Map.Ilshenar,1215,467,-13),
            new Destination("Covetous entrance",Map.Trammel,2499,919,0),
            new Destination("Deceit entrance",Map.Trammel,4111,432,5),
            new Destination("Despise entrance",Map.Trammel,1298,1080,0),
            new Destination("Destard entrance",Map.Trammel,1176,2637,0),
            new Destination("Hythloth entrance",Map.Trammel,4721,3822,0),
            new Destination("Shame entrance",Map.Trammel,514,1561,0),
            new Destination("Wrong entrance",Map.Trammel,2043,238,10),
            new Destination("Isamu-Jima",Map.Tokuno,1169,998,41),
            new Destination("Homare-Jima",Map.Tokuno,270,628,15),
            new Destination("Ilshenar Spirituality",Map.Ilshenar,1532,1340,-3),
            new Destination("Frostbound bear den",Map.Tokuno,942,116,Map.Tokuno.GetAverageZ(942,116)),
            new Destination("Ancient Hunt",Map.TerMur,527,758,-92),
            new Destination("Chelonia sanctuary",Map.Trammel,4094,3475,0),
            new Destination("Corsair island estate",Map.Trammel,4196,2886,0),
            new Destination("Island community center",Map.Trammel,3984,2897,0),
            HavenWardenPost.Arrival
        };
        public static void Initialize()
        {
            CommandSystem.Register("preview", AccessLevel.Player, e => { if (Enabled) e.Mobile.SendGump(new PreviewGump()); });
            EventSink.Login += e => { if (Enabled) e.Mobile.SendMessage("Welcome to Haven: use [preview for travel; [c for your companion."); };
            EventSink.ServerStarted += () => {
                if (Enabled) Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckLocalSaveRequest);
            };
        }
        private static void CheckLocalSaveRequest()
        {
            if (!Enabled) return;
            bool stop = File.Exists("PREVIEW-SAVE-AND-STOP");
            if (!stop && !File.Exists("PREVIEW-SAVE")) return;
            World.Save(false, false);
            if (File.Exists("PREVIEW-SAVE")) File.Delete("PREVIEW-SAVE");
            if (stop) { File.Delete("PREVIEW-SAVE-AND-STOP"); Core.Kill(false); }
        }
        public static bool CanTravel(Mobile from)
        {
            return Enabled && from != null && from.Player && !from.Deleted && from.Alive && from.Map != Map.Internal &&
                TravelCombatSeconds(from) == 0;
        }
        // Travel cooldown is independent of criminal status and native aggression expiry.
        public static int TravelCombatSeconds(Mobile from)
        {
            if (from == null) return 0;
            var now = DateTime.UtcNow;
            return from.Aggressors.Concat(from.Aggressed)
                .Where(a => !a.Expired)
                .Select(a => Math.Max(0, (int)Math.Ceiling((a.LastCombatTime.AddSeconds(15) - now).TotalSeconds)))
                .DefaultIfEmpty(0).Max();
        }
        public static bool FindLanding(Destination destination, out Point3D landing)
        {
            for (int radius = 0; radius <= 3; ++radius)
                for (int dx = -radius; dx <= radius; ++dx)
                    for (int dy = -radius; dy <= radius; ++dy)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                        var point = new Point3D(destination.Point.X + dx, destination.Point.Y + dy, destination.Point.Z);
                        if (destination.Map.CanFit(point, 16, false, true)) { landing = point; return true; }
                    }
            landing = Point3D.Zero; return false;
        }
        public static bool Travel(Mobile from, int index)
        {
            Point3D landing;
            if (index >= 0 && index < Destinations.Length && (Destinations[index].Name == "Corsair island estate" || Destinations[index].Name == "Island community center") && !HavenIslandInstall.Installed) return false;
            if (!CanTravel(from) || index < 0 || index >= Destinations.Length || !FindLanding(Destinations[index], out landing)) return false;
            var destination = Destinations[index];
            BaseCreature.TeleportPets(from, landing, destination.Map);
            from.MoveToWorld(landing, destination.Map);
            return true;
        }
        public static bool Prepare(Mobile from)
        {
            var account = from == null ? null : from.Account as Account;
            if (!CanTravel(from) || account == null || from.Backpack == null) return false;
            string key = "HavenPreview.Kit:" + from.Serial.Value;
            if (account.GetTag(key) != null) { from.SendMessage("This character already received the preview kit."); return false; }
            var sword = new Broadsword(); sword.WeaponAttributes.HitLeechMana = 60; sword.Attributes.WeaponDamage = 40;
            var mace = new WarMace(); mace.WeaponAttributes.HitLeechMana = 60; mace.Attributes.WeaponDamage = 40;
            var robe = new Robe(); robe.Attributes.LowerRegCost = 100; robe.Attributes.LowerManaCost = 40; robe.Attributes.RegenMana = 10;
            var kit = new List<Item> { sword, mace, new MetalKiteShield(), robe, new Bow(), new Arrow(500), new Spellbook(ulong.MaxValue),
                new NecromancerSpellbook(ulong.MaxValue), new SpellweavingBook(ulong.MaxValue), new BookOfChivalry(ulong.MaxValue),
                new BookOfMasteries(), new Bandage(200), new BankCheck(100000), new HavenResourceLedger(),
                new SkillMasteryPrimer(SkillName.Parry, 3) };
            foreach (BaseArmor armor in new BaseArmor[] { new LeatherChest(), new LeatherLegs(), new LeatherArms(), new LeatherGloves(), new LeatherGorget(), new LeatherCap() })
            {
                armor.PhysicalBonus = armor.FireBonus = armor.ColdBonus = armor.PoisonBonus = armor.EnergyBonus = 10;
                armor.Attributes.RegenHits = 1; armor.Attributes.RegenStam = 1;
                armor.Name = "preview " + armor.GetType().Name;
                kit.Add(armor);
            }
            int extraItems = 0, extraWeight = 0;
            foreach (Item item in kit)
            {
                if (!from.Backpack.CheckHold(from, item, false, true, extraItems, extraWeight))
                {
                    foreach (Item unused in kit) unused.Delete();
                    from.SendMessage("Make room in your backpack, then try again. Nothing has been claimed."); return false;
                }
                extraItems += item.TotalItems + 1; extraWeight += item.TotalWeight + item.PileWeight;
            }
            foreach (Item item in kit) from.Backpack.DropItem(item);
            from.StatCap = 300; from.RawStr = 100; from.RawDex = 100; from.RawInt = 100;
            from.Skills.Cap = from.Skills.Length * 1200;
            for (int i = 0; i < from.Skills.Length; ++i) { from.Skills[i].Cap = 120; from.Skills[i].Base = 120; }
            from.Hits = from.HitsMax; from.Mana = from.ManaMax; from.Stam = from.StamMax;
            account.SetTag(key, "claimed");
            from.SendMessage("Preview kit ready. Equip your weapon, shield and robe. Read the Parry primer to learn its mastery.");
            return true;
        }
    }
    public class PreviewGump : HavenStoneGump
    {
        private readonly int _page;
        public PreviewGump(int page=0) : base(50,50)
        {
            _page=Math.Max(0,Math.Min((HavenPreview.Destinations.Length-1)/10,page));
            AddBackground(0,0,590,570,0xA28);
            AddLabel(24,20,0,"Travel stone");
            AddHtml(24,50,540,64,"<BASEFONT COLOR=#202020>Travel with nearby followers. Town stops use public moongates; dungeon stops use entrances. Hostile creatures may be nearby.</BASEFONT>",false,false);
            AddLabel(24,122,0,"Leave combat before traveling. Felucca has PvP rules.");
            for (int row = 0; row < 10; ++row) {int i=_page*10+row;if(i>=HavenPreview.Destinations.Length)break;
                Button(24 + (row % 2) * 280,165 + (row / 2) * 43,100 + i,HavenPreview.Destinations[i].Name); }
            AddHtml(24,430,540,75,"<BASEFONT COLOR=#202020>Pages: 1 - modern adventures; 2 - towns and gateways; 3 - dungeons and hunting; 4 - Warden. Trammel dungeon entrances use non-PvP rules.</BASEFONT>",false,false);
            if(_page>0)Button(24,526,3,"Previous");AddLabel(165,526,0,"Page "+(_page+1)+" / "+((HavenPreview.Destinations.Length+9)/10));if((_page+1)*10<HavenPreview.Destinations.Length)Button(280,526,4,"Next");Button(430,526,0,"Close");
        }
        private void Button(int x,int y,int id,string label) { FlatButton(x,y,id>=100?260:id==1?270:id==2?230:110,id,label); }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!HavenPreview.Enabled || sender.Mobile == null || info.ButtonID == 0) return;
            var from = sender.Mobile;
            if(info.ButtonID==3||info.ButtonID==4){from.SendGump(new PreviewGump(_page+(info.ButtonID==3?-1:1)));return;}
            if (info.ButtonID < 100 || info.ButtonID >= 100 + HavenPreview.Destinations.Length) return;
            if (!HavenPreview.Travel(from, info.ButtonID - 100)) from.SendMessage("Travel unavailable: leave combat and wait for recent combat to expire, then try again.");
            from.SendGump(new PreviewGump(_page));
        }
    }
}


