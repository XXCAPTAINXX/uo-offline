using System;
using System.Collections.Generic;
namespace Server.HavenPrototype
{
    public static class HavenGatheringMissions
    {
        static readonly double[] Metal = {0,65,70,75,80,85,90,95,99};
        static readonly double[] Wood = {0,65,80,95,100,100,100};
        static readonly double[] Leather = {0,65,80,100};
        public static int Amount(CompanionMission kind, int minutes)
        {
            return minutes * (kind == CompanionMission.Lumber ? 40 : 20) * HavenRegionalMissions.Bonus(minutes) / 100;
        }
        public static void Prepare(HavenCompanion companion, CompanionMission kind, int minutes, Dictionary<int,int> rewards, double roll)
        {
            var levels = kind == CompanionMission.Mining ? Metal : kind == CompanionMission.Lumber ? Wood : Leather;
            int basic = kind == CompanionMission.Mining ? 0 : kind == CompanionMission.Lumber ? 9 : 16;
            double skill = kind == CompanionMission.Mining ? companion.Skills.Mining.Base : kind == CompanionMission.Lumber ? companion.Skills.Lumberjacking.Base : Math.Min(companion.Skills.Wrestling.Base, companion.Skills.Tactics.Base);
            int highest = 0;
            while (highest + 1 < levels.Length && skill >= levels[highest + 1]) highest++;
            int selected = basic;
            if (highest > 0)
            {
                roll = Math.Max(0, Math.Min(0.999999, roll));
                if (roll < 0.6)
                {
                    int first = highest;
                    while (first > 1 && levels[first - 1] == levels[highest]) first--;
                    selected += first + (int)(roll / 0.6 * (highest - first + 1));
                }
                else selected += 1 + Math.Min(highest - 1, (int)((roll - 0.6) / 0.4 * highest));
            }
            int amount = Amount(kind, minutes);
            if (selected == basic) rewards.Add(basic, amount);
            else { rewards.Add(basic, amount / 2); rewards.Add(selected, amount - amount / 2); }
        }
    }
}
