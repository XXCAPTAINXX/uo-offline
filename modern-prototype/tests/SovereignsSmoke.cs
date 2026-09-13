using System;
using Server;
using Server.Accounting;
using Server.Mobiles;
using Server.HavenPrototype;
using Server.Engines.UOStore;

public static class SovereignsSmoke
{
    public static void Run(Mobile location, Action<string> report)
    {
        var account = new Account("sovereign-test-" + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"));
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        account[0] = player;
        player.MoveToWorld(location.Location, location.Map);
        player.Skills.Magery.Base = 100;
        HavenSovereigns.CheckProgress(player);
        int balance = account.Sovereigns;
        if (balance < 55 || UltimaStore.GetCurrency(player) != balance) throw new Exception("Milestones do not fund native store");
        HavenSovereigns.CheckProgress(player);
        if (account.Sovereigns != balance) throw new Exception("Repeated milestones duplicate currency");
        var alt = new PlayerMobile { Player = true, Body = 0x190 };
        account[1] = alt; alt.MoveToWorld(player.Location, player.Map); alt.Skills.Magery.Base = 100;
        HavenSovereigns.CheckProgress(alt);
        if (account.Sovereigns != balance) throw new Exception("Alternate character repeats rewards");
        var boss = new Ogre(); boss.SetHits(5000); boss.Karma = -1000; boss.MoveToWorld(player.Location, player.Map);
        HavenSovereigns.OnMonsterKilled(boss, player);
        if (account.Sovereigns != balance + 60) throw new Exception("Boss and first kill rewards incorrect");
        HavenSovereigns.OnMonsterKilled(boss, player);
        if (account.Sovereigns != balance + 60) throw new Exception("Duplicate kill reward");
        var pet = new Ogre(); pet.SetHits(5000); pet.Karma = -1000; pet.MoveToWorld(player.Location, player.Map); pet.SetControlMaster(player);
        HavenSovereigns.OnMonsterKilled(pet, player);
        if (account.Sovereigns != balance + 60) throw new Exception("Pet farm awarded currency");
        UltimaStore.DeductCurrency(player, 10);
        if (account.Sovereigns != balance + 50) throw new Exception("Store does not spend earned currency");
        pet.Delete(); boss.Delete(); alt.Delete(); player.Delete();
        report("PASS Sovereigns: native store funding/spending, milestone backfill, account-wide deduplication, boss rewards, duplicate kill and pet protections");
    }
}
