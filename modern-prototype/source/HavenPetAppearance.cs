using System;
using Server;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public static class HavenPetAppearance
 {
  static readonly int[][] Palettes={new int[0],new[]{0x21E,0x1BB,0x02D,0x02E},new[]{0x1FB,0x198,0x00A,0x137},new[]{0x24B,0x1E8,0x05A,0x05B},new[]{0x237,0x1D4,0x046,0x047},new[]{0x255,0x1F2,0x064,0x191},new[]{0x200,0x19D,0x00F,0x074},new[]{0x250,0x1ED,0x05F,0x0C4},new[]{0x219,0x1B6,0x028,0x029},new[]{0x214,0x1B1,0x023,0x0EC},new[]{0x237,0x1D4,0x046,0x047}};
  public static int NaturalHue(BaseCreature pet){int kind=HavenPetSignatures.Kind(pet);return kind==0?pet.Hue:Palettes[kind][HavenPetDefenses.Tier(pet)];}
  public static void ApplyNaturalHue(BaseCreature pet){if(HavenPetSignatures.Kind(pet)!=0)pet.Hue=NaturalHue(pet);}
  public static void Shimmer(BaseCreature pet){if(HavenPetDefenses.Tier(pet)!=3||!HavenPetSignatures.Active(pet)||pet.Hidden||pet.ControlMaster.Hidden||pet.Combatant!=null)return;var state=HavenPetSignatures.Ensure(pet);if(DateTime.UtcNow<state.NextShimmer)return;state.NextShimmer=DateTime.UtcNow.AddSeconds(45);pet.FixedEffect(0x373A,10,12,NaturalHue(pet),0);}
 }
}
