using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public abstract partial class HavenAbyssMaterial : Item, ICommodity
{
    protected HavenAbyssMaterial(int art, int amount) : base(art)
    { Stackable = true; Weight = 0.1; Amount = amount; }
    public virtual int ResourceHue => 0;
    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
