using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

public sealed class HavenCompanionGump : Gump
{
    private readonly HavenCompanion _companion;

    public static void DisplayTo(Mobile from, HavenCompanion companion)
    {
        if (from?.Deleted != false || companion?.Deleted != false || companion.BoundOwner != from ||
            from.Map != companion.Map || !from.InRange(companion, 12)) { return; }
        companion.UpdateTraining(Core.Now);
        from.CloseGump<HavenCompanionGump>();
        from.SendGump(new HavenCompanionGump(companion));
    }

    private HavenCompanionGump(HavenCompanion companion) : base(30, 30)
    {
        _companion = companion;
        AddBackground(0, 0, 600, 565, 5054);
        AddBackground(14, 14, 572, 537, 3000);
        AddHtml(32, 28, 530, 30, "<B>Your adventuring companion</B>");
        AddLabel(32, 65, 0, $"{companion.Name}  |  {companion.Role}  |  Level {companion.TrainingLevel:N0}");
        AddLabel(32, 90, 0, $"Mastery {companion.Mastery:N1}  |  Health {companion.Hits}/{companion.HitsMax}  |  Mana {companion.Mana}/{companion.ManaMax}");
        AddHtml(32, 120, 530, 55, "Training continues while you are away, including server downtime. Combat adds practice. Mastery has no gameplay cap.");
        Button(32, 190, 1, "Follow me");
        Button(310, 190, 2, "Guard me");
        Button(32, 235, 3, "Stay here");
        Button(310, 235, 4, "Attack a monster...");
        Button(32, 280, 5, "Heal / resurrect me");
        Button(310, 280, 6, "Open shared pack");
        Button(32, 325, 7, "Join my party");
        Button(310, 325, 8, "Leave party");
        AddHtml(32, 375, 530, 25, "<B>Choose a role — progress and equipment are kept</B>");
        Button(32, 410, 10, "Fighter");
        Button(220, 410, 11, "Healer");
        Button(408, 410, 12, "Bard");
        AddHtml(32, 460, 530, 55, "Healers mend wounds faster. Bards strengthen nearby allies. All roles can fight, heal and offer resurrection. Pack: 1,000 items / 50,000 stones; stand within 3 tiles.");
        Button(445, 520, 0, "Close");
    }

    private void Button(int x, int y, int id, string text)
    {
        AddButton(x, y, 4005, 4007, id);
        AddLabel(x + 36, y + 2, 0, text);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || _companion.Deleted || _companion.BoundOwner != from ||
            _companion.Map != from.Map || !from.InRange(_companion, 12)) { return; }
        switch (info.ButtonID)
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
                from.SendMessage("Choose the monster you want your companion to attack.");
                return;
            case 5:
                if (!_companion.Support(from)) { from.SendMessage("Stand within 3 tiles. Your companion needs mana and time to recover between heals, and must be alive."); }
                break;
            case 6:
                if (_companion.Backpack.CheckContentDisplay(from)) { _companion.Backpack.DisplayTo(from); }
                else { from.SendMessage("Stand within 3 tiles to open your companion's pack."); }
                return;
            case 7:
                _companion.JoinParty(from);
                break;
            case 8:
                Server.Engines.PartySystem.Party.Get(_companion)?.Remove(_companion);
                break;
            case 10:
            case 11:
            case 12:
                _companion.Role = (HavenCompanionRole)(info.ButtonID - 10);
                break;
        }
        DisplayTo(from, _companion);
    }

    private sealed class CompanionAttackTarget : Target
    {
        private readonly HavenCompanion _companion;
        public CompanionAttackTarget(HavenCompanion companion) : base(10, false, TargetFlags.None) => _companion = companion;
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_companion.Deleted || _companion.IsDeadPet || _companion.BoundOwner != from ||
                from.Map != _companion.Map || !from.InRange(_companion, 12) || targeted is not Mobile enemy ||
                enemy.Deleted || !enemy.Alive || enemy.Map != _companion.Map || !_companion.InRange(enemy, 10) ||
                !_companion.CanBeHarmful(enemy, false) || !_companion.InLOS(enemy)) { return; }
            _companion.ControlTarget = enemy;
            _companion.ControlOrder = OrderType.Attack;
            _companion.Combatant = enemy;
        }
    }
}
