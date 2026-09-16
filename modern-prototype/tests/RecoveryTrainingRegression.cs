using System;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

// Run only in an isolated Haven test world. The caller must not save fixtures.
public static class RecoveryTrainingRegression
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Run(Action<string> report)
    {
        HavenRecovery.Ensure();
        var owner = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100 };
        owner.AddItem(new Backpack());
        new Account("recovery-check-" + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"))[0] = owner;
        var steward = HavenRecovery.Steward();
        owner.MoveToWorld(steward.Location, steward.Map);
        var helmet = new NorseHelm();
        var older = new Corpse(owner, new List<Item> { helmet });
        var newest = new Corpse(owner, new List<Item>());
        var heavy = new Bag { Weight = 1000 };
        older.DropItem(heavy);
        older.DropItem(helmet);
        older.MoveToWorld(new Point3D(1015, 527, -65), Map.Malas);
        newest.MoveToWorld(new Point3D(1015, 527, -65), Map.Malas);
        owner.Corpse = newest;
        try
        {
            Require(HavenRecovery.RecallCorpses(owner) == 2, "Older corpse was missed");
            Require(older.Map == owner.Map && newest.Map == owner.Map && heavy.Parent == older, "Recall lost contents");
            report("PASS recall finds older belongings even when the newest corpse is empty");
            older.Open(owner, true);
            Require(heavy.Parent == older, "Overweight bag should stay on corpse");
            Require(helmet.Parent == owner, "A heavy bag blocked later wearable equipment");
            report("PASS overweight bag stays on corpse while later equipment is recovered");
            var pet = new Dog();
            try
            {
                pet.SetControlMaster(owner); pet.IsBonded = true; pet.MoveToWorld(owner.Location, owner.Map);
                pet.Loyalty = 1; pet.Loyalty -= 10;
                Require(pet.Loyalty == 100, "Bonded pet lost loyalty");
                pet.IsBonded = false; pet.Loyalty = 100; pet.Loyalty -= 10;
                Require(pet.Loyalty == 90, "Unbonded loyalty decay changed");
                pet.MoveToWorld(new Point3D(1015, 527, -65), Map.Malas);
                Require(HavenPetTrainingMenu.UseFailure(owner, pet).Contains("within 12 tiles"), "Remote training silently rejected");
                report("PASS bonded loyalty protection and explicit remote-training rejection");
            }
            finally { pet.Delete(); }
        }
        finally { older.Delete(); newest.Delete(); helmet.Delete(); owner.Delete(); }
    }
}
