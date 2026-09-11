using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public class HavenLegendaryPetSkills:Item
 {
  readonly List<SkillName> _boosted=new List<SkillName>();
  public HavenLegendaryPetSkills():base(1){Visible=false;Movable=false;Weight=0;Name="legendary pet skill roll";}
  public HavenLegendaryPetSkills(Serial serial):base(serial){}
  public static HavenLegendaryPetSkills Roll(BaseCreature pet){if(pet.Backpack==null||HavenPetDefenses.Tier(pet)!=3)return null;var record=pet.Backpack.FindItemByType(typeof(HavenLegendaryPetSkills),true) as HavenLegendaryPetSkills;if(record!=null)return record;record=new HavenLegendaryPetSkills();pet.Backpack.DropItem(record);if(Utility.RandomDouble()>=0.5)return record;var eligible=HavenPetMissions.TrainableSkills.Where(name=>pet.Skills[name].Base>0).ToList();int count=Math.Min(eligible.Count,Utility.RandomMinMax(1,3));for(int i=0;i<count;i++){int index=Utility.Random(eligible.Count);var name=eligible[index];eligible.RemoveAt(index);int value=Utility.RandomMinMax(125,150);pet.Skills[name].Cap=Math.Max(pet.Skills[name].Cap,value);pet.Skills[name].Base=Math.Max(pet.Skills[name].Base,value);record._boosted.Add(name);}pet.InvalidateProperties();return record;}
  public static void AddProperties(BaseCreature pet,ObjectPropertyList list){if(pet.Backpack==null)return;var record=pet.Backpack.FindItemByType(typeof(HavenLegendaryPetSkills),true) as HavenLegendaryPetSkills;if(record==null)return;if(record._boosted.Count==0)list.Add("Legendary skill roll: no over-cap skills rolled");foreach(var name in record._boosted)list.Add("Legendary skill: "+pet.Skills[name].Name+" "+pet.Skills[name].Base.ToString("F1")+" / "+pet.Skills[name].Cap.ToString("F1"));}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_boosted.Count);foreach(var name in _boosted)w.Write((int)name);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();int count=r.ReadInt();if(count<0||count>3)throw new InvalidOperationException("Invalid legendary skills");for(int i=0;i<count;i++)_boosted.Add((SkillName)r.ReadInt());}
 }
}
