using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Engines.MLQuests.Objectives;
using Server.Items;
using Server.Mobiles;
namespace Server.UOOffline;
public static class HavenTrainingRewards
{
    private static readonly Type[] Quests = [
typeof(CleansingOldHaven),
typeof(TheRudimentsOfSelfDefense),
typeof(CrushingBonesAndTakingNames),
typeof(SwiftAsAnArrow),
typeof(EnGuarde),
typeof(TheArtOfWar),
typeof(TheWayOfTheBlade),
typeof(ThouAndThineShield),
typeof(DefyingTheArcane),
typeof(StoppingTheWorld),
typeof(ScribingArcaneKnowledge),
typeof(TheMagesApprentice),
typeof(ScholarlyTask),
typeof(TheRightToolForTheJob),
typeof(KnowThineEnemy),
typeof(BruisesBandagesAndBlood),
typeof(TheInnerWarrior),
typeof(TheArtOfStealth),
typeof(BecomingOneWithTheShadows),
typeof(WalkingSilently),
typeof(EyesOfARanger),
typeof(TheWayOfTheSamurai),
typeof(TheAllureOfDarkMagic),
typeof(ChannelingTheSupernatural),
typeof(TheDeluciansLostMine),
typeof(ItsHammerTime),
    ];
    public static void Initialize() => CommandSystem.Register("HavenRewards", AccessLevel.Player, e => Claim(e.Mobile));
    public static void Claim(Mobile from)
    {
        if (from is not PlayerMobile player || !from.Alive || from.Backpack == null) { return; }
        var count = 0;
        foreach (var type in Quests)
        {
            var quest = MLQuestSystem.FindQuest(type);
            if (quest != null && ClaimQuest(player, quest)) { count++; }
        }
        from.SendMessage($"Claimed {count} Haven training reward bundle(s). Rewards require the quest's base skill threshold and can be claimed once; active quests must be turned in normally.");
    }
    internal static bool ClaimQuest(PlayerMobile player, MLQuest quest)
    {
        if (!player.Alive || player.Backpack == null || Array.IndexOf(Quests, quest.GetType()) < 0) { return false; }
        var context = MLQuestSystem.GetOrCreateContext(player);
        if (context.HasDoneQuest(quest) || context.IsDoingQuest(quest) || quest.Objectives.Count == 0) { return false; }
        foreach (var objective in quest.Objectives)
        {
            if (objective is not GainSkillObjective skill || player.Skills[skill.Skill].BaseFixedPoint < skill.ThresholdFixed) { return false; }
        }
        var items = new List<Item>();
        foreach (var reward in quest.Rewards) { reward.AddRewardItems(player, items); }
        if (items.Count == 0) { return false; }
        var bag = new Bag { Name = "Haven training rewards" };
        foreach (var item in items) { bag.DropItem(item); }
        if (!player.Backpack.TryDropItem(player, bag, false)) { bag.Delete(); player.SendMessage("Make room for your Haven rewards; nothing has been claimed."); return false; }
        context.SetDoneQuest(quest);
        return true;
    }
}