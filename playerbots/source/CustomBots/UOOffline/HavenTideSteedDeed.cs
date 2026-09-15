using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenTideSteedDeed : Item
{
    [Constructible] public HavenTideSteedDeed() : base(0x14F0) { Name = "Tidebound sea horse charter"; Hue = 0x482; Weight = 1; LootType = LootType.Blessed; }
    internal bool Claim(Mobile from)
    {
        if (Deleted || from?.Backpack == null || !from.Alive || from.Map == null || from.Map == Map.Internal || !IsChildOf(from.Backpack) || from.Followers + 1 > from.FollowersMax) { return false; }
        var steed = new HavenTideSteed();
        if (!steed.SetControlMaster(from)) { steed.Delete(); return false; }
        steed.BondingBegin = Core.Now - TimeSpan.FromDays(8); steed.MoveToWorld(from.Location, from.Map); Delete(); return true;
    }
    public override void OnDoubleClick(Mobile from)
    { from.SendMessage(Claim(from) ? "Your sea horse is ready to bond when fed. Mount it, then use [tide for cargo and SOS navigation." : "Keep the charter in your pack and leave one follower slot free."); }
}
