using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Network;

namespace Server.HavenPrototype
{
    public class CompanionActivityGump:HavenMenuGump
    {
        readonly HavenCompanion _companion;
        readonly int _tab,_selection,_minutes;
        static readonly string[] BaseGathering={"Supply run","Mining","Lumberjacking","Leather gathering","Malas supplies","Abyss supplies"};
        static readonly string[] Gathering=BaseGathering.Concat(HavenRegionalMissions.Names).ToArray();
        static CompanionMission GatheringKind(int i){return i<6?(CompanionMission)i:HavenRegionalMissions.Kinds[i-6];}
        static readonly string[] Roles={"Warrior","Caster","Archer","Bard","Healer"};
        public static string MissionName(CompanionMission kind){int i=(int)kind;if(HavenRegionalMissions.Valid(kind))return HavenRegionalMissions.Names[Array.IndexOf(HavenRegionalMissions.Kinds,kind)];return i>=6&&i<18?HavenPetMissions.Names[i-6]:i>=0&&i<Gathering.Length?Gathering[i]:"Mission";}
        public CompanionActivityGump(HavenCompanion companion,int tab=0,int selection=0,int minutes=5):base(50,50)
        {
            _companion=companion;_tab=Math.Max(0,Math.Min(2,tab));_minutes=minutes==15||minutes==30||minutes==60?minutes:5;
            var names=_tab==0?Gathering:_tab==1?HavenPetMissions.Names:Roles;
            _selection=Math.Max(0,Math.Min(names.Length-1,selection));int page=_selection/6;
            AddBackground(0,0,720,560,0xA28);
            Text(24,20,660,28,""+companion.Name+" - "+(_tab==2?"combat role":"missions")+"");
            Text(24,52,664,25,companion.OnMission?"Away: "+MissionName(companion.MissionKind)+" | "+CompanionMissionTimerGump.Remaining(companion):"Ready | Combat role: "+companion.Role);
            Button(24,87,10,_tab==0?"[Gathering]":"Gathering",145);
            Button(215,87,11,_tab==1?"[Taming]":"Taming",140);

            AddBackground(14,122,282,348,3000);
            AddBackground(306,122,397,374,3000);
            Text(24,133,264,25,""+(_tab==2?"Choose a role":"1. Choose a mission")+"");
            for(int row=0;row<6&&page*6+row<names.Length;row++)
            {
                int index=page*6+row,y=174+row*42;
                Button(24,y,100+index,index==_selection?"Selected: "+names[index]+"":names[index],235,40);
            }
            if(names.Length>6)
            {
                if(page>0)Button(24,435,30,"Prev",54);
                Text(115,435,95,25,"Page "+(page+1)+" / "+((names.Length+5)/6));
                if((page+1)*6<names.Length)Button(212,435,31,"Next",58);
            }
            Text(322,133,365,28,""+names[_selection]+"");
            if(_tab==2)
            {
                string detail=_selection==0?"Sword and shield. Fights up close.":_selection==1?"Magery and Spellweaving, Wraith Form and automatic Arcane Focus.":_selection==2?"Bow combat from range.":_selection==4?"Stronger direct heals, cures and resurrection. Treats the most urgent patient first.<BR><BR>Emergency group recovery: 30 mana, 20-second cooldown, six-tile range. Stays with the group.":"Peacemaking, provocation and discordance. Native mastery songs at 90 skill; join the party to share them.<BR><BR>Use Tame assist in Companion pets to calm and tame wild animals.";
                Text(322,179,365,200,detail);
                Text(322,385,365,58,"All roles can heal. Changing role preserves equipment and trained skills.");
                Button(322,455,1,"Use "+names[_selection]+" role",320);
            }
            else
            {
                Text(322,175,365,24,"2. Duration");
                int[] durations={5,15,30,60};
                for(int i=0;i<4;i++)Button(322+i*91,204,20+i,(_minutes==durations[i]?"[":"")+durations[i]+"m"+(_minutes==durations[i]?"]":""),53);
                Text(322,242,365,24,"Requirements");
                Text(322,268,365,62,Requirement(companion,_tab,_selection));
                Text(322,334,365,24,"Rewards on completion");
                Text(322,360,365,88,(_minutes*100).ToString("N0")+" gold + "+(_minutes*2)+" Haven Marks.<BR>"+Reward(_tab,_selection,_minutes));
                if(!companion.OnMission)Button(322,455,1,"Start "+HavenMissionLuck.Duration(_minutes,companion.BoundOwner==null?0:companion.BoundOwner.Luck)+" mission",320);
                else {Button(322,455,2,"Show timer",145);Button(515,455,3,"Recall early",145);}
                Text(24,496,670,22,"Luck "+(companion.BoundOwner==null?0:companion.BoundOwner.Luck)+": "+HavenMissionLuck.Duration(_minutes,companion.BoundOwner==null?0:companion.BoundOwner.Luck)+" duration, full "+_minutes+"m rewards. Early recall forfeits rewards.");
            }
            Button(24,520,0,"Back",65);
            if(_tab!=2)
            {
                Button(139,520,4,"Ledger",85);
                Button(254,520,5,"Tickets ("+companion.PendingPetTickets+")",100);Button(386,520,8,"Pet exchange",105);
                Button(511,520,7,"Offline setup",145);
            }
        }

