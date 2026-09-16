using System;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.HavenPrototype
{
    // Repeatable explanations never buy items, start encounters or consume rewards.
    public sealed class HavenJennaFieldBriefing : HavenMenuGump
    {
        private readonly Mobile _owner;
        private readonly int _page;
        private static readonly string[] Titles = { "Getting back on your feet", "Equipping for the journey", "The crews of Blackwake Cove" };
        private static readonly string[] Lessons = {
            "<B>Jenna:</B> Before we go chasing lights across the sea, you should know who can bring us home.<BR><BR><B>Ava, the healer:</B> If you die, use [healer to reach Haven's recovery area. Approach Ava as a ghost and accept resurrection.<BR><BR><B>Mara, the recovery steward:</B> Double-click Mara nearby. Stand within three tiles, alive and out of combat, to use her services.<BR><BR><B>Summon my corpses</B> brings your surviving corpses to you. Open each corpse and take your belongings; the items do not automatically return to your backpack. Decayed corpses cannot be restored.<BR><BR><B>Heal / resurrect nearby pets</B> restores your nearby pets, including dead bonded pets. Bring them within sight. Mara also has a separate option to recover a dead companion.<BR><BR><B>Jenna:</B> A bad fight needn't be the end of our adventure. Just don't leave your belongings behind twice.",
            "<B>Jenna:</B> Those stones in the plaza are our supply counters. Have a look before spending your first reward.<BR><BR><B>Haven supplies and equipment</B> offers starting equipment. <B>Arcane supplies</B> carries magical supplies and useful travel items. <B>Training supplies</B> and <B>Special rewards</B> serve different needs as you grow.<BR><BR>Select an item to read its price and description. Hover over its preview for properties. Only <B>Buy selected</B> makes the purchase. Check whether the price asks for gold, Haven Marks or Astral shards. Gold and Marks balances appear at the top; shard purchases use your wallet.<BR><BR>Your leveling cape grows through use while worn. Inspect its tooltip for progress. Keep useful leveling gear rather than replacing it just because it started modestly.<BR><BR><B>Jenna:</B> Supplies first, shiny things second. I say that with the authority of someone who has done it the other way round.",
            "<B>Jenna:</B> The refuge isn't deserted. Three crews are contesting Blackwake Cove, and we can choose which one to face.<BR><BR>Use the expedition camp's menu. A normal expedition has <B>three waves of five enemies</B>, followed by that crew's champion. Stormsail brings fast blade fighters; Blackwake brings a boarding crew; the drowned fleet brings undead sailors and a spellcasting captain.<BR><BR>Your pets' and companion's damage counts toward your participation. Stay near the battle. Rewards arrive in your backpack; if it is full, use Collect pending rewards at the camp.<BR><BR><B>Challenge: all three crews</B> raises each wave to fifteen mixed enemies and ends with all three champions together. Save that for when we're ready.<BR><BR><B>Jenna:</B> We'll learn the camp first. The island's stronger foes and dangerous northeastern wildlife can wait until we've inspected them. A creature being tameable doesn't make it safe."
        };

        public HavenJennaFieldBriefing(Mobile owner, int page = 0) : base(55,55)
        {
            _owner = owner;
            _page = Math.Max(0, Math.Min(Titles.Length - 1, page));
            AddBackground(0,0,720,530,3000);
            AddLabel(24,20,0,"JENNA'S FIELD GUIDE | " + Titles[_page]);
            AddHtml(24,64,670,350,"<BASEFONT COLOR=#3B2A1A>" + Lessons[_page] + "</BASEFONT>",false,true);
            FlatButton(24,432,210,1,"Recovery services");
            FlatButton(249,432,210,2,"Purchasing stones");
            FlatButton(474,432,220,3,"Island expeditions");
            FlatButton(24,478,210,4,"Back to quest journal");
            FlatButton(474,478,220,0,"Close");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if(sender.Mobile != _owner || !HavenPreview.Enabled || _owner.Deleted || !_owner.Player || _owner.Account == null) return;
            if(info.ButtonID >= 1 && info.ButtonID <= 3)
                _owner.SendGump(new HavenJennaFieldBriefing(_owner,info.ButtonID - 1));
            else if(info.ButtonID == 4) HavenBeaconQuest.Show(_owner);
        }
    }
}
