using System;
using System.Linq;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public static class HavenPetTamingSkills
 {
  // Called by the native taming loss path, before ownership is assigned.
  public static bool Scale(BaseCreature pet,double scalar,bool firstTame)
  {
   if(!HavenPreview.Enabled||pet==null||pet is HavenCompanion)return false;
   var record=HavenLegendaryPetSkills.Find(pet);
   var rolled=record==null?new SkillName[0]:record.Boosted.ToArray();
   for(int i=0;i<pet.Skills.Length;i++)
   {
    var skill=pet.Skills[i];double value=skill.Base;
    if(firstTame)skill.Cap=rolled.Contains((SkillName)i)?Math.Max(125,skill.Cap):100;
    skill.Base=Math.Min(skill.Cap,(firstTame?Math.Min(100,value):value)*Math.Max(0,Math.Min(1,scalar)));
   }
   if(record!=null)record.MarkTamed();
   pet.InvalidateProperties();return true;
  }
 }
}
