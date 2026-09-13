using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.HavenPrototype;

public static class PetTrainingButtonsSmoke
{
    public static void Run(PlayerMobile owner, BaseCreature pet, Action<string> report)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var client = new TcpClient(); client.Connect((IPEndPoint)listener.LocalEndpoint);
        var state = new NetState(new SocketState(listener.AcceptSocket(), new byte[4]));
        owner.NetState = state; state.Mobile = owner;
        int points = PetTrainingHelper.GetTrainingProfile(pet, true).TrainingPoints;
        try
        {
            for (int row = 0; row < 10; row++)
            {
                var menu = new HavenPetTrainingGump(pet);
                var buttons = menu.Entries.OfType<GumpButton>().Where(b => b.ButtonID == 100 + row).ToArray();
                if (buttons.Length != 1 || buttons[0].NormalID != 10800) throw new Exception("Upgrade needs one native framed button");
                owner.SendGump(menu);
                // Route a client-format 0xB1 response through the real packet handler.
                byte[] packet = new byte[23]; packet[0] = 0xB1; packet[2] = 23;
                Write(packet, 3, menu.Serial); Write(packet, 7, menu.TypeID); Write(packet, 11, 100 + row);
                PacketHandlers.DisplayGumpResponse(state, new PacketReader(packet, packet.Length, false));
                if (!state.Gumps.OfType<HavenPetUpgradeGump>().Any()) throw new Exception("Upgrade row did not open confirmation: " + row);
                owner.CloseGump(typeof(HavenPetUpgradeGump));
            }
            if (PetTrainingHelper.GetTrainingProfile(pet, true).TrainingPoints != points) throw new Exception("Opening confirmation spent points");
            report("PASS all ten native framed upgrade buttons route through client packet handler to confirmation without spending points");
        }
        finally { owner.NetState = null; state.Dispose(); client.Close(); listener.Stop(); }
    }
    static void Write(byte[] data, int offset, int value) { for (int i = 0; i < 4; i++) data[offset + i] = (byte)(value >> (24 - i * 8)); }
}


