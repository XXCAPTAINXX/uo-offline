using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
namespace Server.Spells.SkillMasteries;
[SerializationGenerator(0)]
public partial class MasteryProgress : Item
{
    [SerializableField(0)] private Dictionary<SkillName, int> _levels = new();
    [SerializableField(1)] private SkillName _selected;
    [SerializableField(2)] private DateTime _nextSwitch;
    public override bool IsVirtualItem => true;
    [Constructible]
    public MasteryProgress() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "mastery progression"; }
    public static MasteryProgress Find(Mobile owner) => owner?.FindBankNoCreate()?.FindItemByType<MasteryProgress>();
    public static SkillName Current(Mobile owner) => Find(owner)?.Selected ?? SkillName.Alchemy;
    public static MasteryProgress Get(Mobile owner)
    {
        var bank = owner.BankBox;
        var record = bank.FindItemByType<MasteryProgress>();
        if (record == null) { record = new MasteryProgress(); bank.DropItem(record); }
        return record;
    }
    public int Level(SkillName skill) => _levels.TryGetValue(skill, out var level) ? level : 0;
    public bool Learn(SkillName skill, int volume)
    {
        if (volume is < 1 or > 3 || !Array.Exists(MasteryInfo.Skills, s => s == skill) || volume <= Level(skill)) { return false; }
        _levels[skill] = volume; this.MarkDirty(); return true;
    }
}
public enum DamageType { Melee, Ranged, Spell, SpellAOE }
public enum SAAbsorptionAttribute { CastingFocus }
public class MasteryBuffInfo : Server.Engines.BuffIcons.BuffInfo
{
    public MasteryBuffInfo(Server.Engines.BuffIcons.BuffIcon icon, int title, int description, string args = null, bool retain = false)
        : base(icon, title, description, default, args, retain) { }
    public MasteryBuffInfo(Server.Engines.BuffIcons.BuffIcon icon, int title, int description, TimeSpan duration, Mobile owner, string args = null, bool retain = false)
        : base(icon, title, description, duration, args, retain) { }
    public static void AddBuff(Mobile owner, MasteryBuffInfo buff) { if (owner is PlayerMobile player) { player.AddBuff(buff); } }
    public static void RemoveBuff(Mobile owner, Server.Engines.BuffIcons.BuffIcon icon) { if (owner is PlayerMobile player) { player.RemoveBuff(icon); } }
}
