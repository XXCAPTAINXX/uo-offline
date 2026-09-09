using System;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenPlayerStats
{
    public const int BaseCap = 300;
    private const int OriginalBaseCap = 225;
    public static bool IsPlayer(Mobile mobile) => mobile is PlayerMobile and not Server.CustomBots.PlayerBot;

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void Apply(PlayerMobile player)
    {
        if (IsPlayer(player) && player.StatCap < BaseCap)
        {
            // Existing scroll and veteran increases are already part of the stored cap.
            player.StatCap = Math.Max(OriginalBaseCap, player.StatCap) + BaseCap - OriginalBaseCap;
        }
    }

    public static int ScrollCap(Mobile mobile, int storedValue) =>
        storedValue + (IsPlayer(mobile) ? BaseCap - OriginalBaseCap : 0) +
        (mobile is PlayerMobile { HasStatReward: true } ? 5 : 0);
}
