using System;
using System.IO;
using System.Linq;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class PreviewSmoke
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run(Action<string, Action> check, bool reload)
    {
        if (reload)
        {
            check("preview kit claim survives save and restart", () => {
                var player = World.FindMobile((Serial)Int32.Parse(File.ReadAllText("preview-fixture.txt")));
                player.MoveToWorld(new Point3D(3506,2570,14), Map.Trammel);
                int count = player.Backpack.TotalItems;
                Require(!HavenPreview.Prepare(player) && player.Backpack.TotalItems == count, "Claim replayed after restart");
                Require(player.Backpack.FindItemsByType(typeof(BankCheck), true).Length == 1, "Missing or duplicate check");
            });
            return;
        }
        check("preview conveniences disabled without explicit marker", () => Require(!HavenPreview.Enabled, "Preview enabled by default"));
        File.WriteAllText("HAVEN-INTERACTIVE-PREVIEW", "Disposable smoke test");
        var owner = new PlayerMobile { Name = "preview controls fixture", Player = true, Body = 0x190 };
        owner.RawStr = owner.RawDex = owner.RawInt = 100;
        owner.AddItem(new Backpack());
        var account = new Account("preview-controls-fixture", Guid.NewGuid().ToString("N")); account[0] = owner;
        owner.MoveToWorld(new Point3D(3506,2570,14), Map.Trammel);
        foreach (var destination in HavenPreview.Destinations)
        {
            var captured = destination;
            check("preview landing fits modern map: " + captured.Name, () => {
                Point3D landing; Require(HavenPreview.FindLanding(captured, out landing), "No walkable landing");
            });
        }
        check("full backpack rejects entire preview kit", () => {
            for (int i = 0; i < 125; i++) owner.Backpack.DropItem(new Dagger());
            int count = owner.Backpack.TotalItems;
            Require(!HavenPreview.Prepare(owner) && owner.Backpack.TotalItems == count && account.GetTag("HavenPreview.Kit:" + owner.Serial.Value) == null, "Partial claim");
            foreach (var item in owner.Backpack.Items.ToArray()) item.Delete();
        });
        check("preview kit uses native leech gear and skill caps", () => {
            Require(HavenPreview.Prepare(owner), "Kit failed");
            var sword = owner.Backpack.FindItemByType(typeof(Broadsword)) as Broadsword;
            Require(sword != null && sword.WeaponAttributes.HitLeechMana == 60 && owner.Skills.Parry.Base == 120 && owner.Skills.Parry.Cap == 120, "Wrong kit");
        });
        check("preview kit cannot be claimed twice", () => {
            int count = owner.Backpack.TotalItems;
            Require(!HavenPreview.Prepare(owner) && owner.Backpack.TotalItems == count, "Duplicate kit");
        });
        check("preview travel rejects active combat and invalid destinations", () => {
            owner.Combatant = new Orc();
            Require(!HavenPreview.Travel(owner,0), "Combat travel allowed");
            ((Mobile)owner.Combatant).Delete(); owner.Combatant = null;
            Require(!HavenPreview.Travel(owner,-1) && !HavenPreview.Travel(owner,100), "Invalid index accepted");
        });
        check("preview travel changes facet", () => Require(HavenPreview.Travel(owner,1) && owner.Map == Map.Malas, "Travel failed"));
        File.WriteAllText("preview-fixture.txt",owner.Serial.Value.ToString());
    }
}
