using ModernUO.Serialization;
using Server.Commands;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenStarterSash : BodySash
{
    [Constructible]
    public HavenStarterSash()
    {
        Name = "Haven adventurer's evolving sash";
        Hue = 0x59B;
        LootType = LootType.Blessed;
        Attributes.BonusStr = Attributes.BonusDex = Attributes.BonusInt = 1;
        Attributes.RegenHits = Attributes.RegenStam = Attributes.RegenMana = 1;
        HavenGearExperience.Gain(this, 1);
    }
    public static void Initialize() => CommandSystem.Register("Sash", AccessLevel.Player, e => Claim(e.Mobile));
    public static void Claim(Mobile from)
    {
        if (!from.Player || from.Backpack == null) { return; }
        if (from.FindItemOnLayer(Layer.MiddleTorso) is HavenStarterSash ||
            from.Backpack.FindItemByType<HavenStarterSash>() != null ||
            from.FindBankNoCreate()?.FindItemByType<HavenStarterSash>() != null)
        { from.SendMessage("You already have your starter sash."); return; }
        var sash = new HavenStarterSash();
        if (!from.Backpack.TryDropItem(from, sash, false)) { sash.Delete(); from.SendMessage("Make room in your backpack."); return; }
        from.SendMessage("Your free evolving sash is in your backpack. Wear it to gain shared experience.");
    }
}
