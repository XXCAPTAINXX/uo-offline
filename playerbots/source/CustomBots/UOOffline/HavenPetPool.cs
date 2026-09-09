using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPetPool : BaseAddon
{
    [SerializableField(0)] private BaseCreature _pet;
    [SerializableField(1)] private Mobile _owner;
    private Timer _timer;
    private int _ticks;
    public override BaseAddonDeed Deed => null;
    internal static int Theme(BaseCreature pet) => pet switch
    {
        HavenEmberwing => 1, HavenFrostmane => 2, HavenVerdantLlama => 3,
        HavenMoonfang or HavenStormhorn or HavenStormscale => 4, _ => 0
    };
    [Constructible]
    public HavenPetPool(BaseCreature pet = null, Mobile owner = null)
    {
        Pet = pet; Owner = owner; Movable = false;
        var theme = Theme(pet);
        Name = theme switch { 1 => "ember pool", 2 => "frost pool", 3 => "venom pool", 4 => "storm pool", _ => "blood pool" };
        var hue = theme switch { 1 => 0x489, 2 => 0x47F, 3 => 0x59B, 4 => 0x482, _ => 0x485 };
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            { AddComponent(new AddonComponent(0x122A) { Hue = hue, Name = Name }, x, y, 0); }
        }
    }
    internal void Start() => _timer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Tick);
    [AfterDeserialization]
    private void ExpireOnLoad() => Timer.DelayCall(Delete);
    internal void Tick()
    {
        if (Deleted) { return; }
        if (Pet?.Deleted != false || Owner?.Deleted != false || !Pet.Alive || Pet.IsDeadPet || !Owner.Alive ||
            !Pet.Controlled || Pet.ControlMaster != Owner || Pet.Map != Map || Owner.Map != Map || !Owner.InRange(this, 18)) { Delete(); return; }
        var theme = Theme(Pet);
        var damage = 8 + Math.Clamp(Pet.Backpack?.FindItemByType<HavenPetRarity>()?.Tier ?? 0, 0, 3) * 4;
        using var targets = PooledRefList<BaseCreature>.Create();
        foreach (var enemy in Map.GetMobilesInRange<BaseCreature>(Location, 1)) { targets.Add(enemy); }
        foreach (var enemy in targets)
        {
            if (enemy.Deleted || !enemy.Alive || enemy == Pet || enemy.Controlled || enemy.Summoned || enemy is BaseVendor ||
                Math.Abs(enemy.Z - Z) > 16 || !Pet.InLOS(enemy) || !Pet.CanBeHarmful(enemy, false) ||
                !Owner.CanBeHarmful(enemy, false) || !SpellHelper.ValidIndirectTarget(Pet, enemy)) { continue; }
            Pet.DoHarmful(enemy);
            AOS.Damage(enemy, Pet, damage, theme == 0 ? 100 : 0, theme == 1 ? 100 : 0, theme == 2 ? 100 : 0, theme == 3 ? 100 : 0, theme == 4 ? 100 : 0);
        }
        if (++_ticks >= 6) { Delete(); }
    }
    public override void OnAfterDelete()
    {
        _timer?.Stop(); _timer = null;
        HavenPetAbilities.Find(Pet)?.ClearPool(this);
        Pet = null; Owner = null; base.OnAfterDelete();
    }
}
