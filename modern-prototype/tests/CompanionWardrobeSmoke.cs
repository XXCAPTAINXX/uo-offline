using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.Network;
using Server.HavenPrototype;
public static class CompanionWardrobeSmoke
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run(Action<string> log)
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100 };
        owner.AddItem(new Backpack()); new Account("wardrobe-" + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"))[0] = owner;
        owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
        var c = new HavenCompanion { Female = true, Body = 0x191 };
        typeof(HavenCompanion).GetField("_owner", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(c,owner);
        c.SetControlMaster(owner); c.MoveToWorld(new Point3D(owner.X+5,owner.Y,owner.Z),owner.Map);
        var stranger = new PlayerMobile(); stranger.MoveToWorld(owner.Location,owner.Map);
        try
        {
            c.FindItemOnLayer(Layer.InnerTorso).Delete(); c.FindItemOnLayer(Layer.Pants).Delete();
            c.EnsureWardrobe(); var fallback = c.FindItemOnLayer(Layer.OuterTorso) as FancyDress;
            Check(fallback != null && fallback.Movable && fallback.MaxHitPoints == 0,"Missing durable removable fallback");
            int count = c.Items.Count; c.EnsureWardrobe(); Check(c.Items.Count == count,"Duplicate fallback");
            Check(c.AllowEquipFrom(owner) && !c.AllowEquipFrom(stranger),"Equipment ownership check failed");
            Check(HavenCompanionAccess.InventoryRange(owner,fallback) == HavenCompanion.SupportRange,"Worn item range differs from pack");
            bool rejected; LRReason reason; owner.Lift(fallback,1,out rejected,out reason);
            Check(!rejected && owner.Holding == fallback,"Native paperdoll lift failed: " + reason);
            owner.Holding = null; owner.Backpack.DropItem(fallback);
            var custom = new FancyDress(0x489); owner.Backpack.DropItem(custom);
            Check(c.EquipItem(custom) && custom.Parent == c,"Native equip failed");
            c.EnsureWardrobe(); Check(c.FindItemOnLayer(Layer.OuterTorso) == custom,"Custom outfit replaced");
            Check(c.SetRole(owner,CompanionRole.Bard) && custom.Parent == c,"Role change stripped outfit");
            c.Kill(); Check(c.IsDeadPet && custom.Parent == c,"Death lost outfit");
            c.ResurrectPet(); c.EnsureWardrobe(); Check(custom.Parent == c,"Resurrection lost outfit");
            log("PASS naked companion gets durable outfit once; owner native paperdoll lift at five tiles and equip; stranger denied; custom dress retained through role change, death and resurrection");
        }
        finally { c.Delete(); owner.Delete(); stranger.Delete(); }
    }
}
