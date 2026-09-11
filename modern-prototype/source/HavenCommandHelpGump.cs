using Server.Gumps;

namespace Server.HavenPrototype
{
    public class HavenCommandHelpGump:Gump
    {
        public HavenCommandHelpGump():base(45,45)
        {
            AddBackground(0,0,650,760,0xA28);
            AddLabel(24,20,0,"Haven command guide");
            AddHtml(24,53,600,60,"<BASEFONT COLOR=#202020>Type these commands in game chat, including the opening [. These are the custom commands available in this modern preview.</BASEFONT>",false,false);
            string[] commands={"[skillbudget","[haven","[c  or  [companion","[preview","[haventest","[home","[havenmarks / [minichamp","[cc","[startergear","[wallet / [tithe","[havenluck"};
            string[] descriptions={"Show counted skills, free skills and your stat cap.","Read about the Haven plaza stones and available services.","Recruit your companion or open orders, roles, pack and missions.","Open dungeon travel and OPTIONAL 120-skill test preparation.","Record Pass, Fail or Blocked results for your playtest checklist.","Claim your account's pirate lodge or return to its porch.","Reward shop / three-wave expeditions and a boss.","Open the compact companion combat controls.","Evolving starter set and robe upgrades at the Haven gear stone.","Store gold, transfer bank funds and buy Chivalry tithing points.","Check your total Luck and Haven training bonuses."};
            for(int i=0;i<commands.Length;i++) { AddLabel(24,123+i*35,0,commands[i]); AddHtml(210,123+i*35,414,34,"<BASEFONT COLOR=#202020>"+descriptions[i]+"</BASEFONT>",false,false); }
            AddLabel(24,515,0,"Companion orders: spoken normally, without [");
            AddHtml(24,544,600,136,"<BASEFONT COLOR=#202020>all follow me - follow you<BR>all guard me - guard you and engage eligible hostiles<BR>all stay / all stop - stop movement and queued casting<BR>all kill / all attack - choose an attack target<BR>all heal me - ask your companion to heal you<BR>These shared orders can also affect your ordinary pets.</BASEFONT>",false,true);
            AddButton(514,710,0xFA5,0xFA7,0,GumpButtonType.Reply,0); AddLabel(550,710,0,"Close");
        }
    }
}



