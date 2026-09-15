using System;
using ModernUO.CodeGeneratedEvents;
using Server.Commands;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenFreeSkills
{
    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void ApplyPlayerCap(PlayerMobile player)
    {
        if (player is not Server.CustomBots.PlayerBot) { player.SkillsCap = 10000; }
    }
    public static bool IsFree(Mobile mobile, Skill skill) => mobile is PlayerMobile && mobile is not Server.CustomBots.PlayerBot &&
        skill.SkillName is SkillName.AnimalTaming or SkillName.AnimalLore or SkillName.Focus or SkillName.Snooping;
    public static int CountedTotal(Mobile mobile) => mobile is PlayerMobile && mobile is not Server.CustomBots.PlayerBot
        ? Math.Max(0, mobile.Skills.Total - mobile.Skills.AnimalTaming.BaseFixedPoint - mobile.Skills.AnimalLore.BaseFixedPoint - mobile.Skills.Focus.BaseFixedPoint - mobile.Skills.Snooping.BaseFixedPoint)
        : mobile.Skills.Total;
    public static void Initialize() => CommandSystem.Register("SkillBudget", AccessLevel.Player, e =>
        e.Mobile.SendMessage($"Counted skills: {CountedTotal(e.Mobile) / 10.0:F1}/{e.Mobile.Skills.Cap / 10.0:F1}. Animal Taming, Animal Lore, Focus and Snooping are free skills; their individual caps still apply."));
}
