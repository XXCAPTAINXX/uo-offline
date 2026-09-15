using System.Linq;
using Server.Network;
using Server.Gumps;

namespace Server.HavenPrototype
{
    public sealed class HavenEncounterSpoilsGump : HavenStoneGump
    {
        readonly HavenEncounterChest _chest;
        public HavenEncounterSpoilsGump(HavenEncounterChest chest) : base(80, 80)
        {
            _chest = chest;
            AddBackground(0, 0, 500, 350, 9270);
            AddLabel(25, 22, 1152, "Encounter spoils");
            AddLabel(25, 55, 2101, "Collect within 28 tiles. Full packs leave remaining loot here.");
            var names = chest.Items.Select(i => System.Net.WebUtility.HtmlEncode(i.Name ?? i.GetType().Name) + " x" + i.Amount);
            AddHtml(25, 90, 450, 190, "<BASEFONT COLOR=#FFFFFF>" + string.Join("<BR>", names) + "</BASEFONT>", false, true);
            FlatButton(25, 305, 220, 1, "Collect spoils");
            FlatButton(350, 305, 100, 0, "Close");
        }
        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID != 1 || !_chest.NearbyClaim(state.Mobile)) return;
            _chest.Collect(state.Mobile);
            state.Mobile.SendGump(new HavenEncounterSpoilsGump(_chest));
        }
    }
}
