using System;
using Server.Spells.SkillMasteries;

namespace Server.UOOffline;

public static class HavenMasteryDefense
{
    // Haven compatibility balance: 10/20/30% at 120 weapon skill and tactics, capped for uncapped companions.
    internal static double DisarmBlockChance(Mobile mobile)
    {
        if (mobile?.Deleted != false || !mobile.Alive || !MasteryInfo.IsActivePassive(mobile, PassiveSpell.SavingThrow)) { return 0; }
        var skill = MasteryProgress.Current(mobile);
        var level = MasteryInfo.GetMasteryLevel(mobile, skill);
        if (level <= 0 || mobile.Skills[skill].Base < 90) { return 0; }
        var training = Math.Clamp(Math.Min(mobile.Skills[skill].Base, mobile.Skills.Tactics.Base), 0, 120) / 120;
        return Math.Clamp(level, 0, 3) * 0.10 * training;
    }

    public static bool BlocksDisarm(Mobile mobile) => Utility.RandomDouble() < DisarmBlockChance(mobile);

    internal static int ResilienceReduction(Mobile mobile)
    {
        if (SkillMasterySpell.GetSpellForParty(mobile, typeof(ResilienceSpell)) is not ResilienceSpell song) { return 0; }
        // Skill-scaled 10–60% protection. Overcapped skills cannot shorten an effect to zero.
        return Math.Clamp((int)(song.BaseSkillBonus * 5 + song.CollectiveBonus * 10 / 3), 10, 60);
    }

    public static TimeSpan ResilientDuration(Mobile mobile, TimeSpan duration) => duration <= TimeSpan.Zero ? duration :
        TimeSpan.FromTicks(duration.Ticks * (100 - ResilienceReduction(mobile)) / 100);

    public static int BleedTicks(Mobile mobile) => Math.Max(1, (int)Math.Ceiling(5 * (100 - ResilienceReduction(mobile)) / 100.0));

    // Preserve the compatibility port's intended 25% poison resistance; normal poison immunity still applies separately.
    public static bool ResistsPoison(Mobile mobile) => ResilienceReduction(mobile) > 0 && Utility.RandomDouble() < 0.25;
}
