using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public class HavenPetTrainingBridge:Item
 {
  int _slots;readonly Dictionary<int,int> _earned=new Dictionary<int,int>();
  public HavenPetTrainingBridge():base(1){Visible=false;Movable=false;Weight=0;Name="Haven pet training progress";}
  public HavenPetTrainingBridge(Serial serial):base(serial){}
  public static TrainingDefinition Definition(BaseCreature pet)
  {
   if(HavenPetSignatures.Kind(pet)==0||PetTrainingHelper.Definitions==null)return null;
   Type template=pet is HavenSnowBear?typeof(PolarBear):pet is HavenChelonian?typeof(Alligator):pet is VampiricSteed?typeof(Nightmare):pet.GetType().BaseType;
   var def=PetTrainingHelper.Definitions.FirstOrDefault(x=>x.CreatureType==template);
   if(def==null)return null;
   if(pet is HavenSnowBear) {
    MagicalAbility magic=MagicalAbility.None;foreach(var ability in PetTrainingHelper.MagicalAbilities)magic|=ability;
    return new TrainingDefinition(pet.GetType(),def.Class,magic,PetTrainingHelper.Definitions.SelectMany(d=>d.SpecialAbilities??new SpecialAbility[0]).Distinct().ToArray(),PetTrainingHelper.Definitions.SelectMany(d=>d.WeaponAbilities??new WeaponAbility[0]).Distinct().ToArray(),PetTrainingHelper.Definitions.SelectMany(d=>d.AreaEffects??new AreaEffect[0]).Distinct().ToArray(),1,5);
   }
   return new TrainingDefinition(pet.GetType(),def.Class,def.MagicalAbilities,pet is HavenAncientHellhound && def.SpecialAbilities!=null?def.SpecialAbilities.Where(a=>a!=SpecialAbility.DragonBreath).ToArray():def.SpecialAbilities,def.WeaponAbilities,def.AreaEffects,1,5);
  }
  public static void Award(BaseCreature target,Mobile attacker,int damage)
  {
   var pet=attacker as BaseCreature;if(pet==null||HavenPetSignatures.Kind(pet)==0||!HavenPetSignatures.Active(pet)||damage<=0||target==pet||target.Controlled||target.Summoned||target.IsDeadPet)return;
   var owner=pet.ControlMaster;if(!(owner is PlayerMobile)||!owner.InRange(pet,12)||!owner.InLOS(pet)||pet.Skills.Wrestling.Base-target.Skills.Wrestling.Base>75)return;
   var profile=PetTrainingHelper.GetTrainingProfile(pet);if(profile==null||!profile.HasBegunTraining||profile.TrainingProgress>=profile.TrainingProgressMax||pet.ControlSlots>=pet.ControlSlotsMax)return;
   if(pet.Backpack==null)pet.AddItem(new Backpack());var record=pet.Backpack.FindItemByType(typeof(HavenPetTrainingBridge),true) as HavenPetTrainingBridge;
   if(record==null){record=new HavenPetTrainingBridge();pet.Backpack.DropItem(record);}
   if(record._slots!=pet.ControlSlots||profile.TrainingProgress==0){record._slots=pet.ControlSlots;record._earned.Clear();}
   int earned;record._earned.TryGetValue(target.Serial.Value,out earned);int limit=pet.ControlSlots<3?5000:2500;
   int gain=(int)Math.Min((long)Math.Min(damage,target.HitsMax)*3,Math.Max(0,limit-earned));if(gain<=0)return;
   record._earned[target.Serial.Value]=earned+gain;profile.TrainingProgress=Math.Min(profile.TrainingProgressMax,profile.TrainingProgress+gain*profile.TrainingProgressMax/10000.0);
   if(profile.TrainingProgress>=profile.TrainingProgressMax)owner.SendMessage(pet.Name+" completed combat training. Use Animal Lore to choose upgrades.");pet.InvalidateProperties();
  }
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_slots);w.Write(_earned.Count);foreach(var pair in _earned){w.Write(pair.Key);w.Write(pair.Value);}}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_slots=r.ReadInt();int count=r.ReadInt();if(count<0||count>100000)throw new InvalidOperationException("Invalid pet training record");for(int i=0;i<count;i++)_earned[r.ReadInt()]=r.ReadInt();}
 }
}
