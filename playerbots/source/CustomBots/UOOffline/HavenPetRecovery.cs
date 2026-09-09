using System;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.UOOffline;

public static class HavenServiceSpeech
{
    public static void TrySpeak(BaseCreature npc, ref DateTime next, string first, string second, string third)
    {
        if (Core.Now < next) { return; }
        next = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(90, 180));
        foreach (var player in npc.Map.GetMobilesInRange<PlayerMobile>(npc.Location, 6))
        {
            if (player.NetState == null || player.Hidden) { continue; }
            npc.Say(Utility.Random(3) switch { 0 => first, 1 => second, _ => third });
            break;
        }
    }
}

public static class HavenPetRecovery
{
    public static void Begin(Mobile from, Mobile healer)
    {
        if (!InRange(from, healer))
        {
            from.SendMessage("Stand beside the veterinarian or stable master.");
            return;
        }
        from.SendMessage("Choose your nearby dead bonded pet. Resurrection is free.");
        from.Target = new PetTarget(healer);
    }

    private static bool InRange(Mobile from, Mobile healer) =>
        from?.Deleted == false && from.Player && healer?.Deleted == false &&
        healer.Map != null && healer.Map != Map.Internal && from.Map == healer.Map && from.InRange(healer, 3);

    internal static bool Resurrect(Mobile from, Mobile healer, BaseCreature pet)
    {
        if (!InRange(from, healer) || pet?.Deleted != false || !pet.Controlled || pet.ControlMaster != from ||
            !pet.IsBonded || !pet.IsDeadPet || pet.IsStabled || pet.Map != healer.Map || !pet.InRange(healer, 3))
        {
            from.SendMessage("Bring your own dead bonded pet beside the healer. Living pets and other players' pets cannot be selected.");
            return false;
        }
        if (!pet.Map.CanFit(pet.Location, 16, false, false))
        {
            from.SendMessage("Move your pet into a clear space first.");
            return false;
        }
        pet.ResurrectPet();
        pet.Hits = pet.HitsMax;
        pet.PlaySound(0x214);
        pet.FixedEffect(0x376A, 10, 16);
        from.SendMessage("Your companion lives again. No gold or pet skills were lost.");
        return true;
    }

    private sealed class PetTarget : Target
    {
        private readonly Mobile _healer;
        public PetTarget(Mobile healer) : base(3, false, TargetFlags.None) => _healer = healer;
        protected override void OnTarget(Mobile from, object targeted) => Resurrect(from, _healer, targeted as BaseCreature);
    }
}

public sealed class HavenPetResurrectionEntry : ContextMenuEntry
{
    public HavenPetResurrectionEntry() : base(6195, 3) { } // Resurrect
    public override void OnClick(Mobile from, IEntity target)
    {
        if (target is AnimalTrainer trainer) { HavenPetRecovery.Begin(from, trainer); }
    }
}

[SerializationGenerator(0)]
public partial class HavenPetHealer : BaseCreature
{
    private DateTime _nextSpeech = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(70, 120));

    [Constructible]
    public HavenPetHealer() : base(AIType.AI_Animal, FightMode.None)
    {
        Name = "Mira Willow";
        Title = "the veterinarian - free pet resurrection";
        Female = true;
        Body = 0x191;
        Hue = 0x83EA;
        CantWalk = true;
        Blessed = true;
        AddItem(new Robe(0x59B));
        AddItem(new Sandals());
    }

    public override void OnDoubleClick(Mobile from) => HavenPetRecovery.Begin(from, this);

    public override void OnThink()
    {
        base.OnThink();
        HavenServiceSpeech.TrySpeak(this, ref _nextSpeech, "Every faithful companion deserves another chance.", "Bring your fallen bonded pets close. I will tend to them.", "A scratch behind the ears does wonders. Sometimes a little magic helps, too.");
    }
}
