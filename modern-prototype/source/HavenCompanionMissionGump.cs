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
            AddBackground(0,0,720,582,0xA28);
            Text(24,20,660,28,"<B>"+companion.Name+" - missions & roles</B>");
            Text(24,53,664,25,companion.OnMission?"Away: "+CompanionMissionTimerGump.Remaining(companion):"Ready | Current role: "+companion.Role);
            Button(24,88,10,_tab==0?"Gathering [selected]":"Gathering",180);
            Button(258,88,11,_tab==1?"Taming [selected]":"Taming",165);
            Button(490,88,12,_tab==2?"Roles [selected]":"Roles",165);
            if(_tab!=2){Text(24,130,108,25,"Duration");Button(128,130,20,_minutes==5?"5 min [x]":"5 min",100);Button(266,130,21,_minutes==15?"15 min [x]":"15 min",110);Button(410,130,22,_minutes==30?"30 min [x]":"30 min",110);Button(554,130,23,_minutes==60?"60 min [x]":"60 min",110);}
            else Text(24,130,655,26,"Change roles while nearby and out of combat. All roles can heal.");
            for(int row=0;row<6&&page*6+row<names.Length;row++){int index=page*6+row,y=181+row*36;if(index==_selection)Text(24,y,266,30,"<B>"+names[index]+"</B>");else Button(24,y,100+index,names[index],233);}
            if(names.Length>6){if(page>0)Button(24,401,30,"Previous",105);if((page+1)*6<names.Length)Button(165,401,31,"Next",105);}
            Text(322,180,365,32,"<B>"+names[_selection]+"</B>");
            string detail;
            if(_tab==2){detail=_selection==0?"Sword and shield. Fights up close.":_selection==1?"Magery and Spellweaving, Wraith Form and automatic Arcane Focus.":_selection==2?"Bow combat from range.":_selection==4?"Stronger direct heals, cures and resurrection. Treats the most urgent patient first. Emergency group recovery: 30 mana, 20-second cooldown, six-tile range. Stays with the group.":"Peacemaking, provocation and discordance. Native mastery songs at 90 skill; join the party to share them. Use Tame assist in Companion pets to calm and tame wild animals.";detail+="<BR><BR>Role changes preserve stored equipment and trained skills.";}
            else {
                detail="<B>Requirements</B><BR>"+Requirement(companion,_tab,_selection)+"<BR><BR><B>On completion</B><BR>"+(_minutes*100).ToString("N0")+" gold + "+(_minutes*2)+" Haven Marks.<BR>"+Reward(_tab,_selection,_minutes);
                detail+="<BR><BR>Early recall cancels the trip without completion rewards.";
            }
            AddHtml(322,222,363,184,"<BASEFONT COLOR=#342B23>"+detail+"</BASEFONT>",false,true);
            if(_tab==2)Button(322,423,1,"Use "+names[_selection]+" role",230);
            else if(!companion.OnMission)Button(322,423,1,"Start "+_minutes+"-minute mission",280);
            else {Button(322,423,2,"Minimize timer",160);Button(516,423,3,"Recall early",145);}
            Text(24,467,663,24,_tab==2?"Select a role above, then apply it.":"Select a mission and duration, then Start. Completed trips return automatically.");
            if(_tab!=2)Button(24,542,6,"Save as offline default",235);Button(365,542,7,"Offline setup",240);
            Button(24,504,0,"Back",100);Button(184,504,4,"Resource ledger",178);Button(418,504,5,"Collect pet tickets ("+companion.PendingPetTickets+")",259);
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
            if(tab==1)return "One owner-bound pet ticket, delivered to your pack when space permits.";
            if(selection>=6)return HavenRegionalMissions.Description(GatheringKind(selection),minutes);
            switch(selection){case 1:return HavenGatheringMissions.Amount(CompanionMission.Mining,minutes)+" ingots into the resource ledger.";case 2:return HavenGatheringMissions.Amount(CompanionMission.Lumber,minutes)+" logs into the resource ledger.";case 3:return HavenGatheringMissions.Amount(CompanionMission.Leather,minutes)+" leather into the resource ledger.";case 4:return (minutes*2)+" of each Malas resource into the ledger.";case 5:return minutes+" of each Abyss essence into the ledger.";default:return "Gold is delivered to the companion's pack.";}
        }
        void Text(int x,int y,int w,int h,string text){AddHtml(x,y,w,h,"<BASEFONT COLOR=#342B23>"+text+"</BASEFONT>",false,false);}
        void Button(int x,int y,int id,string label,int width){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);Text(x+33,y,width,27,label);}
        public override void OnResponse(NetState state,RelayInfo info)
        {
            var p=state.Mobile;int id=info.ButtonID;if(!_companion.IsOwner(p))return;
            if(id==0){_companion.Show(p,true);return;}
            if(id==1){bool ok=_tab==2?_companion.SetRole(p,(CompanionRole)_selection):_companion.StartMission(p,_minutes,(_tab==1?(CompanionMission)(_selection+6):GatheringKind(_selection)));if(ok&&_tab!=2){_companion.Show(p);return;}if(!ok&&_tab==2)p.SendMessage("Move near your companion and finish combat before changing roles.");}
            if(id==2){_companion.Show(p);return;}
            if(id==3&&!_companion.Recall(p))p.SendMessage("Cannot recall while in combat or unable to travel.");
            if(id==4){_companion.OpenResourceLedger(p);return;}
            if(id==5)_companion.DeliverPetTickets();
            if(id==6&&_tab!=2){var plan=HavenOfflineMissionPlan.Ensure(_companion);if(!plan.Configure(p,_tab==1?(CompanionMission)(_selection+6):GatheringKind(_selection),_minutes))p.SendMessage("Recall your companion and stand nearby to save a default.");else p.SendMessage("Offline default saved. Enable it in Offline setup.");}
            if(id==7){p.SendGump(new HavenOfflineMissionGump(HavenOfflineMissionPlan.Ensure(_companion)));return;}
            int tab=id>=10&&id<=12?id-10:_tab;
            int selected=tab!=_tab?0:id>=100&&id<112?id-100:id==30?Math.Max(0,(_selection/6-1)*6):id==31?(_selection/6+1)*6:_selection;
            int minutes=id==20?5:id==21?15:id==22?30:id==23?60:_minutes;
            p.CloseGump(typeof(CompanionActivityGump));p.SendGump(new CompanionActivityGump(_companion,tab,selected,minutes));
        }
    }
}
