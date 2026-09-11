using System;
using Server;
using Server.Mobiles;
namespace Server.HavenPrototype
{
    public static class HavenPetDefenses
    {
        public static int Tier(BaseCreature pet){var rarity=HavenPetRarity.Find(pet);return rarity==null?0:rarity.Tier;}
        public static ResistanceType? Element(BaseCreature pet)
        {
            if(pet is HavenEmberwing)return ResistanceType.Fire;
            if(pet is HavenFrostmane)return ResistanceType.Cold;
            if(pet is HavenStormscale)return ResistanceType.Energy;
            return null;
        }
        public static int Resistance(BaseCreature pet,ResistanceType type,int normal)
        {
            if(Element(pet)!=type)return normal;
            int tier=Tier(pet);return Math.Max(normal,tier==3?100:tier==2?90:tier==1?80:75);
        }
        public static bool FullyImmune(Mobile target,int physical,int fire,int cold,int poison,int energy,int direct)
        {
            var pet=target as BaseCreature;
            if(pet==null||direct>0||physical>0||poison>0||Tier(pet)!=3)return false;
            var element=Element(pet);
            return element==ResistanceType.Fire?fire>0&&cold==0&&energy==0:
                element==ResistanceType.Cold?cold>0&&fire==0&&energy==0:
                element==ResistanceType.Energy&&energy>0&&fire==0&&cold==0;
        }
        public static void AddProperties(BaseCreature pet,ObjectPropertyList list)
        {
            var element=Element(pet);if(element==null)return;
            list.Add(Tier(pet)==3?"Innate "+element+" immunity; other elements and armor-ignore still work.":"Innate "+element+" defense: at least "+Resistance(pet,element.Value,0)+"%.");
        }
    }
}
