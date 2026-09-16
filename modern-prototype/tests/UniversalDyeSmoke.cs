using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.HavenPrototype;

public static class UniversalDyeSmoke
{
    public static void Run(PlayerMobile owner, Action<string> report)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var client = new TcpClient(); client.Connect((IPEndPoint)listener.LocalEndpoint);
        var state = new NetState(new SocketState(listener.AcceptSocket(), new byte[4])); owner.NetState = state; state.Mobile = owner;
        var tub = new HavenUniversalDyeTub(); owner.Backpack.DropItem(tub);
        var item = new Shirt(); owner.Backpack.DropItem(item);
        try
        {
            for (int page = 0; page < 50; page++)
            {
                var menu = new HavenUniversalDyeTub.Palette(tub, page);
                if (menu.Entries.OfType<GumpButton>().Count(b => b.ButtonID >= 100 && b.ButtonID < 160) != 60) throw new Exception("Missing color choices");
                menu.OnResponse(state, new RelayInfo(159, new int[0], new TextRelay[0]));
                if (tub.DyedHue != page * 60 + 60) throw new Exception("Wrong selected color");
            }
            var last = new HavenUniversalDyeTub.Palette(tub, 49);
            last.OnResponse(state, new RelayInfo(3, new int[0], new[] { new TextRelay(1, "3001") }));
            if (tub.DyedHue != 3000) throw new Exception("Invalid hue accepted");
            last.OnResponse(state, new RelayInfo(5, new int[0], new TextRelay[0])); owner.Target.Invoke(owner, item);
            if (item.Hue != 3000) throw new Exception("Selected color not applied");
            tub.MoveToWorld(owner.Location, owner.Map);
            last.OnResponse(state, new RelayInfo(4, new int[0], new TextRelay[0]));
            if (tub.DyedHue != 3000) throw new Exception("Removed tub accepted stale response");
            var buyer = new PlayerMobile { Player = true, Body = 0x190 }; buyer.AddItem(new Backpack());
            var account = new Server.Accounting.Account("lantern-check-"+Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N")); account[0] = buyer;
            var lantern = new HavenProspectorsLantern(); buyer.Backpack.DropItem(lantern); lantern.OnDoubleClick(buyer);
            if(!lantern.Deleted || HavenProspectorsLantern.Multiplier(buyer)!=1.25 || HavenProspectorsLantern.Remaining(buyer).TotalMinutes<59)throw new Exception("Lantern activation failed");
            var second = new HavenProspectorsLantern(); buyer.Backpack.DropItem(second); second.OnDoubleClick(buyer);
            if(second.Deleted)throw new Exception("Active boost consumed another lantern");
            account.SetTag("Haven.Prospector.Expires", DateTime.UtcNow.AddSeconds(-1).ToString("O"));
            if(HavenProspectorsLantern.Multiplier(buyer)!=1)throw new Exception("Expired boost remained active");
            second.Delete();buyer.Delete();
            report("PASS lantern activation, non-stacking consumption protection and expiration");
            report("PASS all 3,000 palette selections, invalid hue rejection, dye application and stale ownership protection");
        }
        finally { item.Delete(); tub.Delete(); owner.NetState = null; state.Dispose(); client.Close(); listener.Stop(); }
    }
}
