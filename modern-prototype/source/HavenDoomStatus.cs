using System;
using Server.Commands;
using Server.Engines.Points;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenDoomStatus
    {
        // Matches DoomGauntlet.ProcessKill: points are awarded before each roll.
        public static double Chance(double points)
        {
            return Math.Min(1.0, 0.000863316841 * Math.Pow(10, 0.00000425531915 * Math.Max(0, points)));
        }

        public static void Initialize()
        {
            CommandSystem.Register("doom", AccessLevel.Player, e =>
            {
                var player = e.Mobile as PlayerMobile;
                if (!HavenPreview.Enabled || player == null) return;
                double points = PointsSystem.DoomGauntlet.GetPoints(player);
                player.SendMessage(0x482, "Doom artifact points: " + points.ToString("N0") + ". Chance at this point total: " + (Chance(points) * 100).ToString("0.00") + "%.");
                player.SendMessage("Your next qualifying boss adds points before rolling. Luck increases points earned; points reset when an artifact is awarded.");
                player.SendMessage("This is a chance per eligible boss kill, not a fixed number of kills until a drop. Use [doom again to refresh.");
            });
        }
    }
}
