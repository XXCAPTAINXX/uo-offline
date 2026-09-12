using Server.Gumps;

namespace Server.HavenPrototype
{
    // Stone services have their own material treatment; portable menus keep parchment.
    public class HavenStoneGump : Gump
    {
        public HavenStoneGump(int x, int y) : base(x, y) { }
        public new void AddBackground(int x, int y, int width, int height, int art)
        {
            base.AddBackground(x, y, width, height, art == 3000 || art == 0x13BE ? 0xA28 : art);
        }
        public new void AddLabel(int x, int y, int hue, string text)
        {
            base.AddLabel(x, y, hue == 1152 ? 0 : hue, text);
        }
        public new void AddHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
        {
            base.AddHtml(x, y, width, height, text.Replace("#FFFFFF", "#242522").Replace("#F2F2F2", "#242522"), background, scrollbar);
        }
        public void FlatButton(int x, int y, int width, int id, string text)
        {
            width = System.Math.Max(38, width);
            for (int offset = 0; offset < width; offset += 19)
                base.AddButton(x + System.Math.Min(offset, width - 19), y, 210, 210, id, GumpButtonType.Reply, 0);
            base.AddImageTiled(x, y, width, 19, 5058);
            string label = HavenMenuText.Encode(text);
            base.AddHtml(x + 1, y + 2, width - 2, 18, "<CENTER><BASEFONT COLOR=#000000>" + label + "</BASEFONT></CENTER>", false, false);
            base.AddHtml(x, y + 1, width, 18, "<CENTER><BASEFONT COLOR=#E4E0D3>" + label + "</BASEFONT></CENTER>", false, false);
        }
        public void ItemButton(Mobile viewer, Item item, int x, int y, int width, int id, string label)
        {
            FlatButton(x, y, width, id, label);
            item.SendPropertiesTo(viewer);
            AddItemProperty(item.Serial);
        }
    }
}
