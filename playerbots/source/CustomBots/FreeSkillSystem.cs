// =========================================================================
// FreeSkillSystem.cs — secondary/free skills for the offline shard.
//
// Free skills train normally and retain their individual caps/power scrolls,
// but they do not consume the character's normal aggregate skill-cap budget.
//
// Baseline = union of public InsaneUO + UOAlive secondary-skill models.
// Player additions = Poisoning + Stealth.
//
// Primary combat/taming/bard-control/magery/healing skills still count.
// =========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.CustomBots
{
    public static class FreeSkillSystem
    {
        private static readonly HashSet<SkillName> _free = new()
        {
            SkillName.Alchemy,
            SkillName.ArmsLore,
            SkillName.Begging,
            SkillName.Fletching,
            SkillName.Camping,
            SkillName.Cartography,
            SkillName.Cooking,
            SkillName.DetectHidden,
            SkillName.Fishing,
            SkillName.Forensics,
            SkillName.Herding,
            SkillName.Hiding,
            SkillName.Inscribe,
            SkillName.ItemID,
            SkillName.Lockpicking,
            SkillName.Lumberjacking,
            SkillName.Mining,
            SkillName.Musicianship,
            SkillName.Poisoning,
            SkillName.RemoveTrap,
            SkillName.Snooping,
            SkillName.Stealth,
            SkillName.TasteID,
            SkillName.Tracking
        };

        public static void Configure()
        {
            CommandSystem.Register("FreeSkills", AccessLevel.Player, FreeSkills_OnCommand);
        }

        public static bool IsFree(SkillName name) => _free.Contains(name);

        public static bool AppliesTo(Mobile from) =>
            from is PlayerMobile && from is not PlayerBot;

        public static bool IsFree(Mobile from, Skill skill) =>
            skill != null && AppliesTo(from) && IsFree(skill.SkillName);

        public static int FreeTotalFixed(Mobile from)
        {
            if (!AppliesTo(from) || from?.Skills == null)
            {
                return 0;
            }

            int total = 0;

            foreach (var name in _free)
            {
                total += from.Skills[name]?.BaseFixedPoint ?? 0;
            }

            return total;
        }

        public static int CappedTotalFixed(Mobile from)
        {
            if (from?.Skills == null)
            {
                return 0;
            }

            return Math.Max(0, from.Skills.Total - FreeTotalFixed(from));
        }

        public static double CappedTotal(Mobile from) => CappedTotalFixed(from) / 10.0;
        public static double FreeTotal(Mobile from) => FreeTotalFixed(from) / 10.0;

        private static void FreeSkills_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            if (from == null)
            {
                return;
            }

            from.SendMessage(0x35, "=== Free / Secondary Skills ===");
            from.SendMessage(
                $"Main skill total: {CappedTotal(from):0.0} / {from.Skills.Cap / 10.0:0.0}"
            );
            from.SendMessage($"Free skill total: {FreeTotal(from):0.0}");
            from.SendMessage($"All trained skills: {from.Skills.Total / 10.0:0.0}");

            var names = _free
                .OrderBy(x => x.ToString())
                .Select(x => x.ToString());

            from.SendMessage(string.Join(", ", names));
            from.SendMessage("Free skills still obey their own individual skill caps.");
        }
    }
}
