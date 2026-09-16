using Server.Gumps;
using System;
using System.Linq;
using Server.Items;
using Server.Network;
using Server.Targeting;
namespace Server.HavenPrototype
{
 public class HavenAbyssAttunement : Item
 {
  public override bool IsVirtualItem {get{return true;}}
  public int RecipeIndex;
  public HavenAbyssAttunement(int recipe):base(1){RecipeIndex=recipe;Visible=false;Movable=false;Weight=0;}
  public HavenAbyssAttunement(Serial serial):base(serial){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(RecipeIndex);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();RecipeIndex=r.ReadInt();}
 }
 public class HavenSmallSoulForge : BaseAddon
 {
  public override BaseAddonDeed Deed {get{return new HavenSmallSoulForgeDeed();}}
  [Constructable] public HavenSmallSoulForge(){Name="Abyss artificer's soul forge";AddComponent(new ForgeComponent(17607),0,0,0);}
  public override void OnComponentUsed(AddonComponent c,Mobile from){HavenAbyssArtifice.Open(this,from);}
  public HavenSmallSoulForge(Serial serial):base(serial){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenSmallSoulForgeDeed : BaseAddonDeed
 {
  public override BaseAddon Addon {get{return new HavenSmallSoulForge();}}
  [Constructable] public HavenSmallSoulForgeDeed(){Name="Abyss artificer's soul forge deed";LootType=LootType.Blessed;}
  public HavenSmallSoulForgeDeed(Serial serial):base(serial){}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Scalis rare reward: 11 permanent gear attunements");list.Add("Requires 80 crafting skill, Abyss essences and materials");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public static class HavenAbyssArtifice
 {
  public sealed class Recipe
  {
   public string Name;public Type Essence,First,Second;public AosAttribute Attribute;public int Bonus,Cap;
   public Recipe(string name,Type essence,Type first,Type second,AosAttribute attribute,int bonus,int cap){Name=name;Essence=essence;First=first;Second=second;Attribute=attribute;Bonus=bonus;Cap=cap;}
  }
  public static readonly Recipe[] Recipes={
        new Recipe("Precision: damage increase +5",typeof(EssencePrecision),typeof(LavaSerpentCrust),typeof(DaemonClaw),AosAttribute.WeaponDamage,5,50),
        new Recipe("Diligence: stamina regen +1",typeof(EssenceDiligence),typeof(FaeryDust),typeof(FeyWings),AosAttribute.RegenStam,1,5),
        new Recipe("Achievement: spell damage +2",typeof(EssenceAchievement),typeof(VoidOrb),typeof(DaemonClaw),AosAttribute.SpellDamage,2,12),
        new Recipe("Balance: defense chance +2",typeof(EssenceBalance),typeof(CrystallineBlackrock),typeof(ArcanicRuneStone),AosAttribute.DefendChance,2,15),
        new Recipe("Singularity: lower mana cost +2",typeof(EssenceSingularity),typeof(VialOfVitriol),typeof(BottleIchor),AosAttribute.LowerManaCost,2,8),
        new Recipe("Direction: faster cast recovery +1",typeof(EssenceDirection),typeof(UndyingFlesh),typeof(SilverSnakeSkin),AosAttribute.CastRecovery,1,3),
        new Recipe("Feeling: health regen +1",typeof(EssenceFeeling),typeof(SeedOfRenewal),typeof(FeyWings),AosAttribute.RegenHits,1,5),
        new Recipe("Order: attack chance +2",typeof(EssenceOrder),typeof(LavaSerpentCrust),typeof(DelicateScales),AosAttribute.AttackChance,2,15),
        new Recipe("Control: lower reagent cost +5",typeof(EssenceControl),typeof(GoblinBlood),typeof(SpiderCarapace),AosAttribute.LowerRegCost,5,20),
        new Recipe("Persistence: maximum health +3",typeof(EssencePersistence),typeof(UndyingFlesh),typeof(ReflectiveWolfEye),AosAttribute.BonusHits,3,5),
        new Recipe("Passion: mana regen +1",typeof(EssencePassion),typeof(DaemonClaw),typeof(LavaSerpentCrust),AosAttribute.RegenMana,1,5)
  };
  public static bool CanUse(Item forge,Mobile from){return HavenPreview.Enabled&&forge!=null&&!forge.Deleted&&forge.Parent==null&&from!=null&&!from.Deleted&&from.Alive&&from.Map==forge.Map&&from.InRange(forge,3)&&from.InLOS(forge);}
  public static bool Eligible(Mobile from,Item gear){return from!=null&&from.Backpack!=null&&gear!=null&&!gear.Deleted&&gear.Movable&&gear.IsChildOf(from.Backpack)&&HavenAdvancedGear.Attributes(gear)!=null&&!(gear is IHavenStarterGear)&&HavenAdvancedGear.Find(gear)==null&&HavenAdvancedGear.AutoKind(gear)==0&&HavenEquipmentEvolution.Find(gear)==null&&!gear.Items.Any(i=>i is HavenAbyssAttunement);}
  public static bool Apply(Item forge,Mobile from,Item gear,int index)
  {
   if(!CanUse(forge,from)||!Eligible(from,gear)||index<0||index>=Recipes.Length)return false;
   if(Math.Max(Math.Max(from.Skills.Blacksmith.Base,from.Skills.Tailoring.Base),Math.Max(from.Skills.Tinkering.Base,from.Skills.Inscribe.Base))<80)return false;
   var r=Recipes[index];var a=HavenAdvancedGear.Attributes(gear);
   if((r.Attribute==AosAttribute.WeaponDamage&&!(gear is BaseWeapon))||a[r.Attribute]+r.Bonus>r.Cap)return false;
   var types=new[]{r.Essence,r.First,r.Second};var amounts=new[]{8,2,2};
   if(from.Backpack.ConsumeTotal(types,amounts,true)!=-1)return false;
   gear.AddItem(new HavenAbyssAttunement(index));a[r.Attribute]+=r.Bonus;gear.InvalidateProperties();
   from.SendMessage("Attunement complete: "+r.Name);return true;
  }
  public static void Open(Item forge,Mobile from)
  {
   if(!CanUse(forge,from))return;
   from.SendMessage("Target ordinary equipment in your backpack. Requires 80 Blacksmithing, Tailoring, Tinkering or Inscription. One attunement per item.");from.Target=new GearTarget(forge);
  }
  private sealed class GearTarget:Target
  {
   readonly Item Forge;public GearTarget(Item forge):base(3,false,TargetFlags.None){Forge=forge;}
   protected override void OnTarget(Mobile from,object target){var gear=target as Item;if(CanUse(Forge,from)&&Eligible(from,gear))from.SendGump(new Menu(Forge,gear));else from.SendMessage("Choose ordinary, unattuned equipment in your backpack. Evolving gear uses its own upgrades.");}
  }
  private sealed class Menu:HavenStoneGump
  {
   readonly Item Forge,Gear;readonly int Page,Selected;
   static string Words(Type t){return System.Text.RegularExpressions.Regex.Replace(t.Name,"([a-z])([A-Z])","$1 $2");}
   public static string Material(Item gear,Type type,int need){var owner=gear.RootParent as Mobile;int have=owner==null||owner.Backpack==null?0:owner.Backpack.GetAmount(type);return Words(type)+" "+have+"/"+need;}
   public Menu(Item forge,Item gear,int page=0,int selected=-1):base(45,45)
   {
    Forge=forge;Gear=gear;Page=Math.Max(0,Math.Min(1,page));Selected=selected;
    AddBackground(0,0,760,560,0xA28);AddLabel(24,18,0,"ABYSS ARTIFICE | One permanent attunement");
    AddLabel(24,48,0,"Requires 80 Blacksmithing, Tailoring, Tinkering or Inscription.");
    AddLabel(24,73,0,"Materials show owned / needed. Choose a recipe, then confirm to spend.");
    for(int row=0;row<6&&Page*6+row<Recipes.Length;row++)
    {
     int i=Page*6+row,y=110+row*52;var r=Recipes[i];FlatButton(24,y,410,100+i,(Selected==i?"Selected: ":"")+r.Name);
     AddLabel(455,y,0,"Current "+HavenAdvancedGear.Attributes(gear)[r.Attribute]+" / cap "+r.Cap);
     AddLabel(24,y+24,0,Material(gear,r.Essence,8)+" | "+Material(gear,r.First,2)+" | "+Material(gear,r.Second,2));
    }
    if(Selected>=0){AddLabel(24,438,0,"Confirm: "+Recipes[Selected].Name);FlatButton(24,475,340,2,"Confirm attunement and spend materials");}
    FlatButton(390,475,155,1,Page==0?"Next page":"Previous page");FlatButton(570,475,150,0,"Close");
   }
   public override void OnResponse(NetState state,RelayInfo info)
   {
    if(info.ButtonID==0||!CanUse(Forge,state.Mobile)||!Eligible(state.Mobile,Gear))return;
    if(info.ButtonID==1)state.Mobile.SendGump(new Menu(Forge,Gear,1-Page,Selected));
    else if(info.ButtonID>=100&&info.ButtonID<100+Recipes.Length)state.Mobile.SendGump(new Menu(Forge,Gear,Page,info.ButtonID-100));
    else if(info.ButtonID==2&&!Apply(Forge,state.Mobile,Gear,Selected)){state.Mobile.SendMessage("Check skill, materials and property cap. Nothing was spent.");state.Mobile.SendGump(new Menu(Forge,Gear,Page,Selected));}
   }
  }
 }
}
