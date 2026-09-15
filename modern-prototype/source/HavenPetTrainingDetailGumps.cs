using System.Linq;
using Server.Gumps;
using Server.Mobiles;
namespace Server.HavenPrototype {
 internal static class HavenPetDetailTheme {
  internal static void Apply(Gump g){
   var entries=g.Entries.ToArray();g.Entries.Clear();
   foreach(var entry in entries){
    var background=entry as GumpBackground;
    if(background!=null&&background.X==0&&background.Y==0){background.GumpID=5054;g.Add(entry);g.AddImageTiled(12,12,background.Width-24,background.Height-24,2624);g.AddAlphaRegion(22,70,background.Width-44,background.Height-92);continue;}
    var label=entry as GumpLabel;if(label!=null)label.Hue=label.Hue==0?1152:53;
    var localized=entry as GumpHtmlLocalized;if(localized!=null){localized.Color=localized.Y<80?0xFFFF00:0xFFFFFF;localized.Background=false;}
    var html=entry as GumpHtml;if(html!=null){html.Text="<BASEFONT COLOR=#FFFFFF>"+html.Text+"</BASEFONT>";html.Background=false;}
    g.Add(entry);
   }
  }
 }
 public class HavenPetTrainingPlanningGump:PetTrainingPlanningGump {
  public HavenPetTrainingPlanningGump(PlayerMobile owner,BaseCreature pet):base(owner,pet){}
  public override void AddGumpLayout(){base.AddGumpLayout();HavenPetDetailTheme.Apply(this);}
  public override void OnResponse(RelayInfo info){if(HavenPetTrainingMenu.CanUse(User,Creature))base.OnResponse(info);}
 }
 public class HavenPetTrainingInfoGump:PetTrainingInfoGump {
  public HavenPetTrainingInfoGump(PlayerMobile owner):base(owner){}
  public override void AddGumpLayout(){base.AddGumpLayout();HavenPetDetailTheme.Apply(this);}
 }
}
