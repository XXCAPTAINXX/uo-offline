using System;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.HavenPrototype
{
    public static class HavenMarks
    {
        public const int Maximum=1000000;
        private static string Key(Mobile owner) {return "Haven.Marks:"+owner.Serial.Value;}
        public static bool CanUse(Mobile owner) {return HavenPreview.Enabled && owner!=null && !owner.Deleted && owner.Player && owner.Alive && owner.Account is Account;}
        public static int Balance(Mobile owner) {
            int value;var account=owner==null?null:owner.Account as Account;
            return account!=null && int.TryParse(account.GetTag(Key(owner)),out value) && value>=0 && value<=Maximum ? value:0;
        }
        // Completed missions can pay while the owner is offline or dead; spending requires life.
        public static int Award(Mobile owner,int amount) {
            var account=owner==null?null:owner.Account as Account;
            if(!HavenPreview.Enabled || owner==null || owner.Deleted || !owner.Player || account==null || amount<=0) return 0;
            int current=Balance(owner),paid=Math.Min(amount,Maximum-current);
            account.SetTag(Key(owner),(current+paid).ToString());return paid;
        }
        private const string TestAllowanceKey="Haven.MarksTestAllowance";
        public static bool CanClaimTestAllowance(Mobile owner) {
            return CanUse(owner) && ((Account)owner.Account).GetTag(TestAllowanceKey)!="claimed";
        }
        public static bool ClaimTestAllowance(Mobile owner) {
            if(!CanClaimTestAllowance(owner))return false;
            if(Balance(owner)>Maximum-100){owner.SendMessage("Spend some Marks before claiming the test allowance.");return false;}
            if(Award(owner,100)!=100)return false;
            ((Account)owner.Account).SetTag(TestAllowanceKey,"claimed");
            owner.SendMessage("Added 100 test Marks. This optional allowance is once per account.");
            return true;
        }
        public static readonly string[] Names={"Corsair's cutlass","Deckbreaker's mace","Boarding shield","Voyager's robe","Prospector's gloves","Beastkeeper's gorget"};
        public static readonly int[] Prices={80,80,60,80,60,100};
        public static readonly string[] Descriptions={
            "One-handed sword: 60% Hit Mana Leech, 30% Hit Life Leech, +30% Damage Increase, +10% Hit Chance Increase. A step above the starter weapon.",
            "One-handed mace: 60% Hit Mana Leech, 30% Hit Life Leech, +30% Damage Increase, +10% Hit Chance Increase. Pair with a shield for a basher build.",
            "Evolves from level 1 to 20 while worn on credited kills. Starts with +10 Parrying, +15% Defense Chance, +10% Hit Chance, +15 Hits, +10 Stamina, +3 Hit/Mana/Stamina Regen, +5% Lower Mana Cost and +10 all resists. Native caps apply.",
            "Robe: 100% Lower Reagent Cost, +10% Lower Mana Cost, +10% Spell Damage Increase and +3 Mana Regeneration. Native attribute limits still apply.",
            "Leather gloves: +5 Mining, +5 Lumberjacking and +100 Luck. Skill bonuses do not raise caps; companion mission tiers use trained base skill.",
            "Leather gorget: +5 Animal Taming, +5 Animal Lore, +5 Veterinary and +5 Intelligence. Skill bonuses do not raise skill caps."
        };
        public static Item CreateReward(int index) {
            Item item;
            if(index==0 || index==1) {
                BaseWeapon weapon=index==0?(BaseWeapon)new Broadsword():new WarMace();
                weapon.WeaponAttributes.HitLeechMana=60;weapon.WeaponAttributes.HitLeechHits=30;
                weapon.Attributes.WeaponDamage=30;weapon.Attributes.AttackChance=10;item=weapon;
            } else if(index==2) {
                var shield=new MetalKiteShield();HavenBoardingShieldUpgrade.Apply(shield);item=shield;
            } else if(index==3) {
                var robe=new Robe();robe.Attributes.LowerRegCost=100;robe.Attributes.LowerManaCost=10;robe.Attributes.SpellDamage=10;robe.Attributes.RegenMana=3;item=robe;
            } else if(index==4) {
                var gloves=new LeatherGloves();gloves.SkillBonuses.SetValues(0,SkillName.Mining,5);gloves.SkillBonuses.SetValues(1,SkillName.Lumberjacking,5);gloves.Attributes.Luck=100;item=gloves;
            } else if(index==5) {
                var gorget=new LeatherGorget();gorget.SkillBonuses.SetValues(0,SkillName.AnimalTaming,5);gorget.SkillBonuses.SetValues(1,SkillName.AnimalLore,5);gorget.SkillBonuses.SetValues(2,SkillName.Veterinary,5);gorget.Attributes.BonusInt=5;item=gorget;
            } else return null;
            item.Name=Names[index];item.Hue=0x489;HavenGearDurability.Apply(item);if(index==2)HavenEquipmentEvolution.Attach(item,0).Apply();return item;
        }
        public static bool Spend(Mobile owner,int amount) {if(!CanUse(owner) || amount<=0 || Balance(owner)<amount)return false;((Account)owner.Account).SetTag(Key(owner),(Balance(owner)-amount).ToString());return true;}
        public static bool Buy(Mobile owner,int index,Item preview=null) {
            if(!CanUse(owner) || index<0 || index>=Prices.Length || owner.Backpack==null) return false;
            int balance=Balance(owner);
            if(balance<Prices[index]) {owner.SendMessage("You need "+Prices[index]+" Haven Marks for that reward.");return false;}
            var item=preview??CreateReward(index);if(item.Deleted)return false;
            if(!owner.Backpack.CheckHold(owner,item,false,true)) {item.Delete();owner.SendMessage("Make room in your pack. No Marks were spent.");return false;}
            owner.Backpack.DropItem(item);
            ((Account)owner.Account).SetTag(Key(owner),(balance-Prices[index]).ToString());
            owner.SendMessage("Purchased "+Names[index]+" for "+Prices[index]+" Haven Marks.");return true;
        }
        public static void Initialize() {CommandSystem.Register("havenmarks",AccessLevel.Player,e=>Show(e.Mobile));}
        public static void Show(Mobile owner,int selected=0) {if(!CanUse(owner))return;owner.CloseGump(typeof(HavenMarksGump));owner.SendGump(new HavenMarksGump(owner,selected));}
    }
    public class HavenMarksGump:Gump
    {
        private readonly int _selected;
        private HavenShopPreviewHolder _holder; private Item _preview;
        private void Cleanup(){if(_holder!=null&&!_holder.Deleted)_holder.Delete();}
        public override void OnServerClose(NetState state){Cleanup();base.OnServerClose(state);}
        public HavenMarksGump(Mobile owner,int selected):base(45,45) {
            _selected=Math.Max(0,Math.Min(HavenMarks.Names.Length-1,selected));
            AddBackground(0,0,650,420,0xA28);
            AddLabel(20,16,0,"Haven rewards");AddLabel(400,16,0,"Marks: "+HavenMarks.Balance(owner));
            AddHtml(20,50,610,50,"<BASEFONT COLOR=#342B23>Earn 2 Marks per completed companion mission minute: 10 for a 5-minute run. Recalled-early missions award none.</BASEFONT>",false,false);
            _holder=new HavenShopPreviewHolder();
            for(int i=0;i<HavenMarks.Names.Length;i++) {var item=HavenMarks.CreateReward(i);_holder.DropItem(item);if(i==_selected)_preview=item;HavenMenuGump.ItemArrow(this,owner,item,20,112+i*39,100+i);AddLabel(54,112+i*39,0,HavenMarks.Names[i]);}
            AddItem(300,112,_preview.ItemID,_preview.Hue);_preview.SendPropertiesTo(owner);AddItemProperty(_preview.Serial);
            AddLabel(350,114,0,HavenMarks.Prices[_selected]+" Marks");
            AddHtml(290,165,335,142,"<BASEFONT COLOR=#342B23>"+HavenMarks.Descriptions[_selected]+"</BASEFONT>",false,false);
            HavenMenuGump.ItemArrow(this,owner,_preview,290,321,1);AddLabel(324,321,0,"Buy selected reward");
            if(HavenMarks.CanClaimTestAllowance(owner)) {
                AddButton(20,370,0xFA5,0xFA7,2,GumpButtonType.Reply,0);
                AddLabel(54,370,0,"Claim 100 test Marks (once per account)");
            } else AddLabel(20,370,0,"Test allowance claimed. Missions earn more Marks.");
            AddButton(545,370,0xFA5,0xFA7,0,GumpButtonType.Reply,0);AddLabel(579,370,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info) {
            if(info.ButtonID==0 || !HavenMarks.CanUse(sender.Mobile)){Cleanup();return;}
            if(info.ButtonID==1)HavenMarks.Buy(sender.Mobile,_selected,_preview);
            else if(info.ButtonID==2)HavenMarks.ClaimTestAllowance(sender.Mobile);
            Cleanup();HavenMarks.Show(sender.Mobile,info.ButtonID>=100 && info.ButtonID<100+HavenMarks.Names.Length?info.ButtonID-100:_selected);
        }
    }
}


