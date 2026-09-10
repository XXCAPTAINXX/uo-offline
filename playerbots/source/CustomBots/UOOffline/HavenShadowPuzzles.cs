using System;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

public static class HavenShadowPuzzles
{
    internal static readonly string[] TreeNames =
    ["Honesty → Deceit", "Deceit → Honesty", "Compassion → Cruelty", "Cruelty → Compassion",
     "Valor → Cowardice", "Cowardice → Valor", "Justice → Injustice", "Injustice → Justice",
     "Sacrifice → Selfishness", "Selfishness → Sacrifice", "Honor → Shame", "Shame → Honor",
     "Spirituality → Materialism", "Materialism → Spirituality", "Humility → Pride", "Pride → Humility"];
    internal static string Instructions(HavenShadowRoom room) => room switch
    {
        HavenShadowRoom.Bar => "Throw rack bottles at pirates. Three hits each. Drinking mishaps cost stamina.",
        HavenShadowRoom.Orchard => "Easy matching: each tree names its partner. Click it, then target that partner.",
        HavenShadowRoom.Armory => "Defeat the guards. Use the brazier, then target an enchanted armor.",
        HavenShadowRoom.Fountain => "Defeat water elementals for pieces. Fill the two numbered canals; turn the valve.",
        HavenShadowRoom.Belfry => "Ring the bell, defeat drakes, then use the bell again to fly onto the dragon platform.",
        _ => "Defeat Minax's four lieutenants. Anon's matching elemental damage heals him."
    };
    internal static bool Use(HavenShadowChamber chamber, Mobile from, HavenShadowNode node, object target, double roll = 1)
    {
        if (chamber?.Deleted != false || node?.Deleted != false || node.Chamber != chamber || !chamber.Participant(from) ||
            !from.Alive || from.Frozen || from.Paralyzed || !from.InRange(node, 3) || !from.InLOS(node)) { return false; }
        switch (node.Kind)
        {
            case 0:
                if (target is not HavenShadowActor { Role: 0 } pirate || pirate.Chamber != chamber || !chamber.Actors.Contains(pirate) || !from.InRange(pirate, 22) || !from.InLOS(pirate)) { return false; }
                if (roll < .15) { from.Stam = Math.Max(0, from.Stam - 5); from.SendMessage("You nearly drink the bottle. Try another."); return true; }
                pirate.PuzzleHits++;
                if (pirate.PuzzleHits >= 3) { chamber.Killed(pirate); pirate.Delete(); }
                else { pirate.FixedEffect(0x36BD, 10, 8); }
                return true;
            case 1:
                if (target is not HavenShadowNode other || other == node || other.Chamber != chamber || other.Kind != 1 || other.State != 0 || node.State != 0) { return false; }
                if ((node.Key ^ 1) != other.Key) { from.SendMessage("Match the name after the arrow on your chosen tree."); return false; }
                node.State = other.State = 1; node.Hue = other.Hue = 0x59; chamber.Progress += 2;
                from.SendMessage($"Orchard: {chamber.Progress / 2}/8 pairs matched.");
                if (chamber.Progress == 16) { chamber.Finish(true); } return true;
            case 2:
                if (chamber.Supplies < 1 || target is not HavenShadowActor { Role: 2 } armor || armor.Chamber != chamber ||
                    !chamber.Actors.Contains(armor) || !from.InRange(armor, 18) || !from.InLOS(armor)) { return false; }
                chamber.Supplies--; chamber.Killed(armor); armor.Delete(); return true;
            case 3:
                if (node.State != 0 || chamber.Supplies < 1) { return false; }
                chamber.Supplies--; node.State = 1; node.Hue = 0x5A; node.Name = $"Canal {node.Key / 8 + 1}, segment {node.Key % 8 + 1} — connected";
                chamber.Progress++; return true;
            case 4:
                if (chamber.Progress != 16) { from.SendMessage("There are still gaps. Complete both numbered canals first."); return false; }
                chamber.Finish(true); return true;
            case 5:
                if (chamber.Stage == 0)
                {
                    chamber.Stage = 1;
                    for (var i = 0; i < 3; i++) { chamber.Spawn(4, chamber.Original ? 12 + i * 3 : -7 + i * 7, chamber.Original ? 4 : -1); }
                    return true;
                }
                if (chamber.Stage == 1 && chamber.Supplies >= 3)
                { chamber.Stage = 2; chamber.Spawn(5, 0, chamber.Original ? 0 : -9, chamber.Original ? 22 : 12); }
                if (chamber.Stage != 2) { return false; }
                // Wings remain available to every member, so no party member is stranded below.
                var point = chamber.Original ? new Point3D(chamber.X - 5, chamber.Y - 5, chamber.Z + 22) : new Point3D(chamber.X - 3, chamber.Y - 7, chamber.Z + 12);
                if (chamber.Original) { point = HavenOriginalDungeons.SpawnPoint(chamber.Map, point, point, true); }
                BaseCreature.TeleportPets(from, point, chamber.Map); from.MoveToWorld(point, chamber.Map); return true;
            case 9: chamber.Leave(from); return true;
        }
        return false;
    }
    internal static bool Assist(HavenShadowChamber chamber, HavenCompanion companion)
    {
        if (!chamber.Participant(companion) || companion.IsDeadPet || companion.Hits <= 0 || companion.Combatant is Mobile { Alive: true }) { return false; }
        HavenShadowNode next = null; object target = null;
        foreach (var item in chamber.Puzzle)
        {
            if (item is not HavenShadowNode node) { continue; }
            if (node.Kind == 0 || node.Kind == 2 && chamber.Supplies > 0)
            {
                foreach (var actor in chamber.Actors)
                { if (actor.Role == (node.Kind == 0 ? 0 : 2)) { next = node; target = actor; break; } }
            }
            else if (node.Kind == 1 && node.State == 0)
            {
                foreach (var candidate in chamber.Puzzle)
                { if (candidate is HavenShadowNode pair && pair.Kind == 1 && pair.Key == (node.Key ^ 1)) { next = node; target = pair; break; } }
            }
            else if (node.Kind == 3 && node.State == 0 && chamber.Supplies > 0 || node.Kind == 4 && chamber.Progress == 16 ||
                     node.Kind == 5 && (chamber.Stage == 0 || chamber.Stage == 1 && chamber.Supplies >= 3)) { next = node; }
            if (next != null) { break; }
        }
        if (next == null)
        { companion.ControlOrder = OrderType.Guard; companion.ControlTarget = companion.BoundOwner; return false; }
        companion.ControlOrder = OrderType.Stay;
        if (chamber.Original)
        {
            if (Use(chamber, companion, next, target, Utility.RandomDouble())) { return true; }
            // Original rooms contain counters and partitions. Stand where both the source
            // and target can be seen instead of repeatedly stopping behind a fixture.
            for (var radius = 1; radius <= 3; radius++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    for (var dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) { continue; }
                        var p = new Point3D(next.X + dx, next.Y + dy, next.Z);
                        if (!chamber.Map.CanFit(p,16,checkMobiles:false)) { continue; }
                        var eye = new Point3D(p.X,p.Y,p.Z+14);
                        if (!chamber.Map.LineOfSight(eye,new Point3D(next.X,next.Y,next.Z+1))) { continue; }
                        if (target is Mobile victim && (!Utility.InRange(p,victim.Location,next.Kind == 0 ? 22 : 18) ||
                            !chamber.Map.LineOfSight(eye,new Point3D(victim.X,victim.Y,victim.Z+14)))) { continue; }
                        new PathFollower(companion,p).Follow(false,0); return false;
                    }
                }
            }
            return false;
        }
        if (!companion.InRange(next, 2)) { new PathFollower(companion, next).Follow(false, 2); return false; }
        return Use(chamber, companion, next, target, Utility.RandomDouble());
    }
    public static void Initialize()
    {
        CommandSystem.Register("puzzle", AccessLevel.Player, e =>
        {
            foreach (var chamber in HavenShadowChamber.Registry)
            {
                if (chamber.Participant(e.Mobile)) { chamber.OrderPuzzle(e.Mobile, !e.ArgString.Equals("stop", StringComparison.OrdinalIgnoreCase)); return; }
            }
            e.Mobile.SendMessage("Use [puzzle inside a supported dungeon room; [puzzle stop cancels companion assistance.");
        });
    }
}

