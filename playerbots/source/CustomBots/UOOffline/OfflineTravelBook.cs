using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Spells;

namespace Server.UOOffline;

// Same portable moongate behavior as the earlier modern-evolution travel book.
[SerializationGenerator(0)]
public partial class OfflineTravelBook : Item
{
    public override string DefaultName => "Blessed Travel Book";

    [Constructible]
    public OfflineTravelBook() : base(0x22C5)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from?.Backpack == null || !IsChildOf(from.Backpack))
        {
            from?.SendMessage("Keep the Travel Book in your backpack to use it.");
            return;
        }
        if (!from.CheckAlive()) { return; }
        if (from.Criminal || SpellHelper.CheckCombat(from) || from.Spell != null)
        {
            from.SendMessage("You cannot use the Travel Book while criminal, in combat or casting.");
            return;
        }
        from.CloseGump<HavenTravelGump>();
        from.SendGump(new HavenTravelGump(this));
    }
}
