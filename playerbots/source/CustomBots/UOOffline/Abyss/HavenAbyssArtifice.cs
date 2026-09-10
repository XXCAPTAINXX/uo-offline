using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

internal sealed record HavenAbyssRecipe(string Name, Type Essence, Type First, Type Second, AosAttribute Attribute, int Bonus, int Cap);

[SerializationGenerator(0)]
public partial class HavenAbyssArtifice : Item
{
    internal static readonly HavenAbyssRecipe[] Recipes =
    [
        new("Precision: damage increase +5",typeof(EssencePrecision),typeof(LavaSerpentCrust),typeof(DaemonClaw),AosAttribute.WeaponDamage,5,50),
        new("Diligence: stamina regen +1",typeof(EssenceDiligence),typeof(FaeryDust),typeof(FeyWings),AosAttribute.RegenStam,1,5),
        new("Achievement: spell damage +2",typeof(EssenceAchievement),typeof(VoidOrb),typeof(DaemonClaw),AosAttribute.SpellDamage,2,12),
        new("Balance: defense chance +2",typeof(EssenceBalance),typeof(CrystallineBlackrock),typeof(ArcanicRuneStone),AosAttribute.DefendChance,2,15),
        new("Singularity: lower mana cost +2",typeof(EssenceSingularity),typeof(VialOfVitriol),typeof(BottleIchor),AosAttribute.LowerManaCost,2,8),
        new("Direction: faster cast recovery +1",typeof(EssenceDirection),typeof(UndyingFlesh),typeof(SilverSnakeSkin),AosAttribute.CastRecovery,1,3),
        new("Feeling: health regen +1",typeof(EssenceFeeling),typeof(SeedOfRenewal),typeof(FeyWings),AosAttribute.RegenHits,1,5),
        new("Order: attack chance +2",typeof(EssenceOrder),typeof(LavaSerpentCrust),typeof(DelicateScales),AosAttribute.AttackChance,2,15),
        new("Control: lower reagent cost +5",typeof(EssenceControl),typeof(GoblinBlood),typeof(SpiderCarapace),AosAttribute.LowerRegCost,5,20),
        new("Persistence: maximum health +3",typeof(EssencePersistence),typeof(UndyingFlesh),typeof(ReflectiveWolfEye),AosAttribute.BonusHits,3,5),
        new("Passion: mana regen +1",typeof(EssencePassion),typeof(DaemonClaw),typeof(LavaSerpentCrust),AosAttribute.RegenMana,1,5)
    ];
    [Constructible]
    public HavenAbyssArtifice() : base(0xFB1) { Name="Abyss artificer's forge"; Movable=false; }
    internal bool CanUse(Mobile from) => !Deleted && from?.Deleted==false && from.Alive && from.Map==Map && from.InRange(this,3) && from.InLOS(this);
    internal static bool Eligible(Mobile from, Item item) => item?.Deleted==false && item.Movable && item.IsChildOf(from.Backpack) && item is IAosItem &&
        !HavenGearExperience.IsSpecial(item) && !Attuned(item);
    internal static bool Attuned(Item item)
    { foreach(var child in item.Items) { if(child is HavenAbyssAttunement) { return true; } } return false; }
    internal bool Apply(Mobile from,Item item,int index)
    {
        if(!CanUse(from) || !Eligible(from,item) || index<0 || index>=Recipes.Length || CraftSkill(from)<80) { return false; }
        var recipe=Recipes[index]; var attributes=((IAosItem)item).Attributes;
        if(recipe.Attribute==AosAttribute.WeaponDamage && item is not BaseWeapon || attributes[recipe.Attribute]+recipe.Bonus>recipe.Cap) { return false; }
        if(from.Backpack.GetAmount(recipe.Essence)<8 || from.Backpack.GetAmount(recipe.First)<2 || from.Backpack.GetAmount(recipe.Second)<2) { return false; }
        // Validation precedes all consumption; the game loop cannot interleave another purchase here.
        from.Backpack.ConsumeTotal(recipe.Essence,8); from.Backpack.ConsumeTotal(recipe.First,2); from.Backpack.ConsumeTotal(recipe.Second,2);
        item.AddItem(new HavenAbyssAttunement { Recipe=index }); attributes[recipe.Attribute]+=recipe.Bonus; item.InvalidateProperties();
        from.SendMessage($"Attuned: {recipe.Name}. This equipment keeps its normal progression rules."); return true;
    }
    private static double CraftSkill(Mobile from) => Math.Max(Math.Max(from.Skills.Blacksmith.Base,from.Skills.Tailoring.Base),Math.Max(from.Skills.Tinkering.Base,from.Skills.Inscribe.Base));
    public override void OnDoubleClick(Mobile from)
    {
        if(!CanUse(from)) { return; }
        from.SendMessage("Select ordinary equipment in your backpack. Attunement needs 80 Blacksmithing, Tailoring, Tinkering or Inscription. One permanent attunement per item.");
        from.Target=new GearTarget(this);
    }
    private sealed class GearTarget : Target
    {
        private readonly HavenAbyssArtifice _forge;
        public GearTarget(HavenAbyssArtifice forge):base(3,false,TargetFlags.None) { _forge=forge; }
        protected override void OnTarget(Mobile from,object targeted)
        {
            if(_forge.CanUse(from) && targeted is Item item && Eligible(from,item)) { from.SendGump(new ArtificeGump(_forge,item)); }
            else { from.SendMessage("Choose unattuned ordinary equipment in your pack. Evolving rewards keep their own upgrade system."); }
        }
    }
    private sealed class ArtificeGump : Gump
    {
        private readonly HavenAbyssArtifice _forge; private readonly Item _gear; private readonly int _page;
        public ArtificeGump(HavenAbyssArtifice forge,Item gear,int page=0):base(45,45)
        {
            _forge=forge; _gear=gear; _page=Math.Clamp(page,0,1); AddBackground(0,0,670,440,9270); AddLabel(25,20,1152,"Abyss artifice — one permanent attunement");
            AddHtml(25,50,620,42,"<BASEFONT COLOR=#FFFFFF>Each choice uses 8 essences and 2 of each listed material.<BR>Requires 80 in a crafting skill. This is Haven's compatible crafting system.</BASEFONT>");
            var attributes=((IAosItem)gear).Attributes;
            for(var row=0;row<6 && _page*6+row<Recipes.Length;row++)
            {
                var i=_page*6+row; var r=Recipes[i];var y=105+row*46; AddButton(25,y,4005,4007,100+i); AddLabel(65,y,1152,r.Name);
                AddLabel(465,y,2101,$"Now {attributes[r.Attribute]}; cap {r.Cap}");
                AddLabel(65,y+21,2101,$"{r.First.Name} + {r.Second.Name}");
            }
            AddButton(25,399,4005,4007,1); AddLabel(65,399,1152,_page==0 ? "Next" : "Previous");
            AddLabel(250,399,2101,$"Page {_page+1} / 2"); AddButton(530,399,4017,4019,0); AddLabel(570,399,1152,"Close");
        }
        public override void OnResponse(NetState state,in RelayInfo info)
        {
            if(info.ButtonID==0) { return; }
            if(info.ButtonID==1 && _forge.CanUse(state.Mobile) && Eligible(state.Mobile,_gear))
            { state.Mobile.SendGump(new ArtificeGump(_forge,_gear,1-_page)); return; }
            if(!_forge.Apply(state.Mobile,_gear,info.ButtonID-100))
            { state.Mobile.SendMessage("Check your crafting skill, materials, item eligibility and property cap. Nothing was consumed."); }
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenAbyssAttunement : Item
{
    [SerializableField(0)] private int _recipe;
    [Constructible] public HavenAbyssAttunement():base(1) { Visible=false; Movable=false; Weight=0; Name="Abyss attunement"; }
    public override bool IsVirtualItem=>true;
}
