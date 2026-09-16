using Server.Commands;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenNewcomerLuck
{
    public const int Bonus = 1000;
    public static bool IsInArea(Map map, Point3D location) => map == Map.Trammel &&
        location.X >= 3314 && location.X < 3814 && location.Y >= 2345 && location.Y < 3095;
    public static int GetBonus(Mobile mobile) => mobile is PlayerMobile and not Server.CustomBots.PlayerBot &&
        IsInArea(mobile.Map, mobile.Location) ? Bonus : 0;
    public static void OnMoved(PlayerMobile player, Map oldMap, Point3D oldLocation)
    {
        if (player is Server.CustomBots.PlayerBot || IsInArea(oldMap, oldLocation) == IsInArea(player.Map, player.Location)) { return; }
        player.Delta(MobileDelta.Stat);
        player.SendMessage(GetBonus(player) > 0 ? "Haven Island bonus: +1,000 Luck while you remain here." : "You left Haven Island. The +1,000 Luck bonus has ended.");
    }
    public static void Initialize() => CommandSystem.Register("HavenLuck", AccessLevel.Player, e =>
        e.Mobile.SendMessage($"Total Luck: {e.Mobile.Luck:N0}. Haven Island bonus: {GetBonus(e.Mobile)}."));
}

