using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;

public static class JewelrySmoke
{
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); File.AppendAllText("jewelry-checks.log", "PASS " + label + "\n"); }
    public static void Initialize() { if (File.Exists("JEWELRY-TEST-ONLY")) EventSink.ServerStarted += () => Timer.DelayCall(TimeSpan.FromSeconds(4), Run); }
    static void Run()
    {
        try {
            var saved = World.Mobiles.Values.OfType<PlayerMobile>().FirstOrDefault(candidate => candidate.Name == "Jewelry reload fixture");
            if (saved != null) {
                var talisman = saved.FindItemOnLayer(Layer.Talisman) as HavenConcordTalisman;
                Check(talisman != null && HavenEquipmentEvolution.Find(talisman).Level == 20, "talisman identity and level survive reload");
                Check(saved.FollowersMax == 6 && talisman.Attributes.Luck == 395, "permanent capacity and growth survive reload");
                var ring = saved.FindItemOnLayer(Layer.Ring) as HavenSetRing;
                Check(ring != null && ring.Theme == 6 && HavenJewelrySets.GetBonus(saved, AosAttribute.Luck) == 600, "ring theme and set bonus survive reload");
                File.AppendAllText("jewelry-checks.log", "RELOAD COMPLETE\n"); Core.Kill(false); return;
            }
            var p = new PlayerMobile { Name = "Jewelry reload fixture", Player = true, Body = 0x190, RawStr = 100 };
            p.AddItem(new Backpack()); p.MoveToWorld(new Point3D(1015,527,-65), Map.Malas);
            for (int theme = 0; theme < 9; theme++) {
                var ring = new HavenSetRing(theme); var bracelet = HavenJewelrySets.CreateBracelet(theme);
                p.AddItem(ring); p.AddItem(bracelet);
                foreach (AosAttribute attr in Enum.GetValues(typeof(AosAttribute))) {
                    int paired = AosAttributes.GetValue(p, attr); int bonus = HavenJewelrySets.GetBonus(p, attr);
                    p.RemoveItem(ring); int alone = AosAttributes.GetValue(p, attr);
                    Check(paired == alone + ring.Attributes[attr] + bonus, "native attribute hook theme " + theme + " " + attr);
                    Check(HavenJewelrySets.GetBonus(p, attr) == 0, "removal disables set bonus"); p.AddItem(ring);
                }
                ring.Delete(); bracelet.Delete();
            }
            var fortune = new HavenSetRing(6); p.AddItem(fortune); p.AddItem(new BraceletOfFortune());
            var concord = new HavenConcordTalisman(); p.AddItem(concord);
            Check(HavenJewelrySets.GetBonus(p, AosAttribute.Luck) == 600, "matching Fortune and Concord bonus");
            var progress = HavenEquipmentEvolution.Attach(concord, 4); progress.Gain(1800);
            Check(p.FollowersMax == 5, "level19 cannot unlock follower"); progress.Gain(100);
            Check(p.FollowersMax == 6 && concord.Attributes.Luck == 395 && concord.Attributes.BonusStr == 14, "original level20 growth and follower unlock");
            concord.OnDoubleClick(p); progress.Apply(); Check(p.FollowersMax == 6 && concord.Attributes.Luck == 395, "unlock and growth do not stack");
            var ringProgress = HavenEquipmentEvolution.Attach(fortune, 4); ringProgress.Gain(1900);
            Check(fortune.Attributes.Luck == 245, "ring preserves original base plus growth");
            Check(HavenSupplyShops.Catalogs[2].Count(x => x.Name.EndsWith(" ring")) == 9, "all nine rings offered");
            Check(HavenSupplyShops.Catalogs[2].Single(x => x.Name == "Concord talisman").Marks == 150, "original talisman price");
            World.Save(); File.AppendAllText("jewelry-checks.log", "FRESH COMPLETE\n"); Core.Kill(false);
        } catch (Exception e) { File.AppendAllText("jewelry-checks.log", "FAIL " + e + "\n"); Core.Kill(false); }
    }
}
