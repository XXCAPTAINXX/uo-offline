using Server;
using Server.Gumps;
namespace Server.HavenPrototype {
 public class HavenMenuGump:Gump {
  public HavenMenuGump(int x,int y):base(x,y){}
  public void FlatButton(int x,int y,int width,int id,string text){
   width=System.Math.Max(38,width);
   for(int offset=0;offset<width;offset+=19)base.AddButton(x+System.Math.Min(offset,width-19),y,210,210,id,GumpButtonType.Reply,0);
   base.AddImageTiled(x,y,width,19,5058);
   base.AddHtml(x,y+1,width,18,"<CENTER><BASEFONT COLOR=#FFFFFF>"+System.Security.SecurityElement.Escape(text)+"</BASEFONT></CENTER>",false,false);
  }
  protected virtual bool CompactButtons => false;
  public new void AddButton(int x,int y,int normal,int pressed,int id,GumpButtonType type,int page){base.AddButton(x,y,!CompactButtons&&normal==0xFA5?2151:normal,!CompactButtons&&pressed==0xFA7?2152:pressed,id,type,page);}
  public new void AddBackground(int x,int y,int w,int h,int art){base.AddBackground(x,y,w,h,art==0x13BE||art==0xA28?3000:art);}
  public new void AddLabel(int x,int y,int hue,string text){base.AddLabel(x,y,hue==1152?0:hue,text);}
  public new void AddHtml(int x,int y,int w,int h,string text,bool background,bool scrollbar){base.AddHtml(x,y,w,h,text.Replace("#FFFFFF","#3B2A1A").Replace("#F2F2F2","#3B2A1A"),background,scrollbar);}
  public static void ItemArrow(Gump g,Mobile viewer,Item item,int x,int y,int id){bool compact=g is HavenMenuGump menu&&menu.CompactButtons;g.AddButton(x,y,compact?0xFA5:2151,compact?0xFA7:2152,id,GumpButtonType.Reply,0);item.SendPropertiesTo(viewer);g.AddItemProperty(item.Serial);}
 }
}
