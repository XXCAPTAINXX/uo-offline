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
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)foreach(var pet in World.Mobiles.Values.OfType<BaseCreature>().ToArray())if(HavenPetDefenses.Tier(pet)==3)Roll(pet);};}
  public IEnumerable<SkillName> Boosted { get { return _boosted.AsReadOnly(); } }
  private bool _expanded;
  readonly List<SkillName> _boosted=new List<SkillName>();
  public HavenLegendaryPetSkills():base(1){Visible=false;Movable=false;Weight=0;Name="legendary pet skill roll";}
  public HavenLegendaryPetSkills(Serial serial):base(serial){}
  public static bool Protects(BaseCreature pet,SkillName name){var record=pet.Backpack==null?null:pet.Backpack.FindItemByType(typeof(HavenLegendaryPetSkills),true) as HavenLegendaryPetSkills;return HavenPreview.Enabled&&HavenPetDefenses.Tier(pet)==3&&record!=null&&record._boosted.Contains(name);}
  private void Repair(BaseCreature pet){foreach(var name in _boosted){var skill=pet.Skills[name];skill.Cap=Math.Max(skill.Cap,Math.Max(125,skill.Base));skill.Base=Math.Max(125,skill.Base);}pet.InvalidateProperties();}
  internal static List<SkillName> EligibleSkills(IEnumerable<SkillName> excluded){return HavenPetMissions.TrainableSkills.Where(name=>!excluded.Contains(name)).ToList();}
  public static HavenLegendaryPetSkills Roll(BaseCreature pet){if(pet.Backpack==null||HavenPetDefenses.Tier(pet)!=3)return null;var record=pet.Backpack.FindItemByType(typeof(HavenLegendaryPetSkills),true) as HavenLegendaryPetSkills;if(record!=null && record._expanded){record.Repair(pet);return record;}if(record==null){record=new HavenLegendaryPetSkills();pet.Backpack.DropItem(record);}var eligible=EligibleSkills(record._boosted);int count=Math.Min(eligible.Count,Math.Max(0,Utility.RandomMinMax(3,5)-record._boosted.Count));for(int i=0;i<count;i++){int index=Utility.Random(eligible.Count);var name=eligible[index];eligible.RemoveAt(index);int value=Utility.RandomMinMax(125,150);pet.Skills[name].Cap=Math.Max(pet.Skills[name].Cap,value);pet.Skills[name].Base=Math.Max(pet.Skills[name].Base,value);record._boosted.Add(name);}record._expanded=true;record.Repair(pet);pet.InvalidateProperties();return record;}
  public static void AddProperties(BaseCreature pet,ObjectPropertyList list){if(pet.Backpack==null)return;var record=pet.Backpack.FindItemByType(typeof(HavenLegendaryPetSkills),true) as HavenLegendaryPetSkills;if(record==null)return;if(record._boosted.Count==0)list.Add("Legendary skill roll: no over-cap skills rolled");foreach(var name in record._boosted)list.Add("Legendary skill: "+pet.Skills[name].Name+" "+pet.Skills[name].Base.ToString("F1")+" / "+pet.Skills[name].Cap.ToString("F1"));}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(1);w.Write(_boosted.Count);foreach(var name in _boosted)w.Write((int)name);w.Write(_expanded);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);int version=r.ReadInt();int count=r.ReadInt();if(count<0||count>5)throw new InvalidOperationException("Invalid legendary skills");for(int i=0;i<count;i++)_boosted.Add((SkillName)r.ReadInt());_expanded=version>=1 && r.ReadBool();}
 }
}
