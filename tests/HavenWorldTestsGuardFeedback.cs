using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsGuardFeedback
{
    public HavenWorldTestsGuardFeedback() => _ = new HavenWorldTests();

    [SkippableFact]
    public void GuardAnnouncesOnceUntilTheStandingOrderChanges()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = CreateOwner();
        var companion = new HavenCompanion { BoundOwner = owner, Name = "Guard feedback companion" };
        var enemy = new Orc();
        using var state = PacketTestUtilities.CreateTestNetState();
        try
        {
            companion.SetControlMaster(owner);
            companion.MoveToWorld(owner.Location, owner.Map);
            enemy.MoveToWorld(new Point3D(owner.X + 2, owner.Y, owner.Z), owner.Map);
            state.Mobile = owner;
            owner.NetState = state;

            Assert.Equal(0, GuardMessages(state, companion));
            companion.ControlOrder = OrderType.Guard;
            Assert.Equal(1, GuardMessages(state, companion));
            Assert.Equal(OrderType.Guard, companion.AIObject.PersistentOrder);

            // Automatic puzzle/companion updates may assign the same order each tick.
            for (var i = 0; i < 12; i++)
            {
                companion.ControlOrder = OrderType.Guard;
            }
            Assert.Equal(1, GuardMessages(state, companion));

            companion.ControlTarget = enemy;
            companion.ControlOrder = OrderType.Attack;
            enemy.Hidden = true;
            Assert.True(companion.AIObject.DoOrderAttack());
            Assert.Equal(OrderType.Guard, companion.ControlOrder);
            Assert.Equal(OrderType.Guard, companion.AIObject.PersistentOrder);
            Assert.Equal(1, GuardMessages(state, companion));

            // Companion recovery and patrol returns also restore Guard directly.
            companion.ControlTarget = enemy;
            companion.ControlOrder = OrderType.Attack;
            companion.ControlTarget = owner;
            companion.ControlOrder = OrderType.Guard;
            Assert.Equal(1, GuardMessages(state, companion));

            companion.ControlTarget = owner;
            companion.ControlOrder = OrderType.Follow;
            companion.ControlOrder = OrderType.Guard;
            Assert.Equal(2, GuardMessages(state, companion));
        }
        finally
        {
            owner.NetState = null;
            state.Mobile = null;
            enemy.Delete();
            companion.Delete();
            owner.Delete();
        }
    }

    [SkippableFact]
    public void HiddenOwnerCombatantCannotRestartTheGuardAttackLoop()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = CreateOwner();
        var companion = new HavenCompanion { BoundOwner = owner };
        var enemy = new Orc();
        try
        {
            companion.SetControlMaster(owner);
            companion.MoveToWorld(owner.Location, owner.Map);
            enemy.MoveToWorld(new Point3D(owner.X + 2, owner.Y, owner.Z), owner.Map);
            companion.ControlOrder = OrderType.Guard;
            owner.Combatant = enemy;

            Assert.True(companion.CanSee(enemy));
            Assert.True(companion.InLOS(enemy));
            Assert.True(companion.CanBeHarmful(enemy, false));
            Assert.True(companion.DefendOwner());
            Assert.Equal(OrderType.Attack, companion.ControlOrder);
            Assert.Same(enemy, companion.ControlTarget);

            enemy.Hidden = true;
            Assert.Same(enemy, owner.Combatant);
            Assert.False(companion.CanSee(enemy));
            Assert.True(companion.AIObject.DoOrderAttack());
            Assert.Equal(OrderType.Guard, companion.ControlOrder);

            for (var i = 0; i < 12; i++)
            {
                Assert.False(companion.DefendOwner());
                Assert.Equal(OrderType.Guard, companion.ControlOrder);
                Assert.Same(owner, companion.ControlTarget);
            }

            enemy.Hidden = false;
            Assert.True(companion.DefendOwner());
            Assert.Equal(OrderType.Attack, companion.ControlOrder);
            Assert.Same(enemy, companion.ControlTarget);
        }
        finally
        {
            enemy.Delete();
            companion.Delete();
            owner.Delete();
        }
    }

    private static PlayerMobile CreateOwner()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100 };
        owner.AddItem(new Backpack());
        owner.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel);
        return owner;
    }

    private static int GuardMessages(NetState state, HavenCompanion companion)
    {
        var expected = new MessageLocalized(
            Serial.MinusOne, -1, MessageType.Regular, 0x3B2, 3, 1049671, "System", companion.Name
        ).Compile();
        ReadOnlySpan<byte> remaining = state.SendBuffer.GetReadSpan();
        var count = 0;
        int offset;
        while ((offset = remaining.IndexOf(expected)) >= 0)
        {
            count++;
            remaining = remaining[(offset + expected.Length)..];
        }
        return count;
    }
}