        public static string Requirement(HavenCompanion c,int tab,int selection)
        {
            if(tab==1){double required=HavenPetMissions.Requirements[selection];return "Taming and Animal Lore: "+required.ToString("0.0")+" each.<BR>Yours: "+c.Skills.AnimalTaming.Base.ToString("0.0")+" / "+c.Skills.AnimalLore.Base.ToString("0.0");}
            if(selection>=6){var kind=GatheringKind(selection);return HavenRegionalMissions.Requirement(kind)+" combat / resist rating.<BR>Yours: "+HavenRegionalMissions.Rating(c).ToString("0.0");}
            if(selection==4||selection==5)return (selection==4?"60":"80")+" Magery or Tactics.<BR>Yours: "+c.Skills.Magery.Base.ToString("0.0")+" / "+c.Skills.Tactics.Base.ToString("0.0");
            if(selection==1)return "Mining determines the metal collected. Half basic, half unlocked metal when a special tier is selected.";
            if(selection==2)return "Lumberjacking determines the wood collected. Half ordinary logs when a special wood is selected.";
            if(selection==3)return "The lower of Wrestling and Tactics determines leather quality.";
            return "No gathering skill requirement.";
        }
        public static string Reward(int tab,int selection,int minutes)
        {
            if(tab==1)return "One pet ticket; "+HavenTamingSupplies.SearchRolls(minutes)+" rarity / supply rolls. Bonus supplies go into the companion pack. Leash, bonding potion, 105 combat scroll or rare house post.";
            if(selection>=6)return HavenRegionalMissions.Description(GatheringKind(selection),minutes);
            switch(selection){case 1:return HavenGatheringMissions.Amount(CompanionMission.Mining,minutes)+" ingots into the resource ledger.";case 2:return HavenGatheringMissions.Amount(CompanionMission.Lumber,minutes)+" logs into the resource ledger.";case 3:return HavenGatheringMissions.Amount(CompanionMission.Leather,minutes)+" leather into the resource ledger.";case 4:return (minutes*2)+" of each Malas resource into the ledger.";case 5:return minutes+" of each Abyss essence into the ledger.";default:return "Gold is delivered to the companion's pack.";}
        }
        void Text(int x,int y,int w,int h,string text){AddHtml(x,y,w,h,"<BASEFONT COLOR=#3B2A1A>"+text+"</BASEFONT>",false,false);}
        void Button(int x,int y,int id,string label,int width,int height=27){FlatButton(x,y,width+20,id,label);}
        public override void OnResponse(NetState state,RelayInfo info)
        {
            var p=state.Mobile;int id=info.ButtonID;if(!_companion.IsOwner(p))return;_companion.ShowAwayTimer(p);
            if(id==0){_companion.Show(p,true);return;}
            if(id==1){bool ok=_tab==2?_companion.SetRole(p,(CompanionRole)_selection):_companion.StartMission(p,_minutes,(_tab==1?(CompanionMission)(_selection+6):GatheringKind(_selection)));if(ok&&_tab!=2){_companion.Show(p);return;}if(!ok&&_tab==2)p.SendMessage("Move near your companion and finish combat before changing roles.");}
            if(id==2){_companion.Show(p);return;}
            if(id==3&&!_companion.Recall(p))p.SendMessage("Cannot recall: your current location is unavailable.");
            if(id==4){_companion.OpenResourceLedger(p);return;}
            if(id==5)_companion.DeliverPetTickets();if(id==8){HavenPetExchange.Show(p);return;}

            if(id==7&&_tab!=2){p.SendGump(new HavenOfflineMissionGump(HavenOfflineMissionPlan.Ensure(_companion),_tab==1?(CompanionMission)(_selection+6):GatheringKind(_selection),_minutes));return;}
            int tab=id>=10&&id<=12?id-10:_tab;
            int selected=tab!=_tab?0:id>=100&&id<112?id-100:id==30?Math.Max(0,(_selection/6-1)*6):id==31?(_selection/6+1)*6:_selection;
            int minutes=id==20?5:id==21?15:id==22?30:id==23?60:_minutes;
            p.CloseGump(typeof(CompanionActivityGump));p.SendGump(new CompanionActivityGump(_companion,tab,selected,minutes));
        }
    }
}
