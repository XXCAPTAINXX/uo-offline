using Server.Items;

namespace Server.HavenPrototype
{
    public class HavenGravefireScimitar : HavenCycloneScimitar
    {
        [Constructable] public HavenGravefireScimitar() { Name = "Gravefire Scimitar"; Hue = 0x48D; Slayer = SlayerName.Exorcism; Slayer2 = SlayerName.Silver; }
        public HavenGravefireScimitar(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class HavenGravefireMace : HavenCycloneMace
    {
        [Constructable] public HavenGravefireMace() { Name = "Gravefire Mace"; Hue = 0x48D; Slayer = SlayerName.Exorcism; Slayer2 = SlayerName.Silver; }
        public HavenGravefireMace(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
}
