using System.Linq;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using CompanionParty = Server.Engines.PartySystem.Party;

namespace Server.UOOffline;

public sealed class HavenCompanionGump : Gump
{
    private readonly HavenCompanion _companion;
    private readonly int _tab;

    public static void DisplayTo(Mobile from, HavenCompanion companion, int tab = 0)
    {
        if (from?.Deleted != false || companion?.Deleted != false || companion.BoundOwner != from) { return; }
        companion.UpdateTraining(Core.Now);
        from.CloseGump<HavenCompanionGump>();
        from.SendGump(new HavenCompanionGump(companion, tab));
    }

    internal HavenCompanionGump(HavenCompanion companion, int tab = 0) : base(20, 60)
    {
        _companion = companion;
        _tab = tab;
        AddBackground(0, 0, 370, 370, 5054);
        AddBackground(10, 10, 350, 350, 3000);
        AddLabel(20, 20, 0, companion.Name);
        AddLabel(20, 43, 0, $"{companion.Role} | Level {companion.TrainingLevel:N0} | {companion.ControlOrder}");
        if (companion.Expedition is { } trip) { AddLabel(20, 65, 0, trip.Status); }
        else { AddLabel(20, 65, 0, $"HP {companion.Hits}/{companion.HitsMax}  Mana {companion.Mana}/{companion.ManaMax}"); }
        Button(20, 95, 100, "Orders");
        Button(115, 95, 101, "Stats");
        Button(205, 95, 102, "Role");
        Button(285, 95, 103, "Gear");
        switch (tab)
        {
            case 1:
                AddLabel(20, 130, 0, $"Str {companion.Str}  Dex {companion.Dex}  Int {companion.Int}");
                AddLabel(20, 152, 0, $"Stamina {companion.Stam}/{companion.StamMax}  Damage {companion.DamageMin}-{companion.DamageMax}");
                AddLabel(20, 174, 0, $"Resists P/F/C/P/E: {companion.PhysicalResistance}/{companion.FireResistance}/{companion.ColdResistance}/{companion.PoisonResistance}/{companion.EnergyResistance}");
                AddLabel(20, 196, 0, $"Swords {companion.Skills.Swords.Value:F1}  Tactics {companion.Skills.Tactics.Value:F1}");
                AddLabel(20, 218, 0, $"Healing {companion.Skills.Healing.Value:F1}  Resist {companion.Skills.MagicResist.Value:F1}");
                AddLabel(20, 240, 0, $"Music {companion.Skills.Musicianship.Value:F1}  Discord {companion.Skills.Discordance.Value:F1}");
                AddLabel(20, 262, 0, $"Peace {companion.Skills.Peacemaking.Value:F1}  Provoke {companion.Skills.Provocation.Value:F1}");
                AddLabel(20, 284, 0, $"Magery {companion.Skills.Magery.Value:F1}  Weaving {companion.Skills.Spellweaving.Value:F1}");
                AddLabel(20, 306, 0, $"Base stats: {companion.RawStr} Str / {companion.RawDex} Dex / {companion.RawInt} Int");
                break;
            case 2:
                Button(20, 133, 10, "Fighter - melee support");
                Button(20, 164, 11, "Healer - faster healing");
                Button(20, 195, 12, "Bard - discord and songs");
                Button(20, 226, 13, "Caster - mage / spellweaver");
                Button(20, 257, 15, "Archer - ranged support");
                AddHtml(20, 291, 330, 36, "Bard: Discord 60; peace/provoke 75; songs 80/90.<BR>Caster: bolts, renewal; death/life at 80.");
                break;
            case 3:
                Button(20, 133, 14, "Equip item...");
                Button(195, 133, 6, "Open pack");
                AddHtml(20, 175, 330, 110, string.Join("<BR>", companion.Items.Where(i => i.Layer != Layer.Backpack).Select(i => $"{i.Layer}: {i.Name ?? i.DefaultName}")), false, true);
                Button(20, 290, 18, "Claim evolving arms");
                break;
            case 4:
                AddHtml(20, 130, 330, 30, "Five-minute expeditions; returns automatically.");
                Button(20, 170, 20, "Grind for loot");
                Button(195, 170, 21, "Gather ore");
                Button(20, 207, 22, "Gather wood");
                Button(195, 207, 23, "Gather leather");
                Button(20, 244, 24, "Gather reagents");
                Button(195, 244, 25, "Return now");
                Button(20, 280, 26, "Taming missions...");
                break;
            case 5:
                AddLabel(20, 128, 0, $"Taming {companion.Skills.AnimalTaming.Base:F1} / Lore {companion.Skills.AnimalLore.Base:F1}");
                for (var i = 0; i < 6; i++)
                {
                    var kind = (HavenExpeditionKind)(5 + i);
                    Button(20, 151 + i * 25, 30 + i, $"{HavenTamingMissions.PetName(kind)} {HavenTamingMissions.Requirement(kind):F1} both skills");
                }
                Button(20, 307, 27, "Rare custom pets...");
                break;
            case 6:
                AddLabel(20, 128, 0, "Required Taming AND Lore; rarity is random");
                for (var i = 6; i < 12; i++)
                {
                    var kind = (HavenExpeditionKind)(5 + i);
                    Button(20, 151 + (i - 6) * 25, 30 + i, $"{HavenTamingMissions.PetName(kind)} {HavenTamingMissions.Requirement(kind):F1} both skills");
                }
                AddHtml(20, 307, 330, 20, "Mounts: Emberwing, Frostmane, Verdant, Stormhorn.");
                break;
            default:
                Button(20, 133, 1, "Follow");
                Button(195, 133, 2, "Guard");
                Button(20, 170, 3, "Stay");
                Button(195, 170, 4, "Attack...");
                Button(20, 207, 5, "Heal / rez");
                Button(195, 207, 6, "Pack");
                Button(20, 244, 9, "Recall");
                Button(195, 244, 7, CompanionParty.Get(companion)?.Contains(companion.BoundOwner) == true ? "Leave party" : "Join party");
                Button(20, 280, 16, companion.TamingAssistActive ? "Stop assist" : "Tame assist...");
                Button(195, 280, 17, "Tasks");
                AddHtml(20, 310, 330, 20, "All roles auto-heal within 12 tiles and sight.");
                break;
        }
        Button(20, 332, 110, "Refresh");
        Button(240, 332, 0, "Close");
    }

