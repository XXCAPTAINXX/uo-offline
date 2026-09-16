using System;
using System.Linq;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenHellhoundBreath
    {
        public static void Initialize() { EventSink.ServerStarted += () => { if (HavenPreview.Enabled) foreach (var pet in World.Mobiles.Values.OfType<HavenAncientHellhound>().ToArray()) Ensure(pet); }; }
        public static void Ensure(HavenAncientHellhound pet)
        {
            if (pet == null || pet.Deleted) return;
            var profile = PetTrainingHelper.GetAbilityProfile(pet);
            if (profile != null) profile.RemoveAbility(SpecialAbility.DragonBreath);
        }
        public static void Think(HavenAncientHellhound pet)
        {
            Ensure(pet);
            if (!HavenPreview.Enabled || pet.Deleted || !pet.Alive || pet.IsDeadPet || pet.Rider != null || pet.Frozen || pet.Paralyzed || pet.Combatant == null || SpecialAbility.DragonBreath.IsInCooldown(pet)) return;
            int damage = 0;
            SpecialAbility.DragonBreath.Trigger(pet, pet.Combatant as Mobile, ref damage);
        }
    }
}

