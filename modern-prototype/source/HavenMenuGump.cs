using Server;
using Server.Gumps;
namespace Server.HavenPrototype {
 public static class HavenMenuText {
  // The game client's HTML renderer does not decode apostrophe entities.
  public static string Encode(string text){return (System.Security.SecurityElement.Escape(text??string.Empty)??string.Empty).Replace("&apos;", "'");}
 }

 public class HavenMenuGump:Gump {
  public HavenMenuGump(int x,int y):base(x,y){}
  public void FlatButton(int x,int y,int width,int id,string text,int tooltip=0){
   width=System.Math.Max(38,width);
   for(int offset=0;offset<width;offset+=19){base.AddButton(x+System.Math.Min(offset,width-19),y,210,210,id,GumpButtonType.Reply,0);if(tooltip>0)AddTooltip(tooltip);}
   base.AddImageTiled(x,y,width,19,5058);if(tooltip>0)AddTooltip(tooltip);
   base.AddHtml(x,y+1,width,18,"<CENTER><BASEFONT COLOR=#FFFFFF>"+HavenMenuText.Encode(text)+"</BASEFONT></CENTER>",false,false);if(tooltip>0)AddTooltip(tooltip);
  }
  protected virtual bool CompactButtons => false;
  protected virtual bool PetTheme => false;
  public new void AddButton(int x,int y,int normal,int pressed,int id,GumpButtonType type,int page){base.AddButton(x,y,!CompactButtons&&normal==0xFA5?2151:normal,!CompactButtons&&pressed==0xFA7?2152:pressed,id,type,page);}
  public new void AddBackground(int x,int y,int w,int h,int art){if(PetTheme){if(x==0&&y==0){base.AddBackground(x,y,w,h,5054);base.AddImageTiled(x+12,y+12,w-24,h-24,2624);}else base.AddAlphaRegion(x,y,w,h);}else base.AddBackground(x,y,w,h,art==0x13BE||art==0xA28?3000:art);}
  public new void AddLabel(int x,int y,int hue,string text){base.AddLabel(x,y,PetTheme?(hue==0||hue==1152?(y<=26?53:1152):hue):(hue==1152?0:hue),text);}
  public new void AddHtml(int x,int y,int w,int h,string text,bool background,bool scrollbar){if(PetTheme)base.AddHtml(x,y,w,h,"<BASEFONT COLOR=#FFFFFF>"+text.Replace("#181818","#FFFFFF").Replace("#3B2A1A","#FFFFFF").Replace("#342B23","#FFFFFF")+"</BASEFONT>",false,scrollbar);else base.AddHtml(x,y,w,h,text.Replace("#FFFFFF","#3B2A1A").Replace("#F2F2F2","#3B2A1A"),background,scrollbar);}
  public new void AddTextEntry(int x,int y,int w,int h,int hue,int id,string text){base.AddTextEntry(x,y,w,h,PetTheme&&hue==0?1152:hue,id,text);}
  public new void AddHtmlLocalized(int x,int y,int w,int h,int number,bool background,bool scrollbar){if(PetTheme)base.AddHtmlLocalized(x,y,w,h,number,0xFFFFFF,false,scrollbar);else base.AddHtmlLocalized(x,y,w,h,number,background,scrollbar);}
  public static void ItemArrow(Gump g,Mobile viewer,Item item,int x,int y,int id){bool compact=g is HavenMenuGump menu&&menu.CompactButtons;g.AddButton(x,y,compact?0xFA5:2151,compact?0xFA7:2152,id,GumpButtonType.Reply,0);item.SendPropertiesTo(viewer);g.AddItemProperty(item.Serial);}
 }
 public class HavenPetMenuGump:HavenMenuGump {public HavenPetMenuGump(int x,int y):base(x,y){}protected override bool PetTheme=>true;}
}
