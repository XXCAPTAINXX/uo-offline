using Server.Items;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public class HavenUniversalDyeTub : DyeTub
    {
        [Constructable]
        public HavenUniversalDyeTub() { Name = "Universal dye tub"; }
        public HavenUniversalDyeTub(Serial serial) : base(serial) { }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Unlimited uses. Use dyes on this tub to choose a color.");
            list.Add("Colors items in your backpack, including weapons, armor and furniture.");
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack)) { from.SendMessage("Place the tub in your backpack to use it."); return; }
            from.SendMessage("Choose an item in your backpack to color. Use dyes on the tub to change its color.");
            from.Target = new DyeTarget(this);
        }
        private class DyeTarget : Target
        {
            private readonly HavenUniversalDyeTub _tub;
            public DyeTarget(HavenUniversalDyeTub tub) : base(1, false, TargetFlags.None) { _tub = tub; }
            protected override void OnTarget(Mobile from, object target)
            {
                var item = target as Item;
                if (_tub.Deleted || !_tub.IsChildOf(from.Backpack)) return;
                if (item == null || item.Deleted || item == _tub || !item.Movable || !item.IsChildOf(from.Backpack))
                { from.SendMessage("Choose a movable item inside your own backpack."); return; }
                item.Hue = _tub.DyedHue;
                from.PlaySound(0x23E);
            }
        }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