    private void Button(int x, int y, int id, string text)
    {
        AddButton(x, y, 4005, 4007, id);
        AddLabel(x + 32, y + 2, 0, text);
    }

    public override void OnResponse(NetState sender, in RelayInfo info) => HandleCommand(sender.Mobile, info.ButtonID);

    internal void HandleCommand(Mobile from, int button)
    {
        if (button == 0 || _companion.Deleted || _companion.BoundOwner != from) { return; }
        if (button is >= 100 and <= 103) { DisplayTo(from, _companion, button - 100); return; }
        if (button == 17) { DisplayTo(from, _companion, 4); return; }
        if (button == 26) { DisplayTo(from, _companion, 5); return; }
        if (button == 27) { DisplayTo(from, _companion, 6); return; }
        if (button == 25 || button == 9 && _companion.Expedition != null)
        { if (_companion.Expedition?.Return(from, Core.Now) != true) { DisplayTo(from, _companion, 4); } return; }
        if (button == 110) { _companion.RecoverFromDeath(Core.Now, true); DisplayTo(from, _companion, _tab); return; }
        if (button == 9)
        {
            HavenCompanions.ClaimOrRecall(from);
            DisplayTo(from, _companion, _tab);
            return;
        }
        if (_companion.Map != from.Map || !from.InRange(_companion, 18))
        {
            from.SendMessage("Your companion is out of reach. Use Recall to bring them back.");
            DisplayTo(from, _companion, _tab);
            return;
        }
        if (button is >= 20 and <= 24 or >= 30 and <= 41)
        {
            var kind = (HavenExpeditionKind)(button >= 30 ? button - 25 : button - 20);
            if (HavenCompanionExpedition.Start(_companion, from, kind)) { return; }
            from.SendMessage("Your companion must meet the mission skills and be alive, nearby and ready.");
            DisplayTo(from, _companion, 4); return;
        }
        switch (button)
        {
            case 1:
                _companion.Combatant = null;
                _companion.ControlTarget = from;
                _companion.ControlOrder = OrderType.Follow;
                break;
            case 2:
                _companion.ControlTarget = from;
                _companion.ControlOrder = OrderType.Guard;
                break;
            case 3:
                _companion.Combatant = null;
                _companion.ControlTarget = null;
                _companion.ControlOrder = OrderType.Stay;
                break;
            case 4:
                from.Target = new CompanionAttackTarget(_companion);
                from.SendMessage("Choose a monster for your companion to attack.");
                break;
            case 5:
                if (!_companion.Support(from)) { from.SendMessage("Stand within 12 tiles and in sight. Your companion needs mana, a ready heal and must be alive."); }
                break;
            case 6:
                if (_companion.Backpack.CheckContentDisplay(from)) { _companion.Backpack.DisplayTo(from); }
                else { from.SendMessage("Stand within 3 tiles to open the pack."); }
                break;
            case 7:
                if (CompanionParty.Get(_companion)?.Contains(from) == true) { CompanionParty.Get(_companion).Remove(_companion); }
                else { _companion.JoinParty(from); }
                break;
            case 18:
                _companion.ClaimEvolvingArms(from);
                break;
            case 16:
                _companion.RequestTamingAssist(from);
                break;
            case 15:
                _companion.Role = HavenCompanionRole.Archer;
                break;
            case 14:
                _companion.RequestEquipment(from);
                break;
            case 10:
            case 11:
            case 12:
            case 13:
                _companion.Role = (HavenCompanionRole)(button - 10);
                break;
        }
        DisplayTo(from, _companion, _tab);
    }

    private sealed class CompanionAttackTarget : Target
    {
        private readonly HavenCompanion _companion;
        public CompanionAttackTarget(HavenCompanion companion) : base(10, false, TargetFlags.None) => _companion = companion;
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_companion.Deleted || _companion.IsDeadPet || _companion.BoundOwner != from ||
                from.Map != _companion.Map || !from.InRange(_companion, 18) || targeted is not Mobile enemy ||
                enemy.Deleted || !enemy.Alive || enemy.Map != _companion.Map || !_companion.InRange(enemy, 10) ||
                !_companion.CanBeHarmful(enemy, false) || !_companion.InLOS(enemy)) { return; }
            _companion.ControlTarget = enemy;
            _companion.ControlOrder = OrderType.Attack;
            _companion.Combatant = enemy;
        }
    }
}
