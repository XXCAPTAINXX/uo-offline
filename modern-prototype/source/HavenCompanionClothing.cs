using System;
using System.Linq;
using Server.Items;

namespace Server.HavenPrototype
{
    public static class HavenCompanionClothing
    {
        public const int EvolutionKind = 5;

        public static void Ensure(HavenCompanion companion)
        {
            foreach (var garment in companion.Items.OfType<BaseClothing>().ToArray())
                HavenEquipmentEvolution.Attach(garment, EvolutionKind);
        }

        public static void Apply(BaseClothing garment, int level)
        {
            int step = Math.Max(0, Math.Min(19, level - 1));
            var a = garment.Attributes;
            a.SpellDamage = Math.Max(a.SpellDamage, 10 + step * 15 / 19);
            a.BonusHits = Math.Max(a.BonusHits, 5 + step * 5 / 19);
            a.BonusMana = Math.Max(a.BonusMana, 8 + step * 12 / 19);
            a.RegenHits = Math.Max(a.RegenHits, 1 + step * 2 / 19);
            a.RegenMana = Math.Max(a.RegenMana, 2 + step * 2 / 19);
            a.LowerManaCost = Math.Max(a.LowerManaCost, 3 + step * 2 / 19);
            a.WeaponDamage = Math.Max(a.WeaponDamage, 5 + step * 5 / 19);
            string name = garment.Name ?? "Companion's " + garment.GetType().Name;
            int suffix = name.LastIndexOf(" [level ", StringComparison.Ordinal);
            if (suffix >= 0) name = name.Substring(0, suffix);
            garment.Name = name + " [level " + level + "/20]";
            garment.InvalidateProperties();
        }
    }
}
