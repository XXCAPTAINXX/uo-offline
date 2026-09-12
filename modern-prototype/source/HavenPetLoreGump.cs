using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
namespace Server.HavenPrototype {
 public class HavenPetLoreGump:HavenMenuGump {
  readonly BaseCreature _pet;readonly int _tab,_page;
  static string Safe(string s){return System.Security.SecurityElement.Escape(s??"None");}
  void Text(int x,int y,int w,int h,string s){AddHtml(x,y,w,h,"<BASEFONT COLOR=#342B23>"+s+"</BASEFONT>",false,false);}
  public HavenPetLoreGump(BaseCreature pet,int tab=0,int page=0):base(50,50){
   _pet=pet;_tab=Math.Max(0,Math.Min(2,tab));_page=Math.Max(0,page);AddBackground(0,0,640,550,3000);
   Text(24,20,590,28,"<B>"+Safe(pet.Name)+" - pet lore</B>");
   Text(24,52,590,24,"Rarity: "+HavenPetRarity.Label(HavenPetDefenses.Tier(pet))+" | Slots: "+pet.ControlSlots+" | "+(pet.IsBonded?"Bonded":pet.Controlled?"Tamed, not bonded":"Wild"));
   FlatButton(24,88,130,10,tab==0?"Lore [selected]":"Lore");FlatButton(168,88,130,11,tab==1?"Combat [selected]":"Combat");FlatButton(312,88,130,12,tab==2?"Skills [selected]":"Skills");FlatButton(456,88,155,13,"Training / native lore");
   if(_tab==0){
    Text(24,128,580,28,"<B>Care and training</B>");
    Text(24,165,580,80,"Owner: "+Safe(pet.ControlMaster?.Name)+"<BR>Taming requirement: "+pet.MinTameSkill.ToString("F1")+" | Loyalty: "+pet.Loyalty+" / "+BaseCreature.MaxLoyalty+"<BR>Order: "+pet.ControlOrder+" | Preferred food: "+Safe(pet.FavoriteFood.ToString()));
    Text(24,253,580,28,"<B>Species ability</B>");Text(24,290,580,105,Safe(HavenPetSignatures.Describe(pet)));
    Text(24,410,580,70,"Skills shows trained values, effective values and caps. Training / native lore opens the server's full training controls and ability details.");
   }else if(_tab==1){
    var weapon=pet.Weapon as BaseWeapon;var skill=weapon==null?SkillName.Wrestling:weapon.GetUsedSkill(pet,true);
    Text(24,128,580,60,"Hits "+pet.Hits+" / "+pet.HitsMax+" | Mana "+pet.Mana+" / "+pet.ManaMax+" | Stamina "+pet.Stam+" / "+pet.StamMax+"<BR>Str "+pet.Str+" | Dex "+pet.Dex+" | Int "+pet.Int);
    Text(24,198,580,60,"<B>Attack</B><BR>Weapon skill: "+Safe(pet.Skills[skill].Name)+" | Base damage: "+pet.DamageMin+" - "+pet.DamageMax+" | AI: "+pet.AI);
    Text(24,270,580,80,"<B>Current resistances</B><BR>Physical "+pet.PhysicalResistance+"% | Fire "+pet.FireResistance+"% | Cold "+pet.ColdResistance+"%<BR>Poison "+pet.PoisonResistance+"% | Energy "+pet.EnergyResistance+"%");
    var profile=PetTrainingHelper.GetAbilityProfile(pet);var abilities=profile==null?"Native species abilities":String.Join(", ",profile.EnumerateAllAbilities().Select(x=>x.ToString()));
    Text(24,360,580,115,"<B>Combat abilities</B><BR>"+Safe(abilities)+"<BR>"+Safe(HavenPetSignatures.Describe(pet)));
   }else{
    var rows=Enumerable.Range(0,pet.Skills.Length).OrderBy(i=>pet.Skills[i].Name).ToArray();int pages=(rows.Length+11)/12;_page=Math.Min(_page,pages-1);
    Text(24,127,270,24,"<B>Skill</B>");Text(300,127,90,24,"<B>Trained</B>");Text(402,127,90,24,"<B>Effective</B>");Text(505,127,90,24,"<B>Cap</B>");
    for(int row=0;row<12;row++){int index=_page*12+row;if(index>=rows.Length)break;var sk=pet.Skills[rows[index]];int y=161+row*25;Text(24,y,270,24,Safe(sk.Name));Text(300,y,90,24,sk.Base.ToString("F1"));Text(402,y,90,24,sk.Value.ToString("F1"));Text(505,y,90,24,sk.Cap.ToString("F1"));}
    if(_page>0)FlatButton(24,477,110,2,"Previous");Text(258,477,130,24,"Page "+(_page+1)+" / "+pages);if(_page+1<pages)FlatButton(500,477,110,3,"Next");
   }
   FlatButton(24,516,120,1,"Refresh");FlatButton(500,516,110,0,"Close");
  }
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||_pet.Deleted||!p.Alive)return;if(p.Map!=_pet.Map||!p.InRange(_pet,8)||!p.InLOS(_pet)){p.SendMessage("Move closer to inspect this pet.");return;}if(info.ButtonID==13){if(PetTrainingHelper.Enabled&&p is PlayerMobile)BaseGump.SendGump(new NewAnimalLoreGump((PlayerMobile)p,_pet));else p.SendGump(new AnimalLoreGump(_pet));return;}p.SendGump(new HavenPetLoreGump(_pet,info.ButtonID>=10&&info.ButtonID<=12?info.ButtonID-10:_tab,info.ButtonID==2?_page-1:info.ButtonID==3?_page+1:info.ButtonID>=10?0:_page));}
 }
}
