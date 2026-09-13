using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.CannedEvil;
using Server.Multis;
using Server.Network;
using Server.Gumps;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public sealed class HavenTravelStop
    {
        public string Name;
        public string Detail;
        public int Index = -1;
        public Item Source;
        public Map Map { get { return Source != null ? Source.Map : HavenPreview.Destinations[Index].Map; } }
        public bool Travel(Mobile from)
        {
            if (Index >= 0) return HavenPreview.Travel(from, Index);
            if (!HavenPreview.CanTravel(from) || Source == null || Source.Deleted || Source.Map == null || Source.Map == Map.Internal) return false;
            // Find a clear approach with line of sight; enemies may still be nearby.
            for (int r = 12; r <= 24; r++)
                for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx),Math.Abs(dy)) != r) continue;
                    int x = Source.X + dx;
                    int y = Source.Y + dy;
                    if (x < 0 || y < 0 || x >= Map.Width || y >= Map.Height) continue;
                    foreach(int z in new[]{Source.Z,Map.GetAverageZ(x,y)}.Distinct()) {
                    var p = new Point3D(x,y,z);
                    if (!Map.CanFit(p,16,true,true) || BaseHouse.FindHouseAt(p,Map,20)!=null || !Map.LineOfSight(new Point3D(p.X,p.Y,p.Z+14),new Point3D(Source.X,Source.Y,Source.Z+14))) continue;
                    BaseCreature.TeleportPets(from,p,Map);from.MoveToWorld(p,Map);return true; }
                }
            return false;
        }
    }
    public class PreviewGump : HavenStoneGump
    {
        public static readonly string[] Categories = {"Towns & gateways","Dungeons","Champions","Hunting","Island & services"};
        private readonly int _category, _page;
        private readonly HavenTravelStop[] _stops;
        public static HavenTravelStop[] Stops(int category)
        {
            var list=new List<HavenTravelStop>();
            int[][] groups={new[]{0,1,2,10,11,12,13,14,15,16,17,18,19,27,28,29},new[]{3,4,5,6,7,8,9,20,21,22,23,24,25,26},new int[0],new[]{30,31,32,36,37},new[]{33,34,35}};
            foreach(int i in groups[category])list.Add(new HavenTravelStop{Name=HavenPreview.Destinations[i].Name,Index=i,Detail=""});
            if(category==2)
            {
                foreach(var c in World.Items.Values.OfType<ChampionSpawn>().Where(c=>!c.Deleted&&c.Map!=null&&c.Map!=Map.Internal))
                    list.Add(new HavenTravelStop{Name=string.IsNullOrWhiteSpace(c.SpawnName)?c.Type.ToString():c.SpawnName,Source=c,Detail=(c.Active?"Active":"Dormant")+" | "+c.Type+" | "+c.X+", "+c.Y});
                foreach(var c in World.Items.Values.OfType<HavenMiniChamp>().Where(c=>!c.Deleted&&c.Map!=null&&c.Map!=Map.Internal))
                    list.Add(new HavenTravelStop{Name=c is HavenCoveEncounter?"Blackwake Cove mini-champ":"Haven mini-champ",Source=c,Detail=c.Active?"Encounter active":"Mini-champ camp"});
            }
            return list.OrderBy(s=>s.Name).ThenBy(s=>s.Map.Name).ToArray();
        }
        public PreviewGump(int page=0,int category=0):base(10,20)
        {
            _category=Math.Max(0,Math.Min(Categories.Length-1,category));_stops=Stops(_category);
            _page=Math.Max(0,Math.Min(Math.Max(0,(_stops.Length-1)/8),page));
            AddBackground(0,0,780,570,0xA28);AddLabel(24,20,0,"TRAVEL STONE");
            AddLabel(24,50,0,"Choose a destination. Nearby followers travel with you.");
            for(int c=0;c<Categories.Length;c++)FlatButton(24,95+c*43,190,10+c,(_category==c?"> ":"")+Categories[c]);
            AddLabel(238,80,0,Categories[_category]+" | "+_stops.Length+" destinations");
            for(int row=0;row<8;row++)
            {
                int index=_page*8+row;if(index>=_stops.Length)break;var stop=_stops[index];int y=113+row*45;
                AddHtml(238,y,420,20,"<BASEFONT COLOR=#202020>"+System.Security.SecurityElement.Escape(stop.Name)+"</BASEFONT>",false,false);
                AddHtml(238,y+18,420,20,"<BASEFONT COLOR=#202020>"+System.Security.SecurityElement.Escape(stop.Map.Name+(stop.Map==Map.Felucca?" | PvP":"")+(stop.Detail.Length>0?" | "+stop.Detail:""))+"</BASEFONT>",false,false);
                FlatButton(670,y,80,100+index,"Travel");
            }
            if(_stops.Length==0)AddLabel(238,130,0,"No champion locations are installed on this world.");
            AddLabel(238,482,0,"Page "+(_page+1)+" / "+Math.Max(1,(_stops.Length+7)/8));
            if(_page>0)FlatButton(400,480,100,3,"Previous");
            if((_page+1)*8<_stops.Length)FlatButton(510,480,100,4,"Next");
            FlatButton(24,480,190,5,"Refresh locations");FlatButton(650,520,100,0,"Close");
            AddLabel(24,525,0,"Leave combat before travel. Champion camps may have nearby enemies.");
        }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var from=sender.Mobile;if(!HavenPreview.Enabled||from==null||info.ButtonID==0)return;
            int category=_category,page=_page;
            if(info.ButtonID>=10&&info.ButtonID<15){category=info.ButtonID-10;page=0;}
            else if(info.ButtonID==3)page--;else if(info.ButtonID==4)page++;
            else if(info.ButtonID>=100&&info.ButtonID<100+_stops.Length)
            {
                if(!HavenPreview.CanTravel(from))from.SendMessage(from.Alive?"Wait until recent combat has ended before traveling.":"You must be alive to use the travel stone.");
                else if(!_stops[info.ButtonID-100].Travel(from))from.SendMessage("No clear landing is available at that destination. Refresh locations or choose another stop.");
            }
            from.SendGump(new PreviewGump(page,category));
        }
    }
}




