using Server;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public partial class HavenCompanion
 {
  Mobile _explicitPetTarget;bool _checkingPetOrder;
  static bool WildCustomPet(Mobile target){var pet=target as BaseCreature;return pet!=null&&!pet.Deleted&&pet.Alive&&pet.Tamable&&!pet.Controlled&&!pet.Summoned&&HavenPetSignatures.Kind(pet)!=0;}
  public override bool CanBeHarmful(IDamageable target,bool message,bool ignoreOurBlessedness){if(WildCustomPet(target as Mobile)&&!_checkingPetOrder&&!_calmingAnimal&&!(_explicitPetTarget==target&&ControlOrder==OrderType.Attack&&ControlTarget==target))return false;return base.CanBeHarmful(target,message,ignoreOurBlessedness);}
  void RespectWildPets(){if(ControlOrder!=OrderType.Attack||ControlTarget!=_explicitPetTarget)_explicitPetTarget=null;if(WildCustomPet(Combatant as Mobile)&&Combatant!=_explicitPetTarget){Combatant=null;FocusMob=null;if(ControlOrder==OrderType.Attack){ControlTarget=BoundOwner;ControlOrder=OrderType.Follow;}}}
 }
}
