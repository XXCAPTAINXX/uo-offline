using System;
using Server;
using Server.Items;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsWeaponReequip
{
    public HavenWorldTestsWeaponReequip() => _ = new HavenWorldTests();
    [SkippableTheory]
    [InlineData(HavenCompanionRole.Fighter)]
    [InlineData(HavenCompanionRole.Archer)]
    [InlineData(HavenCompanionRole.Caster)]
    public void RestoresSameArmsWithoutChangingRole(HavenCompanionRole role)
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion { Role = role };
        try
        {
            var now = Core.Now;
            companion.ConfigureCombatRole(now);
            var arm = companion.FindItemOnLayer(role == HavenCompanionRole.Archer ? Layer.TwoHanded : Layer.OneHanded);
            Assert.NotNull(arm);
            companion.Backpack.DropItem(arm);
            var count = companion.Backpack.TotalItems;
            companion.ConfigureCombatRole(now + TimeSpan.FromSeconds(3));
            Assert.Same(companion, arm.Parent);
            Assert.Equal(count - 1, companion.Backpack.TotalItems);
            Assert.Equal(role, companion.Role);
        }
        finally { companion.Delete(); }
    }

    [SkippableFact]
    public void RearmWaitsForDisarmLockAndDoesNotCreateDuplicateWeapons()
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion();
        try
        {
            var now = Core.Now;
            companion.ConfigureCombatRole(now);
            var arm = companion.FindItemOnLayer(Layer.OneHanded);
            companion.Backpack.DropItem(arm);
            Assert.True(companion.BeginAction(typeof(BaseWeapon)));
            var count = companion.Backpack.TotalItems;
            companion.ConfigureCombatRole(now + TimeSpan.FromSeconds(3));
            Assert.Same(companion.Backpack, arm.Parent);
            Assert.Equal(count, companion.Backpack.TotalItems);
            companion.EndAction(typeof(BaseWeapon));
            companion.ConfigureCombatRole(now + TimeSpan.FromSeconds(6));
            Assert.Same(companion, arm.Parent);
        }
        finally { companion.EndAction(typeof(BaseWeapon)); companion.Delete(); }
    }
}
