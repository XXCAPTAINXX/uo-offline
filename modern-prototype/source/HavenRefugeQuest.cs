using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Gumps;
using Server.Network;

namespace Server.HavenPrototype
{
    public static class HavenRefugeQuest
    {
        public static int Stage(Mobile p)
        {
            int value;
            var account = p == null ? null : p.Account as Account;
            return account != null && int.TryParse(account.GetTag("Haven.RefugeQuest:" + p.Serial.Value), out value) ? value : 0;
        }
        private static void Advance(Mobile p, int stage)
        {
            ((Account)p.Account).SetTag("Haven.RefugeQuest:" + p.Serial.Value, stage.ToString());
            HavenBeaconQuest.StopGuide(p);
            HavenBeaconQuest.Speak(p, 8 + Math.Min(stage,4));
        }
        public static HavenMiniChamp Camp()
        {
            return HavenMiniChamp.Find();
        }
        public static bool Continue(Mobile p)
        {
            if(!HavenMarks.CanUse(p) || HavenBeaconQuest.Phase(p) != 5 || HavenBeaconQuest.NearbyJenna(p) == null) return false;
            int stage = Stage(p);
            if(stage == 0)
            {
                var steward = HavenRecovery.Steward();
                if(steward == null || p.Map != steward.Map || !p.InRange(steward,4) || !p.InLOS(steward)) return false;
            }
            else if(stage == 1)
            {
                if(!World.Items.Values.OfType<HavenServiceStone>().Any(s => !s.Deleted && (s.Service == 0 || s.Service == 1 || s.Service == 10) && p.Map == s.Map && p.InRange(s,3) && p.InLOS(s))) return false;
            }
            else if(stage == 2)
            {
                var camp = Camp();
                if(camp == null || p.Map != camp.Map || !p.InRange(camp,8) || !p.InLOS(camp)) return false;
            }
            else return false;
            Advance(p,stage + 1);
            return true;
        }
        public static void CompletedExpedition(Mobile p, HavenMiniChamp camp)
        {
            if(camp == null || camp != Camp() || p == null || p.Deleted || !(p.Account is Account) || HavenBeaconQuest.Phase(p) != 5 || Stage(p) != 3) return;
            Advance(p,4);
            p.SendMessage(53,"The Refuge Remembers: the expedition report holds your next story clue. Open [storyquest and choose Continue adventure.");
        }
        public static void Guide(Mobile p)
        {
            IEntity target = null;
            switch(Stage(p))
            {
                case 0: target = HavenRecovery.Steward(); break;
                case 1: target = World.Items.Values.OfType<HavenServiceStone>().FirstOrDefault(s => !s.Deleted && s.Service == 1 && s.Map == Map.Trammel); break;
                case 2: case 3: target = Camp(); break;
            }
            if(target == null) { p.SendMessage("No active destination is available for this objective."); return; }
            if(p.Map != target.Map) { p.SendMessage("Destination: " + target.Map + " " + target.Location + (Stage(p)<2 ? ". Return to New Haven for this introduction." : ". Return to Haven, then use [minichamp and Travel to camp.")); return; }
            if(p.QuestArrow != null) p.QuestArrow.Stop();
            p.QuestArrow = new HavenBeaconQuestArrow(p,target);
            p.SendMessage("Follow the arrow to " + target.Location + ". Bring Jenna with you.");
        }
        public static void Show(Mobile p)
        {
            if(!HavenMarks.CanUse(p) || HavenBeaconQuest.Phase(p) != 5) return;
            var account = (Account)p.Account;
            string intro = "Haven.RefugeIntro:" + p.Serial.Value;
            if(account.GetTag(intro) == null) { account.SetTag(intro,"1"); HavenBeaconQuest.Speak(p,8); }
            p.CloseGump(typeof(HavenRefugeQuestGump));
            p.SendGump(new HavenRefugeQuestGump(p));
        }
    }
    public sealed class HavenRefugeQuestGump : HavenMenuGump
    {
        private readonly Mobile _owner;
        public HavenRefugeQuestGump(Mobile p) : base(55,55)
        {
            _owner = p;
            int stage = Math.Max(0,Math.Min(4,HavenRefugeQuest.Stage(p)));
            string[] objectives = { "Meet the recovery steward", "Visit the purchasing stones", "Reach the Haven mini-champ", "Complete a Haven expedition", "The expedition report" };
            string[] pages = {
                "<B>Jenna:</B> The beacon found our refuge, but I am not taking you into pirate country without a way home.<BR><BR>Visit Mara, Haven's pet healer and recovery steward, with Jenna beside you. Use Show direction, then choose Discuss this stop when you arrive.<BR><BR>Ava resurrects you. Mara heals or resurrects nearby pets and summons surviving corpses. You must open summoned corpses to reclaim their contents. Read Recovery and supplies below for the full explanation.<BR><BR>You do not need to die or spend anything to complete this introduction.",
                "<B>Jenna:</B> Now we know how to recover from trouble. Let's try being prepared for it.<BR><BR>Visit a purchasing stone with Jenna and discuss the stop. No purchase is required.<BR><BR>Select an item to inspect its price and hover over its preview to read properties. Buy selected is the purchase action. Gold, Haven Marks and Astral shards are different currencies; check the price before buying.<BR><BR>Wear your leveling cape and bring healing supplies before we sail.",
                "<B>Jenna:</B> Before we sail, we need some experience working together outside town. The Haven expedition camp is the place to start.<BR><BR>Use [minichamp and choose Travel to camp, or follow Show direction. Approach the Haven camp with Jenna, then discuss the stop.<BR><BR>This is a scouting visit. Discussing the camp does not start a fight.",
                "<B>Jenna:</B> Wildwood creatures, corsair raiders, or restless dead. Choose one threat and we'll face it together, here on Haven.<BR><BR>Start and finish any normal Haven mini-champ expedition. Each has three waves of five enemies, followed by its champion. Your damage, Jenna's damage and your pets' damage all count toward participation.<BR><BR>Challenge mode also counts, but is optional: three waves of fifteen enemies, then all three champions together.<BR><BR>Completion is recorded from the actual expedition victory. Existing expedition rewards still apply; this journal does not replace or duplicate them.",
                "The expedition report includes a message recovered on Haven: <I>Keep the refuge light burning. The next arrival must find the shore.</I><BR><BR><B>Jenna:</B> They weren't trying to extinguish the beacon. Someone wants us to reach that island. I don't know whether that makes me feel better.<BR><BR><B>Chapter complete:</B> You learned Haven's recovery services, inspected the purchasing stones, found the Haven camp and completed an expedition. Collect any pending expedition rewards at the camp.<BR><BR>The island refuge is our next destination. Its Cove battles and dangerous northeastern wildlife come later; you do not need to visit the island to finish this chapter."
            };
            AddBackground(0,0,720,570,3000);
            AddLabel(24,20,0,"THE REFUGE REMEMBERS | Chapter two");
            AddLabel(24,52,0,"Objective: " + objectives[stage]);
            AddHtml(24,90,670,300,"<BASEFONT COLOR=#3B2A1A>" + pages[stage] + "</BASEFONT>",false,true);
            if(stage < 3) FlatButton(24,412,210,1,"Discuss this stop");
            FlatButton(249,412,210,2,"Show direction");
            FlatButton(474,412,220,3,"Replay Jenna's line");
            FlatButton(24,462,210,4,"Recovery and supplies");
            FlatButton(249,462,210,5,"Opening quest / gifts");
            FlatButton(474,462,220,0,"Close");
            if(stage>=1) FlatButton(24,512,435,6,HavenPersonalCamp.Claimed(p)?"Replay Jenna's camp explanation":"Claim personal camp from Jenna");
        }
        public override void OnResponse(NetState state, RelayInfo info)
        {
            if(state.Mobile != _owner || !HavenMarks.CanUse(_owner) || HavenBeaconQuest.Phase(_owner) != 5 || info.ButtonID == 0) return;
            if(info.ButtonID == 1 && !HavenRefugeQuest.Continue(_owner)) _owner.SendMessage("Bring Jenna beside the current objective, within sight, then discuss this stop.");
            else if(info.ButtonID == 2) HavenRefugeQuest.Guide(_owner);
            else if(info.ButtonID == 3) HavenBeaconQuest.Speak(_owner,8 + Math.Min(4,HavenRefugeQuest.Stage(_owner)),true);
            else if(info.ButtonID == 4) { _owner.SendGump(new HavenJennaFieldBriefing(_owner)); return; }
            else if(info.ButtonID == 5) { HavenBeaconQuest.Show(_owner); return; }
            else if(info.ButtonID == 6){if(HavenPersonalCamp.Claimed(_owner))HavenBeaconQuest.Speak(_owner,13,true);else if(!HavenPersonalCamp.Claim(_owner))_owner.SendMessage("Keep Jenna nearby and make room in your backpack. Each character receives one camp.");}
            HavenRefugeQuest.Show(_owner);
        }
    }
}
