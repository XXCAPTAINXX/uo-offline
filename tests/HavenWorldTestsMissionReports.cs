using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMissionReports
{
    public HavenWorldTestsMissionReports() { _=new HavenWorldTests(); }
    [SkippableTheory]
    [InlineData(HavenExpeditionKind.MalasReagents)]
    [InlineData(HavenExpeditionKind.DoomBones)]
    [InlineData(HavenExpeditionKind.AbyssEssences)]
    [InlineData(HavenExpeditionKind.AbyssIngredients)]
    public void EveryRegionalRewardCombinesAndWithdrawsFromTheResourceBook(HavenExpeditionKind kind)
    {
        TileDataRequirement.SkipIfMissing();var owner=new PlayerMobile();owner.AddItem(new Backpack());
        var book=new HavenResourceLedger();owner.Backpack.DropItem(book);
        var loot=HavenCompanionExpedition.CreateLoot(kind,5);owner.Backpack.DropItem(loot);
        try
        {
            Assert.NotEmpty(loot.Items);
            foreach(var item in loot.Items.ToArray())
            {
                var deed=Assert.IsType<CommodityDeed>(item);var resource=deed.Commodity;
                var type=resource.GetType();var amount=resource.Amount;var index=HavenResourceCatalog.Index(resource);
                Assert.True(index>=0);owner.Backpack.DropItem(deed);Assert.True(book.Absorb(owner,deed));
                var second=new CommodityDeed();Assert.True(second.SetCommodity(HavenResourceCatalog.Entries[index].Create(amount)));owner.Backpack.DropItem(second);
                Assert.True(book.Absorb(owner,second));Assert.Equal(amount*2,book.Balance(index));
                Assert.True(book.Withdraw(owner,index,amount+1));
                var extracted=owner.Backpack.FindItemByType<CommodityDeed>();Assert.Equal(type,extracted.Commodity.GetType());Assert.Equal(amount+1,extracted.Commodity.Amount);
                Assert.Equal(index,HavenResourceCatalog.Index(extracted.Commodity));Assert.Equal(amount-1,book.Balance(index));extracted.Delete();
            }
        }
        finally {owner.Delete();}
    }
    [Fact]
    public void RegionalSkillsAndSavedMissionIdsStayCompatible()
    {
        var companion=new HavenCompanion();
        try
        {
            Assert.Equal(16,(int)HavenExpeditionKind.TameStormhorn);Assert.Equal(17,(int)HavenExpeditionKind.MalasReagents);
            companion.Skills.Tactics.Base=100;companion.Skills.MagicResist.Base=79;
            Assert.False(HavenRegionalMissions.CanStart(companion,HavenExpeditionKind.AbyssEssences));
            companion.Skills.MagicResist.Base=100;Assert.True(HavenRegionalMissions.CanStart(companion,HavenExpeditionKind.AbyssIngredients));
            Assert.All(HavenRegionalMissions.Kinds,k=>Assert.False(HavenTamingMissions.IsTaming(k)));
        }
        finally {companion.Delete();}
    }
    [SkippableFact]
    public void CompletedMissionReportsExactDeedContentsAndSurvivesReload()
    {
        TileDataRequirement.SkipIfMissing();var owner=new PlayerMobile { Player=true,Body=400 };owner.AddItem(new Backpack());
        var companion=new HavenCompanion { BoundOwner=owner };
        owner.MoveToWorld(HavenRecovery.BankLocation,Map.Trammel);companion.MoveToWorld(owner.Location,owner.Map);
        var copy=new HavenMissionJournal(World.NewItem);
        try
        {
            Assert.True(HavenCompanionExpedition.Start(companion,owner,HavenExpeditionKind.Ore));
            var trip=companion.Expedition;Assert.True(trip.Return(owner,trip.Due));
            var journal=HavenMissionJournal.Find(companion);var report=Assert.Single(journal.Reports);
            Assert.Equal(1,report.Runs);Assert.Equal(5,report.Minutes);Assert.Equal(100,report.Loot.Sum(x=>x.Amount));
            Assert.All(report.Loot,x=>Assert.Contains("resource deed",x.Name));Assert.False(journal.Active);Assert.True(journal.Unread);
            Assert.True(report.After.Str>report.Before.Str);Assert.NotEmpty(report.After.Gear);Assert.NotEmpty(report.After.Skills);
            var writer=new BufferWriter(true);journal.Serialize(writer);copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0,(int)writer.Position).ToArray()));
            Assert.Equal(journal.Data,copy.Data);Assert.True(copy.Unread);Assert.False(copy.MayShow(new PlayerMobile()));
            Assert.False(trip.Return(owner,trip.Due));Assert.Equal(1,journal.Reports[0].Runs);
            _=new HavenMissionReportGump(journal,1);_=new HavenMissionReportGump(journal,3);
        }
        finally {copy.Delete();companion.Delete();owner.Delete();}
    }
    [Fact]
    public void OfflineRunsAggregateAndReturnKeepsAReadableHistory()
    {
        var owner=new PlayerMobile();var companion=new HavenCompanion { BoundOwner=owner };
        try
        {
            var job=HavenCompanionGearAssignment.Begin(companion,Core.Now);
            job.Tick(Core.Now+TimeSpan.FromMinutes(5),false);job.Tick(Core.Now+TimeSpan.FromMinutes(10),false);
            var journal=HavenMissionJournal.Find(companion);Assert.Single(journal.Reports);Assert.Equal(2,journal.Reports[0].Runs);
            Assert.Equal(10,journal.Reports[0].Loot.Where(x=>x.Name.Contains("mark",StringComparison.OrdinalIgnoreCase)).Sum(x=>x.Amount));
            job.Stop("Returned");Assert.False(journal.Active);Assert.True(journal.Unread);
            HavenMissionJournal.Begin(companion,Core.Now+TimeSpan.FromMinutes(15));Assert.Equal(2,journal.Reports.Count);Assert.Equal(2,journal.Reports[1].Runs);
        }
        finally {companion.Delete();owner.Delete();}
    }
}
