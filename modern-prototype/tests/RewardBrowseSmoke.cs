using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.HavenPrototype;

public static class RewardBrowseSmoke
{
    public static void Run(Mobile owner, Action<string> report)
    {
        var catalog = HavenSupplyShops.Catalogs[2];
        var all = HavenSupplyShops.Browse(2, -1);
        if (all.Count != catalog.Count || all.Distinct().Count() != catalog.Count) throw new Exception("Reward missing or duplicated");
        var weapons = HavenSupplyShops.Browse(2, 0);
        var keys = weapons.Select(i => HavenSupplyShops.WeaponFamily(catalog[i])).ToArray();
        if (!keys.SequenceEqual(keys.OrderBy(k => k))) throw new Exception("Weapon families interleaved");
        for (int family = 0; family < 4; family++)
        {
            var filtered = HavenSupplyShops.Browse(2, 0, family);
            if (filtered.Count == 0 || filtered.Any(i => catalog[i].Group != 0 || HavenSupplyShops.WeaponFamily(catalog[i]) != family)) throw new Exception("Incorrect weapon filter");
            var menu = new HavenSupplyShopGump(owner, 2, -1, group: 0, weaponFamily: family);
            try
            {
                if (menu.Entries.OfType<GumpButton>().Any(b => b.NormalID != 2445 && b.NormalID != 2446)) throw new Exception("Unexpected non-stone button");
                foreach (var label in menu.Entries.OfType<GumpLabel>()) if (label.Text.Contains("&amp;") || label.Text.Contains("&apos;")) throw new Exception("Escaped label displayed literally");
                var selection = menu.Entries.OfType<GumpButton>().Where(b => b.ButtonID >= 100).Select(b => b.ButtonID - 100).ToArray();
                if (!selection.SequenceEqual(filtered.Take(8))) throw new Exception("Selection no longer maps to catalog identity");
            }
            finally { menu.OnServerClose(null); }
        }
        if (!HavenSupplyShops.Browse(2, 0, 0).Any(i => catalog[i].Name == "Cyclone Scimitar") || !HavenSupplyShops.Browse(2, 0, 0).Any(i => catalog[i].Name == "Cyclone Kryss")) throw new Exception("Blades not grouped together");
        report("PASS all rewards retained; weapon families grouped and filtered; native stone buttons; plain labels; purchase catalog IDs preserved");
    }
}
