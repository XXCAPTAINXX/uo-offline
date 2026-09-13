using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Engines.Doom;
using Server.HavenPrototype;

public static class SoloDungeonSmoke
{
    public static void Run(PlayerMobile owner, Action<string> report)
    {
        if (Math.Abs(HavenSoloDungeons.ArtifactChance(.2) - .3) > .000001 || HavenSoloDungeons.ArtifactChance(.9) != 1) throw new Exception("Solo chance multiplier or cap incorrect");
        if (Math.Abs(HavenDoomStatus.Chance(0) - .000863316841 * 1.5) > .000000001) throw new Exception("Doom display disagrees with reward multiplier");
        foreach (var weapon in new Server.Items.BaseWeapon[] { new HavenGravefireScimitar(), new HavenGravefireMace() })
        {
            if (weapon.Slayer != Server.Items.SlayerName.Exorcism || weapon.Slayer2 != Server.Items.SlayerName.Silver || weapon.Layer != Layer.OneHanded || HavenAdvancedGear.Find(weapon) == null) throw new Exception("Gravefire weapon missing slayers, shield compatibility or evolution");
            weapon.Delete();
        }
        var knight = new ShadowKnight(); knight.MoveToWorld(owner.Location, owner.Map); knight.Hidden = true; knight.Frozen = true;
        knight.ProcessHavenHiding(DateTime.UtcNow); knight.ProcessHavenHiding(DateTime.UtcNow.AddSeconds(9));
        if (knight.Hidden || knight.Frozen) throw new Exception("Shadow Knight timeout left boss hidden or frozen");
        var detector = new Dog(); detector.MoveToWorld(owner.Location, owner.Map); detector.SetControlMaster(owner); detector.Skills.DetectHidden.Base = 150;
        var secondKnight = new ShadowKnight(); secondKnight.MoveToWorld(owner.Location, owner.Map); secondKnight.Hidden = true; secondKnight.Frozen = true;
        secondKnight.ProcessHavenHiding(DateTime.UtcNow);
        if (secondKnight.Hidden || secondKnight.Frozen) throw new Exception("Pet Detect Hidden failed guaranteed skill check");
        detector.Delete(); knight.Delete(); secondKnight.Delete();
        report("PASS Shadow Knight timed reveal and controlled pet Detect Hidden early reveal clear frozen state");
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var client = new TcpClient(); client.Connect((IPEndPoint)listener.LocalEndpoint);
        var state = new NetState(new SocketState(listener.AcceptSocket(), new byte[4]));
        owner.NetState = state; state.Mobile = owner;
        var device = new HavenDoomControl(); owner.Backpack.DropItem(device);
        var room = new GauntletSpawner();
        try
        {
            if (HavenSoloDungeons.SpawnCount(room, 9) != 1) throw new Exception("Solo default is not one");
            device.OnDoubleClick(owner);
            var menu = state.Gumps.Last();
            menu.OnResponse(state, new RelayInfo(5, new int[0], new TextRelay[0]));
            if (HavenSoloDungeons.Setting != 5 || HavenSoloDungeons.SpawnCount(room, 9) != 1) throw new Exception("Setting changed current room");
            HavenSoloDungeons.StartRoom(room);
            if (HavenSoloDungeons.SpawnCount(room, 1) != 5) throw new Exception("Next room missed selection");
            menu.OnResponse(state, new RelayInfo(100, new int[0], new TextRelay[0]));
            if (HavenSoloDungeons.Setting != 5) throw new Exception("Invalid selection accepted");
            device.MoveToWorld(owner.Location, owner.Map);
            menu.OnResponse(state, new RelayInfo(1, new int[0], new TextRelay[0]));
            if (HavenSoloDungeons.Setting != 5) throw new Exception("Removed device accepted stale response");
            report("PASS solo artifact boost/cap, matching Doom display, encounter selection, next-room application, invalid and stale response rejection");
        }
        finally { device.Delete(); room.Delete(); owner.NetState = null; state.Dispose(); client.Close(); listener.Stop(); }
    }
}
