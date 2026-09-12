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
        public static readonly string[] Categories = { "Stats", "Resists", "Magic skill caps", "Combat skill caps", "Abilities" };
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
            return !Server.Spells.SpellHelper.CheckCombat(owner) && !Server.Spells.SpellHelper.CheckCombat(pet) &&
                !pet.Aggressed.Any(a=>!a.Expired) && pet.Combatant == null;
        }
        public static List<TrainingPoint> Options(BaseCreature pet, int category) {
            var result = new List<TrainingPoint>();
            Action<object> add = o => { var tp=PetTrainingHelper.GetTrainingPoint(o); if(tp!=null)result.Add(tp); };
            if(category==0)foreach(PetStat stat in Enum.GetValues(typeof(PetStat)))add(stat);
            else if(category==1)foreach(ResistanceType resist in Enum.GetValues(typeof(ResistanceType)))add(resist);
            else if(category==2 || category==3)foreach(var skill in category==2?PetTrainingHelper.MagicSkills:PetTrainingHelper.CombatSkills) { if(pet.Skills[skill].Base>0)add(skill); }
            else {
                var def=PetTrainingHelper.GetTrainingDefinition(pet); if(def==null)return result;
                foreach(var ability in PetTrainingHelper.MagicalAbilities)if(PetTrainingHelper.ValidateTrainingPoint(pet,ability))add(ability);
                foreach(var ability in SpecialAbility.Abilities)if(ability!=null && PetTrainingHelper.ValidateTrainingPoint(pet,ability))add(ability);
                foreach(var ability in def.WeaponAbilities)if(ability!=null)add(ability);
                foreach(var ability in AreaEffect.Effects)if(ability!=null && PetTrainingHelper.ValidateTrainingPoint(pet,ability))add(ability);
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
            int step=tp.Weight>0 && tp.Weight<1 && !(tp.TrainPoint is SkillName)?Math.Max(1,(int)(tp.Weight*100)):1;
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
    public class HavenPetTrainingGump : HavenMenuGump
    {
        readonly BaseCreature _pet; readonly int _category,_page; readonly List<TrainingPoint> _options;
        const int Rows=7;
        public HavenPetTrainingGump(BaseCreature pet,int category=0,int page=0):base(20,30) {
            _pet=pet;_category=Math.Max(0,Math.Min(4,category));_options=HavenPetTrainingMenu.Options(pet,_category);_page=Math.Max(0,Math.Min(Math.Max(0,(_options.Count-1)/Rows),page));
            var profile=PetTrainingHelper.GetTrainingProfile(pet,true);
            AddBackground(0,0,740,590,3000);AddLabel(24,20,0,"ANIMAL TRAINING - "+pet.Name);
            double progress=profile.TrainingProgressMax<=0?0:profile.TrainingProgressPercentile*100;
            AddLabel(24,52,0,"Slots "+pet.ControlSlots+" / "+pet.ControlSlotsMax+"    Combat progress "+progress.ToString("F1")+"%    Points "+profile.TrainingPoints);
            AddLabel(24,82,0,profile.CanApplyOptions?"Ready to choose upgrades":profile.HasBegunTraining?"Training active - fight suitable enemies":"Begin training to earn upgrade points");
            AddLabel(24,126,0,"CATEGORIES");AddLabel(230,126,0,"SELECTIONS");
            for(int i=0;i<5;i++)FlatButton(24,165+i*44,184,10+i,(_category==i?"[":"")+HavenPetTrainingMenu.Categories[i]+(_category==i?"]":""));
            for(int row=0;row<Rows;row++) { int index=_page*Rows+row;if(index>=_options.Count)break;var tp=_options[index];int y=164+row*36;
                if(tp.Name.Number>0)AddHtmlLocalized(230,y,240,24,tp.Name.Number,false,false);else AddLabel(230,y,0,tp.Name.String??tp.TrainPoint.ToString());
                AddLabel(480,y,0,HavenPetTrainingMenu.ValueText(pet,tp));FlatButton(590,y,120,100+index,"Choose upgrade");
            }
            FlatButton(230,426,100,2,"Previous");AddLabel(359,426,0,"Page "+(_page+1)+" / "+Math.Max(1,(_options.Count+Rows-1)/Rows));FlatButton(590,426,120,3,"Next");
            AddHtml(24,464,686,42,"Train through combat to 100%, then spend points. The first purchase adds a follower slot. Skill caps require matching power scrolls.",false,false);
            FlatButton(24,518,184,1,profile.HasBegunTraining?"Training status":"Begin training");FlatButton(230,518,180,4,"Finish stage");FlatButton(432,518,130,5,"Animal Lore");FlatButton(590,518,120,6,"Refresh");FlatButton(590,554,120,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info) {
            var p=sender.Mobile;int id=info.ButtonID;if(id==0||!HavenPetTrainingMenu.CanUse(p,_pet))return;
            var profile=PetTrainingHelper.GetTrainingProfile(_pet,true);
            if(id>=10&&id<15){HavenPetTrainingMenu.Show(p,_pet,id-10);return;}
            if(id==5){HavenAnimalLoreGump.DisplayTo(p,_pet);return;}
            if(id==1&&!profile.HasBegunTraining){if(HavenPetTrainingMenu.Peaceful(p,_pet)&&_pet.ControlSlots<_pet.ControlSlotsMax)profile.BeginTraining();else p.SendMessage("Leave combat and check available training stages.");}
            if(id==4){p.SendGump(new HavenPetUpgradeGump(_pet,null,0,_category,_page));return;}
            if(id>=100&&id-100<_options.Count){var tp=_options[id-100];p.SendGump(new HavenPetUpgradeGump(_pet,tp,HavenPetTrainingMenu.Current(_pet,tp),_category,_page));return;}
            HavenPetTrainingMenu.Show(p,_pet,_category,_page+(id==2?-1:id==3?1:0));
        }
    }
    public class HavenPetUpgradeGump : HavenMenuGump
    {
        readonly BaseCreature _pet;readonly TrainingPoint _point;readonly int _expected,_category,_page;
        public HavenPetUpgradeGump(BaseCreature pet,TrainingPoint point,int expected,int category,int page):base(80,80) {
            _pet=pet;_point=point;_expected=expected;_category=category;_page=page;
            AddBackground(0,0,560,300,3000);AddLabel(24,20,0,point==null?"Finish training stage":"Choose training upgrade");
            if(point==null){AddHtml(24,65,510,85,"Finishing discards all remaining training points. This cannot be undone.",false,false);FlatButton(24,230,240,1,"Confirm finish stage");}
            else {
                if(point.Name.Number>0)AddHtmlLocalized(24,58,510,24,point.Name.Number,false,false);else AddLabel(24,58,0,point.Name.String??point.TrainPoint.ToString());
                if(point.Description.Number>0)AddHtmlLocalized(24,92,510,68,point.Description.Number,false,true);else AddHtml(24,92,510,68,point.Description.String??"",false,true);
                AddLabel(24,173,0,"Current: "+HavenPetTrainingMenu.ValueText(pet,point)+"    Max: "+(point.TrainPoint is SkillName?"120":point.GetMax(pet).ToString()));
                int[] values=Values();for(int i=0;i<values.Length;i++){int value=values[i];string label=point.TrainPoint is SkillName?"Cap "+(100+value/10):point.Start==point.Max?"Learn":"+"+(value-expected);int cost=PetTrainingHelper.GetTotalCost(point,pet,value,expected);FlatButton(24+i*128,207,120,100+i,label+" / "+cost+" pts");}
            }
            FlatButton(414,263,120,0,"Back");
        }
        int Step(){return _point.Weight>0&&_point.Weight<1?Math.Max(1,(int)(_point.Weight*100)):1;}
        int[] Values(){if(_point==null)return new int[0];return _point.TrainPoint is SkillName?new[]{50,100,150,200}.Where(v=>v>_expected).ToArray():_point.Start==_point.Max?new[]{_point.Start}:new[]{_expected+Step(),_expected+Step()*10}.Where(v=>v<=_point.GetMax(_pet)).ToArray();}
        public override void OnResponse(NetState sender,RelayInfo info){var p=sender.Mobile;if(!HavenPetTrainingMenu.CanUse(p,_pet))return;
            if(info.ButtonID==1&&_point==null){var profile=PetTrainingHelper.GetTrainingProfile(_pet,true);if(profile.CanApplyOptions&&HavenPetTrainingMenu.Peaceful(p,_pet))profile.EndTraining();else p.SendMessage("Complete combat training before finishing this stage.");}
            if(info.ButtonID>=100&&_point!=null){var values=Values();int index=info.ButtonID-100;bool ok=index<values.Length&&HavenPetTrainingMenu.Purchase(p,_pet,_point,_expected,values[index]);p.SendMessage(ok?"Pet training applied.":"No change: check completed training, points, limits, follower capacity and the matching scroll in your main backpack.");}
            HavenPetTrainingMenu.Show(p,_pet,_category,_page);
        }
    }
}
