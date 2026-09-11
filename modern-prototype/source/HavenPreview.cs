using System;
using System.Collections.Generic;
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
            new Destination("Abyss - Silver Sapling", Map.TerMur, 341,619,26)
        };
        public static void Initialize()
        {
            CommandSystem.Register("preview", AccessLevel.Player, e => { if (Enabled) e.Mobile.SendGump(new PreviewGump()); });
            EventSink.Login += e => { if (Enabled) e.Mobile.SendMessage("Haven modern preview: use [preview for test preparation and travel; [c for your companion."); };
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
                from.Combatant == null && from.Aggressors.Count == 0 && from.Aggressed.Count == 0 && !from.Criminal;
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
    public class PreviewGump : Gump
    {
        public PreviewGump() : base(50,50)
        {
            AddBackground(0,0,590,570,0xA28);
            AddLabel(24,20,0,"Haven - modern test world");
            AddHtml(24,50,540,64,"<BASEFONT COLOR=#202020>A separate fresh-world preview. Your existing character, island and possessions remain on the original server.</BASEFONT>",false,false);
            Button(24,122,1,"Prepare test character (once)");
            Button(320,122,2,"Open companion");
            AddLabel(24,165,0,"Test travel - leave combat first; Felucca has PvP rules");
            for (int i = 0; i < HavenPreview.Destinations.Length; ++i)
                Button(24 + (i % 2) * 280,205 + (i / 2) * 43,100 + i,HavenPreview.Destinations[i].Name);
            AddHtml(24,430,540,75,"<BASEFONT COLOR=#202020>Try companion orders, resource missions and native dungeons. The older custom island, bots and market are not migrated yet. Test skills and gear are conveniences, not final balance.</BASEFONT>",false,false);
            Button(430,526,0,"Close");
        }
        private void Button(int x,int y,int id,string label) { AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0); AddLabel(x+34,y,0,label); }
        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (!HavenPreview.Enabled || sender.Mobile == null || info.ButtonID == 0) return;
            var from = sender.Mobile;
            if (info.ButtonID == 1) HavenPreview.Prepare(from);
            else if (info.ButtonID == 2) { var companion = HavenCompanion.Claim(from); if (companion != null) companion.Show(from); return; }
            else if (!HavenPreview.Travel(from, info.ButtonID - 100)) from.SendMessage("Travel unavailable: leave combat, clear criminal status, and try again.");
            from.SendGump(new PreviewGump());
        }
    }
}


