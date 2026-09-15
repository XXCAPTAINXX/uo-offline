using System;
using Server.Gumps;
using Server.Network;
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
            list.Add("Unlimited uses. Double-click to browse 3,000 colors.");
            list.Add("Colors items in your backpack, including weapons, armor and furniture.");
        }
        public override void OnDoubleClick(Mobile from) { ShowPalette(from, Math.Max(0, DyedHue - 1) / 60); }
        public bool CanUse(Mobile from) { return from != null && from.Alive && !Deleted && IsChildOf(from.Backpack); }
        public void ShowPalette(Mobile from, int page = 0)
        {
            if (!CanUse(from)) { from.SendMessage("Place the tub in your backpack to use it."); return; }
            from.CloseGump(typeof(Palette)); from.SendGump(new Palette(this, page));
        }
        public class Palette : Gump
        {
            readonly HavenUniversalDyeTub _tub; readonly int _page;
            public Palette(HavenUniversalDyeTub tub, int page) : base(80, 70)
            {
                _tub = tub; _page = Math.Max(0, Math.Min(49, page));
                AddBackground(0, 0, 680, 570, 5054);
                AddLabel(25, 22, 0, "Universal dye tub - Color library");
                AddLabel(25, 52, 0, "Selected hue: " + tub.DyedHue + "   |   3,000 colors");
                AddItem(575, 22, 0x1F03, tub.DyedHue);
                for (int i = 0; i < 60; i++)
                {
                    int hue = _page * 60 + i + 1, x = 28 + (i % 10) * 64, y = 100 + (i / 10) * 56;
                    AddButton(x, y, 210, 211, 100 + i, GumpButtonType.Reply, 0);
                    AddItem(x + 20, y - 4, 0x1766, hue);
                    AddLabel(x + 2, y + 22, 0, hue.ToString());
                }
                Button(25, 443, 1, "Previous"); AddLabel(270, 447, 0, "Page " + (_page + 1) + " / 50"); Button(525, 443, 2, "Next");
                AddLabel(25, 485, 0, "Hue number:"); AddBackground(125, 481, 90, 28, 3000); AddTextEntry(132, 485, 75, 22, 0, 1, tub.DyedHue.ToString());
                Button(230, 483, 3, "Use hue"); Button(360, 483, 4, "Remove dye");
                Button(25, 530, 5, "Dye an item"); Button(525, 530, 0, "Close");
            }
            void Button(int x, int y, int id, string label) { AddButton(x, y, 2445, 2445, id, GumpButtonType.Reply, 0); AddLabel(x + 8, y + 3, 0, label); }
            public override void OnResponse(NetState state, RelayInfo info)
            {
                var from = state.Mobile; if (!_tub.CanUse(from) || info.ButtonID == 0) return;
                int page = _page;
                if (info.ButtonID == 1) page--;
                else if (info.ButtonID == 2) page++;
                else if (info.ButtonID == 3)
                {
                    int hue;
                    if (!int.TryParse(info.GetTextEntry(1)?.Text, out hue) || hue < 0 || hue > 3000) { from.SendMessage("Choose a hue number from 0 to 3000."); _tub.ShowPalette(from, page); return; }
                    _tub.DyedHue = hue; page = Math.Max(0, hue - 1) / 60;
                }
                else if (info.ButtonID == 4) _tub.DyedHue = 0;
                else if (info.ButtonID == 5) { from.SendMessage("Choose an item in your backpack to color."); from.Target = new DyeTarget(_tub); return; }
                else if (info.ButtonID >= 100 && info.ButtonID < 160) _tub.DyedHue = _page * 60 + info.ButtonID - 99;
                else return;
                _tub.ShowPalette(from, page);
            }
        }
        private class DyeTarget : Target
        {
            private readonly HavenUniversalDyeTub _tub;
            public DyeTarget(HavenUniversalDyeTub tub) : base(1, false, TargetFlags.None) { _tub = tub; }
            protected override void OnTarget(Mobile from, object target)
            {
                var item = target as Item;
                if (!_tub.CanUse(from)) return;
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

