using System;
using Server.Network;
using Server.Gumps;

namespace Server.HavenPrototype
{
    public class HavenCommandHelpGump : HavenMenuGump
    {
        static readonly string[] Categories={"Companions & pets","Travel & recovery","Gear & shops","Storage & currency","Combat & skills","Spoken orders"};
        static readonly string[][] Commands={
            new[]{"[c  /  [companion","[cc","[companionpets","[pettrain","[petexchange"},
            new[]{"[preview","[home","[healer","[recovery","[tide","[rune"},
            new[]{"[startergear","[gearprogress","[rewards","[arcane","[training"},
            new[]{"[wallet","[tithe 1000","[codex","[trashbag"},
            new[]{"[mystats","[minichamp","[warden","[abyss","[skillbudget","[havenluck","[renounceyoung"},
            new[]{"all follow me","all guard me","all stay  /  all stop","all kill  /  all attack","all heal me"}
        };
        static readonly string[][] Descriptions={
            new[]{"Companion menu: orders, roles, inventory and missions.","Compact companion combat bar, including Heal, Tame and Pack.","Manage pets assigned to your companion.","Target your pet to view training categories and upgrades.","Exchange unused mission tickets for credits toward rarer pets."},
            new[]{"Open the travel destinations. Leave combat before traveling.","Return to your home, or open the home claim menu.","Travel to the Haven healer. Recent combat can block travel.","Open healing, pet resurrection, companion and corpse recovery.","While mounted on your Tidebound sea horse: cargo and SOS navigation.","Withdraw one blank rune from your Wayfarer's rune pouch."},
            new[]{"View evolving starter equipment and upgrades.","Target supported equipment to inspect its progression.","Browse Special Rewards by category. Buy beside a shop stone.","Browse spellbooks, reagents and magical supplies.","Browse power scrolls, mastery books and primers."},
            new[]{"Open your wallet: deposit gold/checks, withdraw funds and tithe.","Spend 1,000 wallet gold on Chivalry tithing points. Change the number as needed.","Open your Champion's Codex for scrolls and other stored rewards.","Claim a trash bag. Review unwanted items before disposing of them."},
            new[]{"Your attributes, resistances, equipment bonuses and skill values/caps.","Open mini-champion expeditions and Challenge mode.","Open the Warden encounter menu.","Open the Abyss trial menu.","Review counted skills, free skills and your stat cap.","Show total Luck and Haven's accelerated skill-training bonuses.","Open the confirmation to give up Young status."},
            new[]{"Ask followers to follow you.","Ask followers to guard you and engage eligible enemies.","Stop movement and queued casting.","Choose a target for your followers to attack.","Ask your companion to heal you."}
        };
        readonly int _category;
        public HavenCommandHelpGump(int category=0):base(45,45)
        {
            _category=Math.Max(0,Math.Min(Categories.Length-1,category));
            AddBackground(0,0,740,575,3000);
            AddLabel(24,22,0,"HAVEN COMMAND GUIDE");
            AddHtml(24,56,690,44,"Type the command in game chat, including the opening <B>[</B>. Choose a category below. Reopen this guide with <B>[?</B>.",false,false);
            AddBackground(18,112,200,405,0xBB8);AddBackground(230,112,490,405,0xBB8);
            AddLabel(30,126,0,"CATEGORIES");
            for(int i=0;i<Categories.Length;i++)FlatButton(30,166+i*48,176,10+i,(_category==i?"[":"")+Categories[i]+(_category==i?"]":""));
            AddLabel(246,126,0,Categories[_category].ToUpperInvariant());
            for(int i=0;i<Commands[_category].Length;i++){
                int y=160+i*48;
                AddHtml(246,y,454,21,"<B>"+HavenMenuText.Encode(Commands[_category][i])+"</B>",false,false);
                AddHtml(246,y+20,454,28,HavenMenuText.Encode(Descriptions[_category][i]),false,false);
            }
            AddHtml(24,531,550,32,_category==5?"Speak these without [. Shared orders can also affect ordinary pets.":"Commands keep their usual ownership, distance and combat requirements.",false,false);
            FlatButton(594,536,120,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info){if(HavenPreview.Enabled&&info.ButtonID>=10&&info.ButtonID<10+Categories.Length)sender.Mobile.SendGump(new HavenCommandHelpGump(info.ButtonID-10));}
    }
}
