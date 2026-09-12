using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public static class HavenPetTrainingMenu
    {
        public static readonly string[] Categories = { "Stats", "Resists", "Magic skill caps", "Combat skill caps", "Magical abilities", "Special abilities", "Special moves", "Area effect abilities" };
        public static void Initialize() { CommandSystem.Register("pettrain", AccessLevel.Player, e => { if(HavenPreview.Enabled) e.Mobile.Target = new TrainingTarget(); }); }
        sealed class TrainingTarget : Target {
            public TrainingTarget() : base(12, false, TargetFlags.None) { }
            protected override void OnTarget(Mobile from, object target) { Show(from, target as BaseCreature); }
        }
        public static bool CanUse(Mobile owner, BaseCreature pet) {
            return HavenPreview.Enabled && PetTrainingHelper.Enabled && owner is PlayerMobile && !owner.Deleted && owner.Alive &&
                pet != null && !pet.Deleted && pet.Alive && !pet.IsDeadPet && pet.Controlled && pet.ControlMaster == owner &&
                !(pet is HavenCompanion) && !pet.IsStabled && pet.Map == owner.Map && owner.InRange(pet,12) && owner.InLOS(pet) &&
                (!(pet is IMount) || ((IMount)pet).Rider == null) && PetTrainingHelper.GetTrainingDefinition(pet) != null;
        }
        public static void Show(Mobile owner, BaseCreature pet, int category=0, int page=0) {
            if (!CanUse(owner,pet)) { owner.SendMessage("Bring your living, unmounted pet nearby to train it."); return; }
            owner.CloseGump(typeof(HavenPetTrainingGump)); owner.SendGump(new HavenPetTrainingGump(pet,category,page));
        }
        public static bool Peaceful(Mobile owner, BaseCreature pet) {
            return HavenPreview.TravelCombatSeconds(owner)==0 && HavenPreview.TravelCombatSeconds(pet)==0;
        }
        public static List<TrainingPoint> Options(BaseCreature pet, int category) {
            var result = new List<TrainingPoint>();
            Action<object> add = o => { var tp=PetTrainingHelper.GetTrainingPoint(o); if(tp!=null)result.Add(tp); };
            if(category==0)foreach(PetStat stat in Enum.GetValues(typeof(PetStat)))add(stat);
            else if(category==1)foreach(ResistanceType resist in Enum.GetValues(typeof(ResistanceType)))add(resist);
            else if(category==2 || category==3)foreach(var skill in category==2?PetTrainingHelper.MagicSkills:PetTrainingHelper.CombatSkills) { if(pet.Skills[skill].Base>0)add(skill); }
            else {
                var def=PetTrainingHelper.GetTrainingDefinition(pet); if(def==null)return result;
                if(category==4)foreach(var ability in PetTrainingHelper.MagicalAbilities)if(PetTrainingHelper.ValidateTrainingPoint(pet,ability))add(ability);
                if(category==5)foreach(var ability in SpecialAbility.Abilities)if(ability!=null && PetTrainingHelper.ValidateTrainingPoint(pet,ability))add(ability);
                if(category==6)foreach(var ability in def.WeaponAbilities)if(ability!=null)add(ability);
                if(category==7)foreach(var ability in AreaEffect.Effects)if(ability!=null && PetTrainingHelper.ValidateTrainingPoint(pet,ability))add(ability);
            }
            return result;
        }
        public static int Current(BaseCreature pet, TrainingPoint tp) {
            if(tp.TrainPoint is SkillName)return Math.Max(0,(int)(pet.Skills[(SkillName)tp.TrainPoint].Cap*10)-1000);
            int value=tp.Start==tp.Max?0:tp.Start; PetTrainingHelper.GetStartValue(tp,pet,ref value); return value;
        }
        public static string ValueText(BaseCreature pet, TrainingPoint tp) {
            if(tp.TrainPoint is SkillName)return pet.Skills[(SkillName)tp.TrainPoint].Cap.ToString("F1");
            if(tp.Start==tp.Max)return PetTrainingHelper.GetAbilityProfile(pet,true).HasAbility(tp.TrainPoint)?"Learned":"Not learned";
            return Current(pet,tp).ToString();
        }
        public static bool Available(BaseCreature pet, TrainingPoint tp) {
            var a=PetTrainingHelper.GetAbilityProfile(pet,true); var point=tp.TrainPoint;
            if(point is PetStat || point is ResistanceType)return true;
            if(point is SkillName)return PetTrainingHelper.ValidateTrainingPoint(pet,(SkillName)point);
            if(a.HasAbility(point))return false;
            if(point is MagicalAbility)return PetTrainingHelper.ValidateTrainingPoint(pet,(MagicalAbility)point)&&a.CanChooseMagicalAbility((MagicalAbility)point);
            if(point is SpecialAbility)return PetTrainingHelper.ValidateTrainingPoint(pet,(SpecialAbility)point)&&a.CanChooseSpecialAbility(PetTrainingHelper.GetTrainingDefinition(pet).SpecialAbilities);
            if(point is AreaEffect)return PetTrainingHelper.ValidateTrainingPoint(pet,(AreaEffect)point)&&a.CanChooseAreaEffect();
            if(point is WeaponAbility)return PetTrainingHelper.ValidateTrainingPoint(pet,(WeaponAbility)point)&&a.CanChooseWeaponAbility();
            return false;
        }
        public static int AdjustmentStep(TrainingPoint point) {
            if(point.TrainPoint is SkillName||point.Weight<=0||point.Weight>=1)return 1;
            for(int step=1;step<=100;step++){decimal cost=step*(decimal)point.Weight;if(cost==decimal.Truncate(cost))return step;}
            return 100;
        }
        public static int AffordableMaximum(BaseCreature pet,TrainingPoint point) {
            int current=Current(pet,point),best=current,step=point.TrainPoint is SkillName?50:AdjustmentStep(point);
            int points=PetTrainingHelper.GetTrainingProfile(pet,true).TrainingPoints;
            for(int value=current+step;value<=point.GetMax(pet);value+=step){
                int cost=PetTrainingHelper.GetTotalCost(point,pet,value,current);if(cost>points)break;
                double increase=(value-current)*point.Weight;
                if(point.TrainPoint is ResistanceType&&PetTrainingHelper.GetTotalResistWeight(pet)+increase>PetTrainingHelper.GetTrainingCapTotal((ResistanceType)point.TrainPoint))break;
                if(point.TrainPoint is PetStat&&(PetStat)point.TrainPoint<=PetStat.Mana){var stat=(PetStat)point.TrainPoint;int total=stat<=PetStat.Int?PetTrainingHelper.GetTotalStatWeight(pet):PetTrainingHelper.GetTotalAttributeWeight(pet);if(total+increase>PetTrainingHelper.GetTrainingCapTotal(stat))break;}
                if(cost>0)best=value;
            }return best;
        }
        public static bool Purchase(Mobile owner, BaseCreature pet, TrainingPoint tp, int expected, int value) {
            if(!CanUse(owner,pet)||!Peaceful(owner,pet)||tp==null||!Available(pet,tp))return false;
            var profile=PetTrainingHelper.GetTrainingProfile(pet,true);
            int current=Current(pet,tp);
            if(profile.TrainingMode!=TrainingMode.Regular||!profile.CanApplyOptions||current!=expected||value<=current||value<tp.Start||value>tp.GetMax(pet)||
                (!profile.HasIncreasedControlSlot && owner.Followers>=owner.FollowersMax)||!PetTrainingHelper.CanControl(owner,pet,profile))return false;
            double increase=(value-current)*tp.Weight;
            if(tp.TrainPoint is PetStat && (PetStat)tp.TrainPoint<=PetStat.Mana) {
                var stat=(PetStat)tp.TrainPoint; int total=stat<=PetStat.Int?PetTrainingHelper.GetTotalStatWeight(pet):PetTrainingHelper.GetTotalAttributeWeight(pet);
                if(total+increase>PetTrainingHelper.GetTrainingCapTotal(stat))return false;
            }
            if(tp.TrainPoint is ResistanceType && PetTrainingHelper.GetTotalResistWeight(pet)+increase>PetTrainingHelper.GetTrainingCapTotal((ResistanceType)tp.TrainPoint))return false;
            int step=AdjustmentStep(tp);
            if((value-current)%step!=0)return false;
            int cost=PetTrainingHelper.GetTotalCost(tp,pet,value,current); if(cost<=0||cost>profile.TrainingPoints)return false;
            PowerScroll scroll=null;
            if(tp.TrainPoint is SkillName) {
                if(value!=50&&value!=100&&value!=150&&value!=200)return false;
                if(owner.Backpack==null)return false;
                scroll=owner.Backpack.Items.OfType<PowerScroll>().FirstOrDefault(s=>!s.Deleted&&s.Skill==(SkillName)tp.TrainPoint&&s.Value==100+value/10);
                if(scroll==null)return false;
            }
            if(!PetTrainingHelper.ApplyTrainingPoint(pet,tp,value))return false;
            if(scroll!=null)scroll.Delete();
            profile.OnTrain((PlayerMobile)owner,cost);pet.InvalidateProperties();
            Server.Engines.Quests.TeachingSomethingNewQuest.CheckComplete((PlayerMobile)owner);
            return true;
        }
    }
    public class HavenPetTrainingGump : HavenPetMenuGump
    {
        readonly BaseCreature _pet; readonly int _category,_page; readonly List<TrainingPoint> _options;
        const int Rows=10;
        public HavenPetTrainingGump(BaseCreature pet,int category=0,int page=0):base(20,30) {
            _pet=pet;_category=Math.Max(0,Math.Min(HavenPetTrainingMenu.Categories.Length-1,category));_options=HavenPetTrainingMenu.Options(pet,_category);_page=Math.Max(0,Math.Min(Math.Max(0,(_options.Count-1)/Rows),page));
            var profile=PetTrainingHelper.GetTrainingProfile(pet,true);
            AddBackground(0,0,740,590,3000);AddLabel(24,20,0,"ANIMAL TRAINING - "+pet.Name);
            double progress=profile.TrainingProgressMax<=0?0:profile.TrainingProgressPercentile*100;
            AddLabel(24,52,0,"Slots "+pet.ControlSlots+" / "+pet.ControlSlotsMax+"    Combat progress "+progress.ToString("F1")+"%    Points "+profile.TrainingPoints);
            AddLabel(24,82,0,profile.CanApplyOptions?"Ready to choose upgrades":profile.HasBegunTraining?"Training active - fight suitable enemies":"Begin training to earn upgrade points");
            AddBackground(18,116,198,296,0xBB8);AddBackground(220,116,500,296,0xBB8);AddLabel(24,126,53,"CATEGORIES");AddLabel(230,126,53,"SELECTIONS");
            for(int i=0;i<HavenPetTrainingMenu.Categories.Length;i++)FlatButton(24,157+i*31,184,10+i,(_category==i?"[":"")+HavenPetTrainingMenu.Categories[i]+(_category==i?"]":""));
            if(_options.Count==0)AddLabel(230,164,0,"No eligible options for this pet in this category.");
            for(int row=0;row<Rows;row++) { int index=_page*Rows+row;if(index>=_options.Count)break;var tp=_options[index];int y=164+row*25;
                if(tp.Name.Number>0)AddHtmlLocalized(230,y,240,24,tp.Name.Number,false,false);else AddLabel(230,y,0,tp.Name.String??tp.TrainPoint.ToString());
                if(tp.Description.Number>0)AddTooltip(tp.Description.Number);
                AddLabel(480,y,0,HavenPetTrainingMenu.ValueText(pet,tp));FlatButton(590,y,120,100+index,"Choose upgrade",tp.Description.Number);
            }
            if(_page>0)FlatButton(230,426,100,2,"Previous");AddLabel(359,426,0,"Page "+(_page+1)+" / "+Math.Max(1,(_options.Count+Rows-1)/Rows));if((_page+1)*Rows<_options.Count)FlatButton(590,426,120,3,"Next");
            AddHtml(24,464,686,42,"Train through combat to 100%, then spend points. The first purchase adds a follower slot. Skill caps require matching power scrolls.",false,false);
            FlatButton(24,518,184,1,profile.HasBegunTraining?"Training status":"Begin training");FlatButton(230,518,180,4,"Finish stage");FlatButton(432,518,130,5,"Animal Lore");FlatButton(590,518,120,6,"Refresh");FlatButton(24,554,184,7,"Plan training");FlatButton(230,554,180,8,"Training info");FlatButton(590,554,120,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info) {
            var p=sender.Mobile;int id=info.ButtonID;if(id==0||!HavenPetTrainingMenu.CanUse(p,_pet))return;
            var profile=PetTrainingHelper.GetTrainingProfile(_pet,true);
            if(id>=10&&id<10+HavenPetTrainingMenu.Categories.Length){HavenPetTrainingMenu.Show(p,_pet,id-10);return;}
            if(id==7){BaseGump.SendGump(new HavenPetTrainingPlanningGump((PlayerMobile)p,_pet));return;}if(id==8){BaseGump.SendGump(new HavenPetTrainingInfoGump((PlayerMobile)p));return;}if(id==5){HavenAnimalLoreGump.DisplayTo(p,_pet);return;}
            if(id==1&&!profile.HasBegunTraining){if(HavenPetTrainingMenu.Peaceful(p,_pet)&&_pet.ControlSlots<_pet.ControlSlotsMax)profile.BeginTraining();else p.SendMessage("Leave combat and check available training stages.");}
            if(id==4){p.SendGump(new HavenPetUpgradeGump(_pet,null,0,_category,_page));return;}
            if(id>=100&&id-100<_options.Count){var tp=_options[id-100];p.SendGump(new HavenPetUpgradeGump(_pet,tp,HavenPetTrainingMenu.Current(_pet,tp),_category,_page));return;}
            HavenPetTrainingMenu.Show(p,_pet,_category,_page+(id==2?-1:id==3?1:0));
        }
    }
    public class HavenPetUpgradeGump : HavenPetMenuGump
    {
        readonly BaseCreature _pet;readonly TrainingPoint _point;readonly int _expected,_category,_page,_value;
        public HavenPetUpgradeGump(BaseCreature pet,TrainingPoint point,int expected,int category,int page,int value=-1):base(80,80) {
            _pet=pet;_point=point;_expected=expected;_category=category;_page=page;
            _value=point==null?0:value<0?(point.Start==point.Max?point.Start:expected):Math.Max(expected,Math.Min(point.GetMax(pet),value));
            var profile=PetTrainingHelper.GetTrainingProfile(pet,true);
            AddBackground(0,0,620,455,3000);AddLabel(24,20,0,point==null?"Finish training stage":"TRAINING CONFIRMATION");
            if(point==null){AddHtml(24,65,560,85,"Finishing discards all remaining training points. This cannot be undone.",false,false);FlatButton(24,390,280,1,"Confirm finish stage");}
            else {
                AddBackground(20,55,280,125,0xBB8);AddBackground(312,55,288,125,0xBB8);
                if(point.Name.Number>0)AddHtmlLocalized(32,69,254,30,point.Name.Number,false,false);else AddLabel(32,69,0,point.Name.String??point.TrainPoint.ToString());
                AddLabel(32,113,0,"Weight per point: "+point.Weight.ToString("0.##"));
                if(point.Description.Number>0)AddHtmlLocalized(324,69,260,95,point.Description.Number,false,true);else AddHtml(324,69,260,95,HavenMenuText.Encode(point.Description.String??""),false,true);
                AddBackground(20,192,280,130,0xBB8);AddBackground(312,192,288,130,0xBB8);
                AddLabel(32,204,53,"REQUIREMENTS");AddHtml(32,234,254,77,point.TrainPoint is SkillName?"Matching power scroll in your main backpack. Completed combat training, enough points and follower capacity.":"Completed combat training, enough points and follower capacity. Native pet limits apply.",false,false);
                int cost=PetTrainingHelper.GetTotalCost(point,pet,_value,expected);
                AddLabel(324,204,53,"RESULTS");AddLabel(324,232,0,"Points: "+profile.TrainingPoints+" - "+cost+" = "+(profile.TrainingPoints-cost));
                AddLabel(324,260,0,"Current: "+HavenPetTrainingMenu.ValueText(pet,point)+"   Result: "+Result(_value));
                AddLabel(324,288,0,"Limit: "+Result(point.GetMax(pet)));
                if(point.Start!=point.Max){int[] delta=point.TrainPoint is SkillName?new[]{-50,50}:new[]{-10*Step(),-Step(),Step(),10*Step()};for(int i=0;i<delta.Length;i++)FlatButton(24+i*112,340,102,100+i,(delta[i]>0?"+":"")+(point.TrainPoint is SkillName?delta[i]/10:delta[i]));FlatButton(472,340,122,2,"Max affordable");}
                if(point.TrainPoint is MagicalAbility)AddLabel(24,370,0,"Choosing a magic school can replace its current school.");
                FlatButton(24,415,270,1,profile.TrainingMode==TrainingMode.Planning?"Add to training plan":"Confirm training purchase");
            }
            FlatButton(464,415,130,0,"Back");
        }
        string Result(int value){return _point.TrainPoint is SkillName?(100+value/10.0).ToString("F1"):_point.Start==_point.Max?"Learned":value.ToString();}
        int Step(){return HavenPetTrainingMenu.AdjustmentStep(_point);}
        public override void OnResponse(NetState sender,RelayInfo info){var p=sender.Mobile;if(!HavenPetTrainingMenu.CanUse(p,_pet))return;
            if(info.ButtonID==2&&_point!=null&&_point.Start!=_point.Max){p.SendGump(new HavenPetUpgradeGump(_pet,_point,_expected,_category,_page,HavenPetTrainingMenu.AffordableMaximum(_pet,_point)));return;}
            if(info.ButtonID>=100&&_point!=null&&_point.Start!=_point.Max){int[] delta=_point.TrainPoint is SkillName?new[]{-50,50}:new[]{-10*Step(),-Step(),Step(),10*Step()};int i=info.ButtonID-100;if(i<delta.Length)p.SendGump(new HavenPetUpgradeGump(_pet,_point,_expected,_category,_page,_value+delta[i]));return;}
            if(info.ButtonID==1){var profile=PetTrainingHelper.GetTrainingProfile(_pet,true);
                if(_point==null){if(profile.CanApplyOptions&&HavenPetTrainingMenu.Peaceful(p,_pet))profile.EndTraining();else p.SendMessage("Complete combat training before finishing this stage.");}
                else if(profile.TrainingMode==TrainingMode.Planning){if(_value>_expected&&HavenPetTrainingMenu.Available(_pet,_point)){PetTrainingHelper.GetPlanningProfile(_pet,true).AddToPlan(_point.TrainPoint,_value,PetTrainingHelper.GetTotalCost(_point,_pet,_value,_expected));p.SendMessage("Added to your plan. No training points or scrolls spent.");}}
                else p.SendMessage(HavenPetTrainingMenu.Purchase(p,_pet,_point,_expected,_value)?"Pet training applied.":"No change: check completed training, points, limits, follower capacity and the matching scroll in your main backpack.");
            }
            HavenPetTrainingMenu.Show(p,_pet,_category,_page);
        }
    }
}
