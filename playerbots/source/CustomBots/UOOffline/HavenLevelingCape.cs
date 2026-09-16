using System;
using ModernUO.Serialization;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenLevelingCape : Cloak
{
    [SerializableField(0)]
    private Mobile _boundTo;
    [SerializableField(1)]
    private long _experience;
    public int Level => Math.Min(20, 1 + (int)Math.Sqrt(Math.Max(0, Experience) / 25.0));
    public static void Initialize() => CommandSystem.Register("Cape", AccessLevel.Player, e => Claim(e.Mobile));
    [Constructible]
    public HavenLevelingCape()
    {
        Name = "Haven adventurer's leveling cape"; Hue = 0x59B; LootType = LootType.Blessed;
        ApplyLevel();
    }
    public override bool CanEquip(Mobile from) => (BoundTo == null || BoundTo == from) && base.CanEquip(from);
    internal void ApplyLevel()
    {
        Attributes.Luck = Level * 25;
        Attributes.RegenHits = Attributes.RegenStam = Attributes.RegenMana = 1 + Level / 5;
        Attributes.LowerManaCost = Attributes.DefendChance = Level / 2;
        Attributes.BonusStr = Attributes.BonusDex = Attributes.BonusInt = 1 + Level / 4;
        InvalidateProperties();
    }
    internal void GainExperience(Mobile owner, int amount)
    {
        if (owner != BoundTo || Parent != owner || amount <= 0 || Level >= 20) { return; }
        var old = Level;
        Experience += amount;
        ApplyLevel();
        if (Level > old) { owner.SendMessage(0x59B, $"Your leveling cape reached level {Level}!"); }
    }
    public static void OnMonsterKilled(BaseCreature creature, Mobile player)
    {
        if (HavenAstralRewards.Eligible(creature, player) && player.FindItemOnLayer(Layer.Cloak) is HavenLevelingCape cape)
        {
            cape.GainExperience(player, Math.Clamp(creature.HitsMax / 100, 1, 20));
        }
    }
    public static void Claim(Mobile from)
    {
        if (!from.Player || from.Backpack == null) { return; }
        if (from.FindItemOnLayer(Layer.Cloak) is HavenLevelingCape || from.Backpack.FindItemByType<HavenLevelingCape>() != null ||
            from.FindBankNoCreate()?.FindItemByType<HavenLevelingCape>() != null)
        {
            from.SendMessage("You already have a leveling cape in your equipment, pack or bank."); return;
        }
        var cape = new HavenLevelingCape { BoundTo = from };
        if (!from.Backpack.TryDropItem(from, cape, false)) { cape.Delete(); from.SendMessage("Make room in your pack, then claim your free cape again."); return; }
        from.SendMessage("Your free leveling cape is in your pack. Wear it to earn experience from credited monster kills.");
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Cape level:"} {Level}/20");
        list.Add($"{"Experience:"} {Experience:N0} / {(Level >= 20 ? Experience : Level * Level * 25):N0}");
        if (BoundTo != null) { list.Add($"{"Bound to:"} {BoundTo.Name}"); }
    }
    public override void OnDelete() { BoundTo = null; base.OnDelete(); }
}
