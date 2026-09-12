using System;
using System.Linq;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.HavenPrototype {

public sealed class HavenAnimalLoreGump : Gump
{
    private readonly BaseCreature _pet;
    private static readonly SkillName[] CombatSkills = new[]{SkillName.Wrestling, SkillName.Tactics, SkillName.MagicResist, SkillName.Anatomy, SkillName.Healing, SkillName.Poisoning, SkillName.Parry, SkillName.Hiding, SkillName.DetectHidden};
    private static readonly SkillName[] MagicSkills = new[]{SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.Necromancy, SkillName.SpiritSpeak, SkillName.Mysticism, SkillName.Focus, SkillName.Spellweaving, SkillName.Chivalry, SkillName.Bushido, SkillName.Ninjitsu, SkillName.Musicianship, SkillName.Discordance, SkillName.Peacemaking, SkillName.Provocation};
    public static void DisplayTo(Mobile from, BaseCreature pet)
    {
        if (from?.NetState == null || pet?.Deleted != false) { return; }


        from.CloseGump(typeof(HavenAnimalLoreGump));
        from.SendGump(new HavenAnimalLoreGump(from, pet));
    }
    internal HavenAnimalLoreGump(Mobile viewer, BaseCreature pet) : base(20, 20)
    {
        _pet = pet;
        var training = HavenLoreCompatibility.Training(pet);
        AddBackground(0, 0, 660, 790, 5054);
        AddImageTiled(12, 12, 636, 766, 2624);
        AddAlphaRegion(22, 128, 298, 596);
        AddAlphaRegion(330, 128, 308, 596);
        AddLabel(28, 24, 53, "ANIMAL LORE");
        AddHtml(28, 49, 280, 44, $"<BASEFONT COLOR=#FFFFFF>{HavenMenuText.Encode(pet.Name)}</BASEFONT>");
        AddLabel(28, 99, 1152, pet.IsBonded ? "Bonded" : pet.Map == Map.Internal ? "Stored pet / ticket preview" : pet.Controlled ? "Tamed - not bonded" : "Wild creature");
        Bar(350, 28, "Loyalty", pet.Loyalty, BaseCreature.MaxLoyalty, pet.Controlled);
        Bar(350, 75, "Training", training?.Progress ?? 0, 10000, training != null);
        AddLabel(28, 136, 53, "Attribute"); AddLabel(143, 136, 53, "Current / max"); AddLabel(259, 136, 53, "Regen");
        Vital(164, "Strength", pet.Str, "Health", pet.Hits, pet.HitsMax, (int)Server.Misc.RegenRates.HitPointRegen(pet), pet.CanRegenHits);
        Vital(225, "Dexterity", pet.Dex, "Stamina", pet.Stam, pet.StamMax, (int)Server.Misc.RegenRates.StamRegen(pet), pet.CanRegenStam);
        Vital(286, "Intelligence", pet.Int, "Mana", pet.Mana, pet.ManaMax, (int)Server.Misc.RegenRates.ManaRegen(pet), pet.CanRegenMana);
        AddLabel(28, 350, 53, "Element"); AddLabel(153, 350, 53, "Resist"); AddLabel(246, 350, 53, "Damage");
        string[] elements = new[]{"Physical", "Fire", "Cold", "Poison", "Energy"};
        int[] resists = new[]{pet.PhysicalResistance, pet.FireResistance, pet.ColdResistance, pet.PoisonResistance, pet.EnergyResistance};
        int[] damage = new[]{pet.PhysicalDamage, pet.FireDamage, pet.ColdDamage, pet.PoisonDamage, pet.EnergyDamage};
        for (var i = 0; i < elements.Length; i++)
        {
            var y = 373 + i * 21;
            AddLabel(28, y, 1152, elements[i]); AddLabel(153, y, new[]{49,33,89,68,13}[i], $"{resists[i]}%"); AddLabel(246, y, new[]{49,33,89,68,13}[i], $"{damage[i]}%");
        }
        AddLabel(28, 486, 1152, $"Base damage: {pet.DamageMin} - {pet.DamageMax}");
        AddLabel(28, 510, 1152, $"Follower slots: {pet.ControlSlots} / {HavenLoreCompatibility.MaxSlots(pet)}");
        AddLabel(28, 534, 1152, $"Taming requirement: {pet.MinTameSkill:F1}");
        AddLabel(28, 558, 1152, $"Barding difficulty: {BaseInstrument.GetBaseDifficulty(pet):F1}");
        AddHtml(28, 584, 280, 35, $"<BASEFONT COLOR=#FFFFFF>Food: {pet.FavoriteFood}<BR>Pack: {pet.PackInstinct}</BASEFONT>");
        Skills(CombatSkills, 136, "Combat ratings");
        Skills(MagicSkills, 348, "Lore and magic");
        var rarity=HavenPetRarity.Find(pet);if(rarity!=null)AddLabel(28,76,rarity.Tier==3?53:rarity.Tier==2?1153:1152,HavenPetRarity.Label(rarity.Tier)+" | "+pet.GetType().Name);
        var abilities=PetTrainingHelper.GetAbilityProfile(pet);var learned=abilities==null?"":string.Join("<BR>",abilities.EnumerateAllAbilities().Select(a=>HavenMenuText.Encode(a.ToString())));
        AddLabel(28,634,53,"Abilities / specials");
        AddHtml(28,658,285,73,"<BASEFONT COLOR=#FFFFFF>"+HavenMenuText.Encode(HavenPetLore.SignatureName(pet))+"<BR>"+learned+"</BASEFONT>",false,true);

        if (HavenLoreCompatibility.Owned(viewer, pet)) { Button(28, 750, 1, "Animal Training"); }
        Button(243, 750, 2, "Abilities / lore"); Button(381, 750, 3, "Refresh"); Button(516, 750, 0, "Close");
    }
    private void Button(int x, int y, int id, string label) { HavenLoreCompatibility.Button(this,x,y,id==1?200:id==2?128:110,id,label); }
    private void AddHtml(int x,int y,int w,int h,string text){base.AddHtml(x,y,w,h,text,false,false);}
    private void Bar(int x, int y, string label, int current, int maximum, bool available)
    {
        AddLabel(x, y, 1152, label);
        AddLabel(x + 169, y, 1152, available ? $"{100.0 * current / Math.Max(1, maximum):F1}%" : "--");
        AddImageTiled(x, y + 23, 256, 7, 9750);
        if (available && current > 0) { AddImageTiled(x, y + 23, HavenLoreCompatibility.Clamp((int)(256L * current / Math.Max(1, maximum)), 1, 256), 7, 9752); }
    }
    private void Vital(int y, string attribute, int value, string vital, int current, int maximum, int regen, bool enabled)
    {
        AddLabel(28, y, 1152, attribute); AddLabel(28, y + 21, 53, $"{value}");
        AddLabel(143, y, 1152, vital); AddLabel(143, y + 21, 1152, $"{current}/{maximum}");
        AddLabel(259, y + 21, 1152, enabled ? $"+{regen}" : "Off");
        // Match native Animal Lore regeneration calculations.
        AddImageTiled(143, y + 45, 163, 5, 9750);
        if (current > 0 && maximum > 0) { AddImageTiled(143, y + 45, HavenLoreCompatibility.Clamp((int)(163L * current / maximum), 1, 163), 5, 9752); }
    }
    private void Skills(SkillName[] skills, int y, string title)
    {
        AddLabel(342, y, 53, title); AddLabel(537, y, 53, "Skill / cap");
        for (var i = 0; i < skills.Length; i++)
        {
            var skill = _pet.Skills[skills[i]];
            AddLabel(342, y + 24 + i * 19, 1152, skill.Name);
            AddLabel(535, y + 24 + i * 19, 1152, $"{skill.Base:F1}/{skill.Cap:F1}");
        }
    }
    internal bool CanRefresh(Mobile owner) => CanInspect(owner, _pet);
    internal static bool CanInspect(Mobile owner, BaseCreature pet)
    {
        if (pet?.Deleted != false || owner?.Deleted != false || !owner.Alive) { return false; }
        if (pet.Map == owner.Map && owner.InRange(pet, 12) && owner.InLOS(pet)) return true;
        return owner.Backpack!=null && owner.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Cast<HavenPetTicket>().Any(t=>!t.Deleted&&t.Owner==owner&&t.Pet==pet&&HavenResources.Accessible(owner,t));
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        if (info.ButtonID == 0 || !CanRefresh(sender.Mobile)) { return; }
        if (info.ButtonID == 1) { HavenLoreCompatibility.OpenTraining(sender.Mobile, _pet); }
        else
        {
            if (info.ButtonID == 2)
            {
                sender.Mobile.SendGump(new HavenPetLoreGump(_pet));
                return;
            }
            DisplayTo(sender.Mobile, _pet);
        }
    }
}

}
