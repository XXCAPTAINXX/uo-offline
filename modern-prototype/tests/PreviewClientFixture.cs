using System;
using System.IO;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

// Local client QA only. Credential input is never checked in or logged.
public static class PreviewClientFixture
{
    public static void Initialize()
    {
        if (File.Exists("PREVIEW-CLIENT-FIXTURE")) EventSink.ServerStarted += () => Timer.DelayCall(TimeSpan.FromSeconds(2), Create);
    }
    private static void Create()
    {
        if (!HavenPreview.Enabled || !File.Exists("preview-world-ready.txt")) throw new InvalidOperationException("Client fixture requires the isolated ready preview.");
        string[] credentials = File.ReadAllLines("PREVIEW-CLIENT-FIXTURE");
        if (credentials.Length != 2 || Accounts.GetAccount(credentials[0]) != null) throw new InvalidOperationException("Refusing to replace an existing account.");
        var player = new PlayerMobile { Name = "Haven Explorer", Player = true, Body = 0x190, Hue = 0x83EA, AccessLevel = AccessLevel.Player };
        player.AddItem(new Backpack { Movable = false }); player.AddItem(new Shirt()); player.AddItem(new LongPants()); player.AddItem(new Boots());
        var account = new Account(credentials[0], credentials[1]); account[0] = player;
        player.MoveToWorld(new Point3D(3506,2570,14), Map.Trammel);
        player.LogoutLocation = player.Location; player.LogoutMap = player.Map;
        World.Save(false,false);
        File.Delete("PREVIEW-CLIENT-FIXTURE");
        File.WriteAllText("preview-client-ready.txt", "Player-level client QA character created and saved.");
    }
}
