using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Engines.Doom;

namespace Server.HavenPrototype
{
    public static class HavenSoloDungeons
    {
        public static double ArtifactChance(double chance) { return Math.Min(1, chance * (HavenPreview.Enabled ? 1.5 : 1)); }
        sealed class RoomSize { public int Count; }
        static readonly ConditionalWeakTable<GauntletSpawner, RoomSize> Rooms = new ConditionalWeakTable<GauntletSpawner, RoomSize>();
        public static int SpawnCount(GauntletSpawner spawner, int original)
        {
            if (!HavenPreview.Enabled) return original;
            return Rooms.GetValue(spawner, s => new RoomSize { Count = Setting }).Count;
        }
        public static void StartRoom(GauntletSpawner spawner) { Rooms.Remove(spawner); }
        public static int Setting
        {
            get { var device = World.Items.Values.OfType<HavenDoomControl>().Where(x => !x.Deleted && x.Count >= 1).OrderBy(x => x.Serial.Value).FirstOrDefault(); return device == null ? 1 : device.Count; }
        }
        public static void Initialize()
        {
            CommandSystem.Register("doomcontrol", AccessLevel.Player, e => {
                if (!HavenPreview.Enabled || e.Mobile.Backpack == null) return;
                var device = e.Mobile.Backpack.FindItemByType(typeof(HavenDoomControl)) as HavenDoomControl;
                if (device == null) { device = new HavenDoomControl(); if (!e.Mobile.PlaceInBackpack(device)) { device.Delete(); return; } }
                device.OnDoubleClick(e.Mobile);
            });
        }
    }
    public class HavenDoomControl : Item
    {
        public int Count { get; private set; }
        [Constructable] public HavenDoomControl() : base(0x1F1C) { Name = "Doom encounter control"; Hue = 0x48D; Weight = 1; LootType = LootType.Blessed; Count = HavenSoloDungeons.Setting; }
        public HavenDoomControl(Serial serial) : base(serial) { }
        public override void OnDoubleClick(Mobile from) { if (HavenPreview.Enabled && IsChildOf(from.Backpack)) { from.CloseGump(typeof(ControlMenu)); from.SendGump(new ControlMenu(this)); } }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Count); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Count = Math.Max(1, Math.Min(5, r.ReadInt())); }
        class ControlMenu : Gump
        {
            readonly HavenDoomControl _device;
            public ControlMenu(HavenDoomControl device) : base(100, 100)
            {
                _device = device; AddBackground(0, 0, 440, 300, 5054);
                AddLabel(25, 25, 0, "Doom encounter size: " + HavenSoloDungeons.Setting);
                AddLabel(25, 55, 0, "Shared gauntlet setting; applies to the next room.");
                AddLabel(25, 80, 0, "Current bosses and earned artifact points are preserved.");
                for (int i = 1; i <= 5; i++) { AddButton(30, 110 + (i - 1) * 30, 2446, 2446, i, GumpButtonType.Reply, 0); AddLabel(48, 112 + (i - 1) * 30, 0, i + (i == 1 ? " boss per room" : " bosses per room")); }
            }
            public override void OnResponse(NetState state, RelayInfo info)
            {
                var from = state.Mobile;
                if (!HavenPreview.Enabled || from == null || _device.Deleted || !_device.IsChildOf(from.Backpack) || info.ButtonID < 1 || info.ButtonID > 5) return;
                foreach (var device in World.Items.Values.OfType<HavenDoomControl>().Where(x => !x.Deleted)) device.Count = info.ButtonID;
                from.SendMessage(0x48D, "Doom set to " + info.ButtonID + " bosses per room, starting with the next room.");
                _device.OnDoubleClick(from);
            }
        }
    }
}
