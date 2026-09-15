using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPetDye : Item
{
    internal static readonly string[] Colors = ["Snow", "Midnight", "Ember", "Forest", "Ocean", "Amethyst", "Rose", "Gold", "Original color"];
    internal static readonly int[] Hues = [0x47F, 0x455, 0x489, 0x59B, 0x482, 0x48E, 0x48D, 0x8A5, 0];
    [Constructible]
    public HavenPetDye() : base(0xFAB) { Name = "pet dye - choose a color"; Weight = 1; Hue = 0x48E; }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Choose one of eight colors, or restore original color. One use; cosmetics only."}");
        list.Add($"{"Use on your living pet, dismounted mount, or shrunken pet. Stats and rarity are unchanged."}");
    }
    internal bool CanUse(Mobile from) => !Deleted && from?.Deleted == false && from.Alive && from.Backpack != null && IsChildOf(from.Backpack);
    public override void OnDoubleClick(Mobile from)
    {
        if (CanUse(from)) { from.CloseGump<Palette>(); from.SendGump(new Palette(this)); }
        else { from.SendMessage("Keep the pet dye in your backpack to use it."); }
    }
    internal bool Apply(Mobile from, object target, int color)
    {
        if (!CanUse(from) || color < 0 || color >= Colors.Length) { return false; }
        var token = target as ShrunkenPet;
        var pet = token != null ? token.Inspect(from) : target as BaseCreature;
        if (pet?.Deleted != false || !pet.Alive || pet.IsDeadPet || pet.Summoned || pet.Body.IsHuman ||
            pet is BaseMount { Rider: not null }) { return false; }
        if (token == null && (!pet.Controlled || pet.ControlMaster != from || pet.Map != from.Map || !from.InRange(pet, 3) || !from.InLOS(pet))) { return false; }
        if (token != null && token.Owner != from) { return false; }
        HavenPetAppearance.Refresh(pet);
        var record = pet.Backpack?.FindItemByType<HavenPetDyeRecord>();
        if (color == 8)
        {
            if (record == null) { from.SendMessage("This pet already has its original color. Your dye was kept."); return false; }
            pet.Hue = record.OriginalHue; record.Delete();
        }
        else
        {
            if (pet.Hue == Hues[color]) { from.SendMessage("This pet already has that color. Your dye was kept."); return false; }
            if (record == null)
            {
                if (pet.Backpack == null) { pet.AddItem(new Backpack()); }
                pet.Backpack.DropItem(new HavenPetDyeRecord { OriginalHue = pet.Hue });
            }
            pet.Hue = Hues[color];
        }
        if (token != null) { token.Hue = pet.Hue; }
        from.SendMessage($"{pet.Name}: {Colors[color]}. Its abilities, stats and rarity are unchanged.");
        Delete(); return true;
    }
    private sealed class Palette : Gump
    {
        private readonly HavenPetDye _dye;
        internal Palette(HavenPetDye dye) : base(60, 60)
        {
            _dye = dye; AddBackground(0, 0, 405, 440, 9270); AddLabel(24, 20, 1152, "Pet colors");
            AddHtml(24, 50, 350, 44, "<BASEFONT COLOR=#FFFFFF>Dyes override rarity colors. Original color restores<BR>the species' rarity shade. One use per bottle.</BASEFONT>");
            for (var i = 0; i < Colors.Length; i++)
            { var y = 102 + i * 32; AddButton(24, y, 4005, 4007, i + 1); AddItem(67, y, 0xFAB, Hues[i]); AddLabel(110, y + 2, 1152, Colors[i]); }
            AddButton(280, 404, 4017, 4019, 0); AddLabel(315, 404, 1152, "Close");
        }
        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (!_dye.CanUse(state.Mobile) || info.ButtonID < 1 || info.ButtonID > Colors.Length) { return; }
            state.Mobile.SendMessage($"Apply {Colors[info.ButtonID - 1]}: choose your pet or its shrunken token. Escape cancels without using the dye.");
            state.Mobile.Target = new DyeTarget(_dye, info.ButtonID - 1);
        }
    }
    private sealed class DyeTarget : Target
    {
        private readonly HavenPetDye _dye; private readonly int _color;
        internal DyeTarget(HavenPetDye dye, int color) : base(3, false, TargetFlags.None) { _dye = dye; _color = color; }
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!_dye.Apply(from, targeted, _color)) { from.SendMessage("Choose your own living pet nearby, or your shrunken pet. Dismount first. No dye was used."); }
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenPetDyeRecord : Item
{
    [SerializableField(0)] private int _originalHue;
    [Constructible]
    public HavenPetDyeRecord() : base(1) { Name = "original pet color"; Visible = false; Movable = false; Weight = 0; }
    public override bool IsVirtualItem => true;
}
