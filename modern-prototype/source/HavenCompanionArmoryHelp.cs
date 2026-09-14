using System;
using System.Linq;
using Server.Engines.Shadowguard;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        private ArmoryEncounter _armoryRoom;
        private Item _armoryGoal;
        private PathFollower _armoryPath;
        private DateTime _nextArmoryStep, _armoryProgressAt;
        private Point3D _armoryLastPosition;
        private string _armoryMessage;
        internal bool ArmoryHelpActive { get { return _armoryRoom != null; } }

        internal bool RequestArmoryHelp(Mobile owner)
        {
            if (ArmoryHelpActive)
            {
                StopArmoryHelp();
                owner.SendMessage("Armory puzzle assistance stopped.");
                return true;
            }
            var room = ShadowguardController.GetEncounter(owner.Location, owner.Map) as ArmoryEncounter;
            if (room == null) return false;
            if (!CanCommand(owner) || !room.HasBegun || room.Completed || !room.Participants.Contains(owner as Server.Mobiles.PlayerMobile))
            { owner.SendMessage("Enter an active Armory room with your living companion first."); return true; }
            if (!SetOrder(owner, Server.Mobiles.OrderType.Stay)) return true;
            ClearRoleSupport();
            _armoryRoom = room;
            _armoryMessage = null;
            _nextArmoryStep = DateTime.MinValue;
            ArmoryStatus("I'll collect phylacteries, purify them in the flames, and use them on the cursed armor. Press Puzzle again to stop.");
            return true;
        }

        internal void StopArmoryHelp()
        {
            if (!ArmoryHelpActive) return;
            _armoryRoom = null;
            _armoryGoal = null;
            _armoryPath = null;
            if (!OnMission && Controlled && ControlMaster == BoundOwner && ControlOrder == Server.Mobiles.OrderType.Stay)
            { ControlTarget = BoundOwner; ControlOrder = Server.Mobiles.OrderType.Follow; }
        }

        private void ArmoryStatus(string text)
        {
            if (_armoryMessage == text) return;
            _armoryMessage = text;
            if (BoundOwner != null && !BoundOwner.Deleted) BoundOwner.SendMessage(53, Name + ": " + text);
        }

        internal bool ThinkArmoryHelp()
        {
            var room = _armoryRoom;
            if (room == null) return false;
            if (!HavenPreview.Enabled || Deleted || !Alive || IsDeadPet || OnMission || !Controlled ||
                BoundOwner == null || BoundOwner.Deleted || !BoundOwner.Alive || ControlMaster != BoundOwner ||
                Map != BoundOwner.Map || room.Instance == null || room.Instance.Encounter != room ||
                !room.HasBegun || room.Completed || room.Armor == null || room.Items == null ||
                !room.Region.Contains(Location) || !room.Region.Contains(BoundOwner.Location) ||
                ControlOrder != Server.Mobiles.OrderType.Stay)
            { StopArmoryHelp(); return false; }
            if (DateTime.UtcNow < _nextArmoryStep) return true;
            _nextArmoryStep = DateTime.UtcNow.AddMilliseconds(250);
            Combatant = null; FocusMob = null; Warmode = false;
            if (Spell != null) return true;

            var phylactery = Backpack == null ? null : Backpack.FindItemsByType(typeof(Phylactery), true)
                .OfType<Phylactery>().OrderByDescending(p => p.Purified).FirstOrDefault();
            if (phylactery == null)
            {
                phylactery = room.Region.GetEnumeratedItems().OfType<Phylactery>()
                    .Where(p => !p.Deleted && p.Movable && p.Parent == null && p.Map == Map && room.Region.Contains(p.Location))
                    .OrderBy(p => GetDistanceToSqrt(p.Location)).FirstOrDefault();
                if (phylactery == null)
                { ArmoryStatus("Waiting for a phylactery to drop from the enchanted armor."); return true; }
                if (!ApproachArmoryItem(phylactery, 1)) return true;
                if (Backpack == null || !Backpack.TryDropItem(this, phylactery, false))
                { ArmoryStatus("My backpack is full. Make some space, then press Puzzle again."); StopArmoryHelp(); return true; }
                ArmoryStatus("Picked up a phylactery.");
                return true;
            }

            Item goal = phylactery.Purified
                ? room.Armor.OfType<CursedSuitOfArmor>().Where(a => !a.Deleted).OrderBy(a => GetDistanceToSqrt(a.Location)).FirstOrDefault() as Item
                : room.Items.OfType<PurifyingFlames>().Where(f => !f.Deleted).OrderBy(f => GetDistanceToSqrt(f.Location)).FirstOrDefault();
            if (goal == null)
            { ArmoryStatus("There isn't an available puzzle target. Assistance stopped."); StopArmoryHelp(); return true; }
            if (!ApproachArmoryItem(goal, 3)) return true;

            // Use the actual item and native target callback: purification, consumption,
            // armor destruction, and encounter credit follow the regular puzzle rules.
            var previous = Target;
            phylactery.OnDoubleClick(this);
            if (Target != null && Target != previous) Target.Invoke(this, goal);
            if (phylactery.Deleted) ArmoryStatus("Another cursed statue is cleared.");
            else if (phylactery.Purified) ArmoryStatus("Purified. Taking it to a cursed statue.");
            if (room.Completed) StopArmoryHelp();
            return true;
        }

        private bool ApproachArmoryItem(Item goal, int range)
        {
            if (_armoryGoal != goal)
            {
                _armoryGoal = goal;
                _armoryPath = new PathFollower(this, goal);
                _armoryProgressAt = DateTime.UtcNow;
                _armoryLastPosition = Location;
            }
            if (InRange(goal, range) && InLOS(goal)) return true;
            _armoryPath.Follow(true, InLOS(goal) ? range : 0);
            if (Location != _armoryLastPosition)
            { _armoryLastPosition = Location; _armoryProgressAt = DateTime.UtcNow; }
            else if (DateTime.UtcNow - _armoryProgressAt > TimeSpan.FromSeconds(12))
            { ArmoryStatus("I can't reach the next puzzle item. Clear the way and press Puzzle to retry."); StopArmoryHelp(); }
            return false;
        }
    }
}
