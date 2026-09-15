using Server;
using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsSharedBags
{
    public HavenWorldTestsSharedBags() => _ = new HavenWorldTests();
    [SkippableFact]
    public void NestedCompanionBagsOpenWithoutSnoopChecksAndRejectOtherPlayers()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190, Karma = 100, Hidden = true };
        var stranger = new PlayerMobile();
        var pet = new HavenCompanion { BoundOwner = owner };
        var outer = new Bag(); var inner = new Bag();
        try
        {
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            pet.MoveToWorld(owner.Location, owner.Map); stranger.MoveToWorld(owner.Location, owner.Map);
            pet.Backpack.DropItem(outer); outer.DropItem(inner);
            var karma = owner.Karma;
            var skill = owner.Skills.Snooping.Base;
            Snooping.Container_Snoop(inner, owner);
            Assert.Contains(owner, inner.Openers);
            Assert.Equal(karma, owner.Karma); Assert.Equal(skill, owner.Skills.Snooping.Base); Assert.True(owner.Hidden);
            Snooping.Container_Snoop(inner, stranger);
            Assert.DoesNotContain(stranger, inner.Openers);
            inner.Openers.Clear();
            owner.MoveToWorld(new Point3D(3521, 2575, 14), Map.Trammel);
            Snooping.Container_Snoop(inner, owner);
            Assert.Empty(inner.Openers);
        }
        finally { pet.Delete(); owner.Delete(); stranger.Delete(); }
    }
    [SkippableFact]
    public void SnoopingDoesNotConsumePlayerSkillBudget()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile();
        try
        {
            owner.Skills.Snooping.Base = 100;
            owner.Skills.Magery.Base = 80;
            Assert.True(HavenFreeSkills.IsFree(owner, owner.Skills.Snooping));
            Assert.Equal(800, HavenFreeSkills.CountedTotal(owner));
        }
        finally { owner.Delete(); }
    }
}
