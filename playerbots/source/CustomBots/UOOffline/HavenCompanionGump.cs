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
    private readonly int _skillPage;
    internal const int SkillsPerPage = 12;

    public static void DisplayTo(Mobile from, HavenCompanion companion, int tab = 0, int skillPage = 0)
    {
        if (from?.Deleted != false || companion?.Deleted != false || companion.BoundOwner != from) { return; }
        companion.UpdateTraining(Core.Now);
        from.CloseGump<HavenCompanionGump>();
        from.SendGump(new HavenCompanionGump(companion, tab, skillPage));
    }

    internal HavenCompanionGump(HavenCompanion companion, int tab = 0, int skillPage = 0) : base(20, 60)
    {
        _companion = companion;
        _tab = tab;
        var pages = (companion.Skills.Length + SkillsPerPage - 1) / SkillsPerPage;
        _skillPage = System.Math.Clamp(skillPage, 0, pages - 1);
        var width = tab == 1 ? 620 : 520;
        var height = tab == 1 ? 620 : 410;
        AddBackground(0, 0, width, height, 5054);
        AddBackground(10, 10, width - 20, height - 20, 3000);
        AddLabel(20, 20, 0, companion.Name);
        AddLabel(20, 43, 0, $"{companion.Role} | Level {companion.TrainingLevel:N0} | {companion.ControlOrder}");
        if (companion.Expedition is { } trip) { AddHtml(20, 65, width - 40, 28, $"<BASEFONT COLOR=#181818>{System.Net.WebUtility.HtmlEncode(trip.Status)}</BASEFONT>", false, true); }
        else { AddLabel(20, 65, 0, $"HP {companion.Hits}/{companion.HitsMax}  Mana {companion.Mana}/{companion.ManaMax}"); }
        Button(20, 95, 100, "Orders");
        Button(105, 95, 101, "Stats");
        Button(183, 95, 102, "Role");
        Button(260, 95, 103, "Gear");
        Button(335, 95, 17, "Tasks");
        Button(415, 95, 107, "Utility");
        switch (tab)
        {
            case 1:
                AddLabel(20, 130, 0, $"Str {companion.Str}  Dex {companion.Dex}  Int {companion.Int}");
                AddLabel(20, 152, 0, $"Stamina {companion.Stam}/{companion.StamMax}  Damage {companion.DamageMin}-{companion.DamageMax}");
                AddLabel(20, 174, 0, $"Resists P/F/C/P/E: {companion.PhysicalResistance}/{companion.FireResistance}/{companion.ColdResistance}/{companion.PoisonResistance}/{companion.EnergyResistance}");
                AddLabel(20, 196, 0, $"Base stats: {companion.RawStr} Str / {companion.RawDex} Dex / {companion.RawInt} Int");
                AddLabel(20, 221, 0, "Skill");
                AddLabel(270, 221, 0, "Trained");
                AddLabel(370, 221, 0, "Effective");
                AddLabel(475, 221, 0, "Limit");
                var skills = Enumerable.Range(0, companion.Skills.Length)
                    .Select(i => companion.Skills[i]).OrderBy(skill => skill.Name).ToArray();
                for (var row = 0; row < SkillsPerPage; row++)
                {
                    var i = _skillPage * SkillsPerPage + row;
                    if (i >= skills.Length) { break; }
                    var skill = skills[i]; var y = 248 + row * 24;
                    AddLabel(20, y, 0, skill.Name);
                    AddLabel(270, y, 0, $"{skill.Base:F1}");
                    AddLabel(370, y, 0, $"{skill.Value:F1}");
                    AddLabel(475, y, 0, skill.Cap >= 6500 ? "Uncapped" : $"{skill.Cap:F1}");
                }
                if (_skillPage > 0) { Button(20, 545, 111, "Previous"); }
                AddLabel(230, 548, 0, $"Skills page {_skillPage + 1} / {pages}");
                if (_skillPage + 1 < pages) { Button(475, 545, 112, "Next"); }
                break;
            case 2:
                Button(20, 133, 10, "Fighter - melee support");
                Button(20, 164, 11, "Healer - faster healing");
                Button(20, 195, 12, "Bard - discord and songs");
                Button(20, 226, 13, "Caster - mage / spellweaver");
                Button(20, 257, 15, "Archer - ranged support");
                AddHtml(20, 291, 470, 55, "Bard: Discord 60; peace/provoke 75; songs 80/90.<BR>Caster: bolts, renewal; death/life at 80.");
                break;
            case 3:
                Button(20, 133, 14, "Equip item...");
                Button(195, 133, 6, "Open pack");
                AddHtml(20, 175, 330, 110, string.Join("<BR>", companion.Items.Where(i => i.Layer != Layer.Backpack).Select(i => $"{i.Layer}: {i.Name ?? i.DefaultName}")), false, true);
                Button(20, 290, 18, "Claim evolving arms");
                break;
            case 4:
                AddHtml(20, 130, 330, 30, "Choose a mission, then its length: 5â€“60 minutes.");
                Button(20, 170, 20, "Grind for loot");
                Button(195, 170, 21, "Gather ore");
                Button(20, 207, 22, "Gather wood");
                Button(195, 207, 23, "Gather leather");
                Button(20, 244, 24, "Gather reagents");
                Button(195, 244, 25, "Return now");
                Button(20, 280, 26, "Taming missions...");
                Button(195, 280, 29, "AFK / auto...");
                Button(20, 307, 28, companion.AutoSkinning ? "Hunting: skin + cut ON" : "Hunting: skin + cut OFF");
                Button(20,339,44,"Resource routes...");Button(195,339,45,"Reports");
                break;
            case 5:
                AddLabel(20, 128, 0, $"Taming {companion.Skills.AnimalTaming.Base:F1} / Lore {companion.Skills.AnimalLore.Base:F1}");
                for (var i = 0; i < 6; i++)
                {
                    var kind = (HavenExpeditionKind)(5 + i);
                    MissionButton(companion, kind, 151 + i * 25, 30 + i);
                }
                Button(20, 307, 27, "Rare custom pets...");
                break;
            case 6:
                AddLabel(20, 128, 0, $"Your Taming {companion.Skills.AnimalTaming.Base:F1} | Lore {companion.Skills.AnimalLore.Base:F1}");
                for (var i = 6; i < 12; i++)
                {
                    var kind = (HavenExpeditionKind)(5 + i);
                    MissionButton(companion, kind, 151 + (i - 6) * 25, 30 + i);
                }
                AddHtml(20, 307, 470, 40, "Mounts: Emberwing, Frostmane, Verdant, Stormhorn.");
                break;
            case 7:
                Button(20, 140, 16, companion.TamingAssistActive ? "Stop taming assist" : "Taming assistance...");
                Button(20, 183, 43, "Dungeon puzzle assistance...");
                Button(20, 226, 46, "Store deeds in ledger");
                Button(20, 269, 45, "Mission reports");
                AddHtml(20, 317, 470, 36, "Use Tasks for missions and AFK mode. Common combat commands stay on Orders.");
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
                AddHtml(20, 294, 470, 45, "All roles automatically heal you when able.<BR>Tasks: missions and AFK. Utility: taming, puzzles and ledger.");
                break;
        }
        Button(20, height - 38, 110, "Refresh");
        Button(width - 130, height - 38, 0, "Close");
    }

    internal static string MissionStatus(HavenCompanion companion, HavenExpeditionKind kind)
    {
        var required = HavenTamingMissions.Requirement(kind);
        var taming = companion.Skills.AnimalTaming.Base >= required;
        var lore = companion.Skills.AnimalLore.Base >= required;
        return taming && lore ? "Ready" : taming ? "Need Lore" : lore ? "Need Tame" : "Need both";
    }
    private void MissionButton(HavenCompanion companion, HavenExpeditionKind kind, int y, int id)
    {
        AddButton(20, y, 4005, 4007, id);
        AddLabelCropped(52, y + 2, 325, 23, 0, $"{HavenTamingMissions.PetName(kind)} ({HavenTamingMissions.Requirement(kind):F1})");
        AddTooltip(1042971, $"{HavenTamingMissions.PetName(kind)} — requires {HavenTamingMissions.Requirement(kind):F1} Taming and Lore.");
        var status = MissionStatus(companion, kind);
        AddLabel(390, y + 2, status == "Ready" ? 0x44 : 0x21, status);
        AddTooltip(1042971, $"Requires {HavenTamingMissions.Requirement(kind):F1} in BOTH Taming and Lore. Your companion: {companion.Skills.AnimalTaming.Base:F1} Taming, {companion.Skills.AnimalLore.Base:F1} Lore.");
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
        if (button == 107) { DisplayTo(from, _companion, 7); return; }
        if (button is 111 or 112) { DisplayTo(from, _companion, 1, _skillPage + (button == 111 ? -1 : 1)); return; }
        if (button == 46)
        {
            try { from.SendMessage($"Stored {HavenResourceLedger.StoreCompanionPack(from)} resource deeds in the companion ledger."); }
            catch (System.InvalidOperationException ex) { from.SendMessage(ex.Message); }
            DisplayTo(from, _companion, 7); return;
        }
        if (button == 29) { from.SendGump(new HavenCompanionAfkGump(HavenCompanionIdleMissions.Ensure(_companion))); return; }
        if (button == 17) { DisplayTo(from, _companion, 4); return; }
        if(button==44) { from.SendGump(new HavenRegionalMissionGump(_companion));return; }
        if(button==45)
        { var journal=HavenMissionJournal.Find(_companion);if(journal==null) { from.SendMessage("No mission report recorded yet."); }else { journal.Show(from); }return; }
        if (button == 26) { DisplayTo(from, _companion, 5); return; }
        if (button == 27) { DisplayTo(from, _companion, 6); return; }
        if (button == 43)
        {
            foreach (var chamber in HavenShadowChamber.Registry)
            {
                if (chamber.Participant(from)) { from.SendGump(new HavenShadowMenu(from, chamber)); return; }
            }
            from.SendMessage("Enter a supported dungeon room together. The room menu lets your companion solve or stop its puzzle.");
            DisplayTo(from, _companion, _tab, _skillPage); return;
        }
        if (button == 25 || button == 9 && _companion.Expedition != null)
        { if (_companion.Expedition?.Return(from, Core.Now) != true) { DisplayTo(from, _companion, 4); } return; }
        if (button == 110) { _companion.RecoverFromDeath(Core.Now, true); DisplayTo(from, _companion, _tab, _skillPage); return; }
        if (button == 9)
        {
            HavenCompanions.ClaimOrRecall(from);
            DisplayTo(from, _companion, _tab, _skillPage);
            return;
        }
        if (_companion.Map != from.Map || !from.InRange(_companion, 18))
        {
            from.SendMessage("Your companion is out of reach. Use Recall to bring them back.");
            DisplayTo(from, _companion, _tab, _skillPage);
            return;
        }
        if (button is >= 20 and <= 24 or >= 30 and <= 41)
        {
            var kind = (HavenExpeditionKind)(button >= 30 ? button - 25 : button - 20);
            from.CloseGump<HavenMissionDurationGump>();
            from.SendGump(new HavenMissionDurationGump(_companion,kind)); return;
        }
        switch (button)
        {
            case 28:
                _companion.AutoSkinning = !_companion.AutoSkinning;
                from.SendMessage(_companion.AutoSkinning
                    ? "I will skin our kills between fights and keep the cut leather in my pack."
                    : "I will leave corpses unskinned while we hunt.");
                DisplayTo(from, _companion, 4);
                return;
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
        DisplayTo(from, _companion, _tab, _skillPage);
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
