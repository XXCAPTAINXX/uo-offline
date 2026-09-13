using System;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        private bool _missionReturnPending;
        private Timer _missionReturnTimer;

        private void ClearMissionReturn()
        {
            _missionReturnPending = false;
            if (_missionReturnTimer != null) _missionReturnTimer.Stop();
            _missionReturnTimer = null;
        }

        private void ScheduleMissionReturn()
        {
            if (_missionReturnTimer != null) _missionReturnTimer.Stop();
            _missionReturnTimer = Timer.DelayCall(TimeSpan.FromSeconds(5), CheckMissionReturn);
        }

        private void CheckMissionReturn()
        {
            _missionReturnTimer = null;
            if (Deleted || !_missionReturnPending || _owner == null || _owner.Deleted)
            {
                ClearMissionReturn();
                return;
            }
            // Login restores the owner's map. Never appear beside an offline character.
            if (_owner.NetState != null && PlaceAfterMission())
            {
                _owner.SendMessage("Your companion has returned from the mission and is following you.");
                return;
            }
            ScheduleMissionReturn();
        }

        private bool PlaceAfterMission()
        {
            if (!_missionReturnPending || Deleted || OnMission || _owner == null || _owner.Deleted ||
                !_owner.Alive || !Alive || IsDeadPet || IsStabled ||
                _owner.Map == null || _owner.Map == Map.Internal ||
                (ControlMaster != null && ControlMaster != _owner)) return false;
            if (!Controlled && !SetControlMaster(_owner)) return false;
            if (ControlMaster != _owner) return false;

            // This is completion of an already dispatched mission, not a combat escape.
            // Use the owner's current position, including a different facet or elevation.
            MoveToWorld(_owner.Location, _owner.Map);
            Combatant = null;
            FocusMob = null;
            Warmode = false;
            ControlTarget = _owner;
            ControlOrder = OrderType.Follow;
            AIObject.NextMove = Core.TickCount;
            AIObject.Activate();
            ClearMissionReturn();
            return true;
        }
    }
}
