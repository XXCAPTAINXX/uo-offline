using System;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public static class HavenSmokeTeleport
    {
        public static void Initialize()
        {
            EventSink.ServerStarted += () => {
                if (!HavenPreview.Enabled) return;
                foreach (var bomb in World.Items.Values.OfType<SmokeBomb>().ToArray()) { bomb.Weight=0.1; bomb.Stackable=true; }
            };
        }
        public static void Begin(Mobile from, SmokeBomb bomb)
        {
            if (!Ready(from,bomb)) return;
            from.SendMessage("Choose a visible location within twelve tiles. One smoke bomb is used on successful travel.");
            from.Target = new SmokeTarget(bomb);
        }
        private static bool Ready(Mobile from, SmokeBomb bomb)
        {
            if (!from.Alive || bomb.Deleted || from.Backpack==null || !bomb.IsChildOf(from.Backpack)) return false;
            if (from.Paralyzed || from.Frozen || !from.CanBeginAction(typeof(HavenSmokeTeleport)))
            { from.SendMessage("You cannot use a smoke bomb yet."); return false; }
            return true;
        }
        private sealed class SmokeTarget : Target
        {
            private readonly SmokeBomb _bomb;
            public SmokeTarget(SmokeBomb bomb) : base(12,true,TargetFlags.None) { _bomb=bomb; }
            protected override void OnTarget(Mobile from,object target)
            {
                if (!Ready(from,_bomb) || !(target is IPoint3D)) return;
                IPoint3D surface=(IPoint3D)target;
                SpellHelper.GetSurfaceTop(ref surface);
                var to=new Point3D(surface); var map=from.Map;
                if (map==null || map==Map.Internal || !from.InRange(to,12) || !from.InLOS(to) ||
                    Server.Factions.Sigil.ExistsOn(from) || Server.Misc.WeightOverloading.IsOverloaded(from) ||
                    !SpellHelper.CheckTravel(from,TravelCheckType.TeleportFrom) ||
                    !SpellHelper.CheckTravel(from,map,to,TravelCheckType.TeleportTo) ||
                    !map.CanSpawnMobile(to.X,to.Y,to.Z) || SpellHelper.CheckMulti(to,map) ||
                    Region.Find(to,map).GetRegion(typeof(Server.Regions.HouseRegion))!=null)
                { from.SendMessage("You cannot smoke-teleport there. Your bomb was not consumed."); return; }
                if (to==from.Location) return;
                if (!from.BeginAction(typeof(HavenSmokeTeleport))) return;
                var origin=from.Location;
                _bomb.Consume();
                Effects.SendLocationEffect(origin,map,0x3709,30,10,1108,6);
                Effects.PlaySound(origin,map,0x22F);
                BaseCreature.TeleportPets(from,to,map);
                from.MoveToWorld(to,map);
                Timer.DelayCall(TimeSpan.FromSeconds(3),()=>from.EndAction(typeof(HavenSmokeTeleport)));
            }
        }
    }
}
