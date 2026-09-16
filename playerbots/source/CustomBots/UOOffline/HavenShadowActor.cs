using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenShadowActor : BaseCreature
{
    [SerializableField(0)] private HavenShadowChamber _chamber;
    [SerializableField(1)] private int _role;
    [SerializableField(2)] private int _puzzleHits;
    [SerializableField(3)] private int _element;
    private DateTime _nextPower;
    [Constructible]
    public HavenShadowActor() : base(AIType.AI_Melee, FightMode.Closest) { }
    public override bool BardImmune => Role == 13;
    public override bool AlwaysAttackable => true;
    public override bool AutoDispel => Role is >= 10 and <= 13;
    public override bool CanRummageCorpses => false;
    internal void ConfigureRole(int partySize)
    {
        Name = Role switch
        {
            0 => "a drunken Shadowguard pirate", 1 => "an ensorcelled guard", 2 => "an enchanted armor",
            3 => "a vile water elemental", 4 => "a vile drake", 5 => "the belfry dragon",
            10 => "Juo'nar", 11 => "Ozymandias", 12 => "Anon", 13 => "Virtuebane",
            _ => Chamber?.Stage == 0 ? "a rift skeleton" : Chamber?.Stage == 1 ? "a rift ninja" : Chamber?.Stage == 2 ? "a rift wisp" : "a rift daemon"
        };
        Body = Role switch { 3 => 16, 4 => 60, 5 => 59, 10 => 24, 13 => 9,
            >= 20 => Chamber?.Stage switch { 1 => 400, 2 => 58, 3 => 9, _ => 50 }, _ => 400 };
        BaseSoundID = Role switch { 3 => 0x119, 4 or 5 => 362, 10 => 0x3E9, 13 => 357, _ => 0x165 };
        var boss = Role is 5 or >= 10 and <= 13;
        SetStr(boss ? 600 : 150); SetDex(boss ? 150 : 100); SetInt(boss ? 600 : 100);
        SetHits((boss ? 4500 : 300) * Math.Clamp(partySize + 1, 2, 6) / 2); SetDamage(boss ? 16 : 6, boss ? 23 : 10);
        SetResistance(ResistanceType.Physical, boss ? 60 : 30); SetResistance(ResistanceType.Fire, boss ? 60 : 30);
        SetResistance(ResistanceType.Cold, boss ? 55 : 25); SetResistance(ResistanceType.Poison, boss ? 65 : 35);
        SetResistance(ResistanceType.Energy, boss ? 55 : 25); SetSkill(SkillName.Wrestling, boss ? 110 : 80);
        SetSkill(SkillName.Tactics, boss ? 110 : 80); SetSkill(SkillName.MagicResist, boss ? 110 : 80);
        if (Role is 10 or 12 or 13)
        { AI = AIType.AI_Mage; SetSkill(SkillName.Magery, 110); SetSkill(SkillName.EvalInt, 110); SetSkill(SkillName.Meditation, 110); }
        if (Role == 11)
        { AI = AIType.AI_Archer; SetSkill(SkillName.Archery, 115); AddItem(new Bow()); PackItem(new Arrow(1000)); }
        Blessed = Role is 0 or 2; Tamable = false; Fame = boss ? 18000 : 1500; Karma = -Fame;
        if (Body == 400)
        {
            if (Role is 1 or 2)
            {
                AddItem(new PlateChest { Movable = false }); AddItem(new PlateArms { Movable = false });
                AddItem(new PlateLegs { Movable = false }); AddItem(new PlateGloves { Movable = false });
                AddItem(new PlateGorget { Movable = false }); AddItem(new PlateHelm { Movable = false });
            }
            else { AddItem(new Shirt(0x455) { Movable = false }); AddItem(new ShortPants(0x59) { Movable = false }); AddItem(new Boots { Movable = false }); }
        }
        _nextPower = Core.Now + TimeSpan.FromSeconds(12);
    }
    public override void GenerateLoot() { AddLoot(Role is >= 10 and <= 13 or 5 ? LootPack.FilthyRich : LootPack.Average); }
    public override void OnThink()
    {
        base.OnThink();
        if (Chamber?.Active != true) { return; }
        if (!Chamber.Inside(this)) { Combatant = null; MoveToWorld(Home, Chamber.Map); }
        if (Combatant is Mobile target && !Chamber.Participant(target)) { Combatant = null; }
        if (Role is < 10 or > 13 || Core.Now < _nextPower || Combatant is not Mobile enemy || !Chamber.Participant(enemy)) { return; }
        _nextPower = Core.Now + TimeSpan.FromSeconds(15);
        if (Role == 12)
        {
            Element = (Element + 1) % 4; Body = Element switch { 0 => 14, 1 => 15, 2 => 16, _ => 13 };
            Say(Element switch { 0 => "Stone devours your physical blows!", 1 => "Fire feeds my strength!", 2 => "Cold restores me!", _ => "Your energy is mine!" });
        }
        if (Role is 11 or 13 && enemy is PlayerMobile player && player.Mount is BaseMount mount)
        { mount.Rider = null; player.SendMessage("The lieutenant knocks you from your mount!"); }
        if (Role == 10)
        { AOS.Damage(enemy, this, 18, 0, 0, 100, 0, 0); }
        if (Role == 13 && InRange(enemy, 4))
        { AOS.Damage(enemy, this, 22, 0, 100, 0, 0, 0); enemy.FixedEffect(0x36BD, 10, 12); }
        if (Chamber.Actors.Count < 7)
        { var add = Chamber.Spawn(20, Utility.RandomMinMax(-8, 8), Utility.RandomMinMax(-6, 6)); add.Combatant = enemy; }
    }
    public static bool Absorb(Mobile victim, Mobile attacker, int damage, bool ignoreArmor, int physical, int fire, int cold, int poison, int energy, int direct)
    {
        if (victim is not HavenShadowActor { Role: 12 } anon || ignoreArmor || direct > 0 || damage <= 0 ||
            anon.Chamber?.Participant(attacker) != true || !anon.Alive || anon.Deleted) { return false; }
        var matching = anon.Element switch
        {
            0 => physical > 0 && fire + cold + poison + energy == 0,
            1 => fire > 0 && physical + cold + poison + energy == 0,
            2 => cold > 0 && physical + fire + poison + energy == 0,
            _ => energy > 0 && physical + fire + cold + poison == 0
        };
        if (!matching) { return false; }
        anon.Hits = Math.Min(anon.HitsMax, anon.Hits + damage); anon.FixedEffect(0x376A, 10, 12); return true;
    }
    public override void OnDeath(Container corpse)
    {
        Chamber?.Killed(this); base.OnDeath(corpse);
        Timer.DelayCall(TimeSpan.FromSeconds(Role >= 10 ? 180 : 45), () => { if (!corpse.Deleted) { corpse.Delete(); } });
    }
    public override void OnDelete() { Chamber = null; base.OnDelete(); }
}
