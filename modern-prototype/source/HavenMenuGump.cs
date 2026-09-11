using Server;
using Server.Gumps;
namespace Server.HavenPrototype {
 public class HavenMenuGump:Gump {
  public HavenMenuGump(int x,int y):base(x,y){}
  public new void AddBackground(int x,int y,int w,int h,int art){base.AddBackground(x,y,w,h,art==0x13BE||art==0xA28?3000:art);}
  public new void AddLabel(int x,int y,int hue,string text){base.AddLabel(x,y,hue==1152?0:hue,text);}
  public new void AddHtml(int x,int y,int w,int h,string text,bool background,bool scrollbar){base.AddHtml(x,y,w,h,text.Replace("#FFFFFF","#3B2A1A").Replace("#F2F2F2","#3B2A1A"),background,scrollbar);}
  public static void ItemArrow(Gump g,Mobile viewer,Item item,int x,int y,int id){g.AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);item.SendPropertiesTo(viewer);g.AddItemProperty(item.Serial);}
 }
}