[SerializationGenerator(0)]
public partial class HavenShadowNode : Item
{
    [SerializableField(0)] private HavenShadowChamber _chamber;
    [SerializableField(1)] private int _kind;
    [SerializableField(2)] private int _key;
    [SerializableField(3)] private int _state;
    [Constructible]
    public HavenShadowNode() : base(0x1E5E) { Movable = false; }
    public override void OnDoubleClick(Mobile from)
    {
        if (Chamber?.Participant(from) != true || !from.InRange(this, 3)) { return; }
        if (Kind is 0 or 1 or 2) { from.Target = new PuzzleTarget(this); }
        else { HavenShadowPuzzles.Use(Chamber, from, this, null); }
    }
    public override void OnDelete() { Chamber = null; base.OnDelete(); }
    private sealed class PuzzleTarget : Target
    {
        private readonly HavenShadowNode _node;
        public PuzzleTarget(HavenShadowNode node) : base(24, false, TargetFlags.None) { _node = node; }
        protected override void OnTarget(Mobile from, object target)
        { if (!HavenShadowPuzzles.Use(_node.Chamber, from, _node, target, Utility.RandomDouble())) { from.SendMessage("That step is not ready, or you need to move closer to the puzzle source."); } }
    }
}

public sealed class HavenShadowMenu : Gump
{
    private readonly HavenShadowChamber _chamber;
    public HavenShadowMenu(Mobile from, HavenShadowChamber chamber = null) : base(60, 60)
    {
        _chamber = chamber; AddBackground(0, 0, 545, 395, 9270); AddLabel(25, 20, 1152, "Shadowguard expeditions");
        if (chamber != null)
        {
            AddLabel(25, 55, 2101, $"{chamber.Room} · supplies {chamber.Supplies} · progress {chamber.Progress}");
            AddHtml(25, 90, 490, 100, HavenShadowPuzzles.Instructions(chamber.Room), true, false);
            AddButton(25, 215, 4005, 4007, 20); AddLabel(65, 215, 1152, "Companion: solve puzzle");
            AddButton(25, 253, 4005, 4007, 21); AddLabel(65, 253, 1152, "Stop puzzle assistance");
            AddButton(25, 291, 4005, 4007, 22); AddLabel(65, 291, 1152, "Exit this room");
        }
        else
        {
            var record = HavenFrontierRecord.Get(from);
            for (var i = 0; i < 6; i++)
            {
                AddButton(25, 58 + i * 43, 4005, 4007, i + 1);
                AddLabel(65, 58 + i * 43, i < 5 && (record.Rooms & 1 << i) != 0 ? 0x59 : 1152,
                    $"{(HavenShadowRoom)i}{(i < 5 && (record.Rooms & 1 << i) != 0 ? " — complete" : "")}");
            }
            AddLabel(25, 321, 2101, "Party leader chooses. Busy rooms preserve their current party.");
        }
        AddButton(418, 352, 4017, 4019, 0); AddLabel(456, 352, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (info.ButtonID == 0) { return; }
        if (_chamber != null)
        {
            if (!_chamber.Participant(from)) { return; }
            if (info.ButtonID == 22) { _chamber.Leave(from); return; }
            if (info.ButtonID is 20 or 21) { _chamber.OrderPuzzle(from, info.ButtonID == 20); }
            from.SendGump(new HavenShadowMenu(from, _chamber)); return;
        }
        if (info.ButtonID is < 1 or > 6) { return; }
        foreach (var chamber in HavenShadowChamber.Registry)
        {
            if ((int)chamber.Room != info.ButtonID - 1) { continue; }
            if (!chamber.Start(from)) { from.SendMessage("Gather your party at the entrance. The room may be occupied, or Roof seals may be missing."); }
            else { from.SendGump(new HavenShadowMenu(from, chamber)); }
            return;
        }
    }
}
