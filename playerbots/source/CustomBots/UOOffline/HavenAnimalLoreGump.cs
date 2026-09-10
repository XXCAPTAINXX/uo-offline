using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenAnimalLoreGump : Gump
{
    private readonly BaseCreature _pet;
    private static readonly SkillName[] CombatSkills = [SkillName.Wrestling, SkillName.Tactics, SkillName.MagicResist, SkillName.Anatomy, SkillName.Healing, SkillName.Poisoning, SkillName.Parry, SkillName.Hiding, SkillName.DetectHidden];
    private static readonly SkillName[] MagicSkills = [SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.Necromancy, SkillName.SpiritSpeak, SkillName.Mysticism, SkillName.Focus, SkillName.Spellweaving, SkillName.Musicianship, SkillName.Discordance, SkillName.Peacemaking, SkillName.Provocation];
    public static void DisplayTo(Mobile from, BaseCreature pet)
    {
        if (from?.NetState == null || pet?.Deleted != false) { return; }
        HavenPetAppearance.Refresh(pet);
        if (pet.ControlMaster == from) { HavenLegendaryPetSkills.Roll(pet); }
        from.CloseGump<HavenAnimalLoreGump>();
        from.SendGump(new HavenAnimalLoreGump(from, pet));
    }
    internal HavenAnimalLoreGump(Mobile viewer, BaseCreature pet) : base(20, 20)
    {
        _pet = pet;
        var training = HavenPetTraining.Find(pet);
        AddBackground(0, 0, 660, 680, 5054);
        AddImageTiled(12, 12, 636, 656, 2624);
        AddAlphaRegion(22, 128, 298, 498);
        AddAlphaRegion(330, 128, 308, 498);
        AddLabel(28, 24, 53, "ANIMAL LORE");
        AddHtml(28, 49, 280, 44, $"<BASEFONT COLOR=#FFFFFF>{Utility.FixHtml(pet.Name)}</BASEFONT>");
        AddLabel(28, 99, 1152, pet.IsBonded ? "Bonded" : pet.Map == Map.Internal ? "Stored pet / ticket preview" : pet.Controlled ? "Tamed - not bonded" : "Wild creature");
        Bar(350, 28, "Loyalty", pet.Loyalty, BaseCreature.MaxLoyalty, pet.Controlled);
        Bar(350, 75, "Training", training?.Progress ?? 0, 10000, training != null);
        AddLabel(28, 136, 53, "Attribute"); AddLabel(143, 136, 53, "Current / max"); AddLabel(259, 136, 53, "Regen");
        Vital(164, "Strength", pet.Str, "Health", pet.Hits, pet.HitsMax, AosAttributes.GetValue(pet, AosAttribute.RegenHits), pet.CanRegenHits);
        Vital(225, "Dexterity", pet.Dex, "Stamina", pet.Stam, pet.StamMax, AosAttributes.GetValue(pet, AosAttribute.RegenStam), pet.CanRegenStam);
        Vital(286, "Intelligence", pet.Int, "Mana", pet.Mana, pet.ManaMax, AosAttributes.GetValue(pet, AosAttribute.RegenMana), pet.CanRegenMana);
        AddLabel(28, 350, 53, "Element"); AddLabel(153, 350, 53, "Resist"); AddLabel(246, 350, 53, "Damage");
        string[] elements = ["Physical", "Fire", "Cold", "Poison", "Energy"];
        int[] resists = [pet.PhysicalResistance, pet.FireResistance, pet.ColdResistance, pet.PoisonResistance, pet.EnergyResistance];
        int[] damage = [pet.PhysicalDamage, pet.FireDamage, pet.ColdDamage, pet.PoisonDamage, pet.EnergyDamage];
        for (var i = 0; i < elements.Length; i++)
        {
            var y = 373 + i * 21;
            AddLabel(28, y, 1152, elements[i]); AddLabel(153, y, 1152, $"{resists[i]}%"); AddLabel(246, y, 1152, $"{damage[i]}%");
        }
        AddLabel(28, 486, 1152, $"Base damage: {pet.DamageMin} - {pet.DamageMax}");
        AddLabel(28, 510, 1152, $"Follower slots: {pet.ControlSlots} / {HavenPetTraining.MaxSlots(pet)}");
        AddLabel(28, 534, 1152, $"Taming requirement: {pet.MinTameSkill:F1}");
        AddLabel(28, 558, 1152, $"Barding difficulty: {BaseInstrument.GetBaseDifficulty(pet):F1}");
        AddHtml(28, 584, 280, 35, $"<BASEFONT COLOR=#FFFFFF>Food: {pet.FavoriteFood}<BR>Pack: {pet.PackInstinct}</BASEFONT>");
        Skills(CombatSkills, 136, "Combat ratings");
        Skills(MagicSkills, 348, "Lore and magic");
        if (HavenPetTraining.Owned(viewer, pet)) { Button(28, 640, 1, "Animal Training"); }
        Button(243, 640, 2, "Details"); Button(381, 640, 3, "Refresh"); Button(516, 640, 0, "Close");
    }
    private void Button(int x, int y, int id, string label) { AddButton(x, y, 4005, 4007, id); AddLabel(x + 33, y + 2, 1152, label); }
    private void Bar(int x, int y, string label, int current, int maximum, bool available)
    {
        AddLabel(x, y, 1152, label);
        AddLabel(x + 169, y, 1152, available ? $"{100.0 * current / Math.Max(1, maximum):F1}%" : "--");
        AddImageTiled(x, y + 23, 256, 7, 9750);
        if (available && current > 0) { AddImageTiled(x, y + 23, Math.Clamp((int)(256L * current / Math.Max(1, maximum)), 1, 256), 7, 9752); }
    }
    private void Vital(int y, string attribute, int value, string vital, int current, int maximum, int regen, bool enabled)
    {
        AddLabel(28, y, 1152, attribute); AddLabel(28, y + 21, 53, $"{value}");
        AddLabel(143, y, 1152, vital); AddLabel(143, y + 21, 1152, $"{current}/{maximum}");
        AddLabel(259, y + 21, 1152, enabled ? $"+{regen}" : "Off");
        AddTooltip(1042971, "Regeneration bonus from attributes. Natural recovery from skills and creature abilities is additional.");
        AddImageTiled(143, y + 45, 163, 5, 9750);
        if (current > 0 && maximum > 0) { AddImageTiled(143, y + 45, Math.Clamp((int)(163L * current / maximum), 1, 163), 5, 9752); }
    }
    private void Skills(SkillName[] skills, int y, string title)
    {
        AddLabel(342, y, 53, title); AddLabel(537, y, 53, "Skill / cap");
        for (var i = 0; i < skills.Length; i++)
        {
            var skill = _pet.Skills[skills[i]];
            AddLabel(342, y + 24 + i * 20, 1152, skill.Name);
            AddLabel(535, y + 24 + i * 20, 1152, $"{skill.Base:F1}/{skill.Cap:F1}");
        }
    }
    internal bool CanRefresh(Mobile owner)
    {
        if (_pet.Deleted || !owner.Alive) { return false; }
        if (HavenMarketDirectory.CanShop(owner))
        {
            foreach (var stall in HavenMarketStall.Registry)
            {
                if (stall.Deleted) { continue; }
                foreach (var stock in stall.Stock)
                { if (stock is HavenMarketPetTicket { Deleted: false } ticket && ticket.Parent == stall && ticket.Pet == _pet) { return true; } }
            }
        }
        if (_pet.Map == owner.Map && owner.InRange(_pet, 12) && owner.InLOS(_pet)) { return true; }
        if (owner.Backpack != null)
        {
            foreach (var ticket in owner.Backpack.FindItemsByType<HavenExpeditionPetClaim>())
            {
                if (ticket.Owner == owner && ticket.ReservedPet == _pet) { return true; }
            }
            foreach (var token in owner.Backpack.FindItemsByType<ShrunkenPet>())
            {
                if (token.Inspect(owner) == _pet) { return true; }
            }
        }
        return false;
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0 || !CanRefresh(sender.Mobile)) { return; }
        if (info.ButtonID == 1) { HavenPetTrainingGump.DisplayTo(sender.Mobile, _pet); }
        else
        {
            if (info.ButtonID == 2)
            {
                sender.Mobile.SendMessage($"{_pet.Name}: food {_pet.FavoriteFood}; pack instinct {_pet.PackInstinct}; taming requirement {_pet.MinTameSkill:F1}.");
                sender.Mobile.SendMessage($"Self healing: {(_pet.CanHeal ? "yes" : "no")}; owner healing: {(_pet.CanHealOwner ? "yes" : "no")}; bard immunity: {(_pet.BardImmune ? "yes" : "no")}.");
                var rarity = _pet.Backpack?.FindItemByType<HavenPetRarity>();
                if (_pet is HavenSnowBear) { sender.Mobile.SendMessage(HavenSnowBear.RageDescription); }
                if(HavenPetSignatures.Kind(_pet)!=0) { sender.Mobile.SendMessage(HavenPetSignatures.Describe(_pet)); }
                var defense = HavenPetDefenses.Describe(_pet);
                if (defense.Length > 0) { sender.Mobile.SendMessage(defense); }
                if (rarity != null && HavenTamingMissions.IsCustomPet(_pet)) { sender.Mobile.SendMessage(HavenPetRarity.Describe(rarity.Tier)); }
            }
            DisplayTo(sender.Mobile, _pet);
        }
    }
}
