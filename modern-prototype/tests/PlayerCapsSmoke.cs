using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class PlayerCapsSmoke
{
    private static PlayerMobile _fixture; private static void Require(bool value){if(!value)throw new Exception("Player cap regression: total="+_fixture.Skills.Total+" counted="+HavenPlayerCaps.Counted(_fixture.Skills)+" taming="+_fixture.Skills.AnimalTaming.Base+" focus="+_fixture.Skills.Focus.Base+" lore="+_fixture.Skills.AnimalLore.Base+" snoop="+_fixture.Skills.Snooping.Base); }
    public static void Run(Action<string,Action> check,bool reload) {
        if(reload) {
            check("player caps and free skill budget survive reload",()=>{
                var restored=World.Mobiles.Values.OfType<PlayerMobile>().Single(x=>x.Name=="Cap fixture");
                _fixture=restored;Require(restored.StatCap>=300 && restored.Skills.Cap==12000 && HavenPlayerCaps.Counted(restored.Skills)==12000);
            });return;
        }
        var owner=new PlayerMobile {Player=true,Name="Cap fixture",Body=0x190,RawStr=50};owner.AddItem(new Backpack());
        new Account("caps-fixture",Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
        check("raising caps preserves trained stats skills and individual caps",()=>{
            _fixture=owner;var total=owner.Skills.Total;HavenPlayerCaps.Apply(owner);
            Require(owner.RawStr==50 && owner.Skills.Total==total && owner.StatCap==300 && owner.Skills.Cap==12000 && owner.Skills.AnimalTaming.Cap==100);
        });
        int filled=0;Skill blocked=null;
        for(int index=0;index<owner.Skills.Length;index++) { var skill=owner.Skills[index];
            skill.SetLockNoRelay(SkillLock.Locked);skill.Base=0;
            if(HavenPlayerCaps.IsFree(owner,skill))continue;
            if(filled++<12)skill.Base=100;else if(blocked==null)blocked=skill;
        }
        check("all 25 published free skills gain at counted cap and respect individual caps",()=>{
            var expected=new[]{SkillName.Alchemy,SkillName.AnimalLore,SkillName.AnimalTaming,SkillName.ArmsLore,SkillName.Begging,SkillName.Camping,SkillName.Cartography,SkillName.Cooking,SkillName.DetectHidden,SkillName.Fishing,SkillName.Fletching,SkillName.Focus,SkillName.Forensics,SkillName.Herding,SkillName.Hiding,SkillName.Inscribe,SkillName.ItemID,SkillName.Lockpicking,SkillName.Lumberjacking,SkillName.Mining,SkillName.Musicianship,SkillName.RemoveTrap,SkillName.Snooping,SkillName.TasteID,SkillName.Tracking};
            Require(owner.Skills.Count(x=>HavenPlayerCaps.IsFree(owner,x))==expected.Length);
            foreach(var name in expected) {
                var skill=owner.Skills[name];Require(HavenPlayerCaps.IsFree(owner,skill));
                skill.SetLockNoRelay(SkillLock.Up);Server.Misc.SkillCheck.Gain(owner,skill,10);
                Require(skill.Base>0 && HavenPlayerCaps.Counted(owner.Skills)==12000);
                skill.Base=skill.Cap;Server.Misc.SkillCheck.Gain(owner,skill,10);Require(skill.Base==skill.Cap);
                skill.Base=0;skill.SetLockNoRelay(SkillLock.Locked);
            }
            var creature=new Rat();Require(!HavenPlayerCaps.IsFree(creature,creature.Skills.Mining));creature.Delete();
        });
        check("free natural skill gain works at counted cap",()=>{
            owner.Skills.AnimalTaming.SetLockNoRelay(SkillLock.Up);
            Server.Misc.SkillCheck.Gain(owner,owner.Skills.AnimalTaming,10);
            Require(owner.Skills.AnimalTaming.Base>0 && HavenPlayerCaps.Counted(owner.Skills)==12000 && owner.Skills.Total>12000);
        });
        check("counted gain cannot consume free skills to bypass cap",()=>{
            owner.Skills.Snooping.Base=50;owner.Skills.Snooping.SetLockNoRelay(SkillLock.Down);blocked.SetLockNoRelay(SkillLock.Up);
            Server.Misc.SkillCheck.Gain(owner,blocked,10);
            Require(blocked.Base==0 && owner.Skills.Snooping.Base==50 && HavenPlayerCaps.Counted(owner.Skills)==12000);
        });
        check("transcendence increases free skill at counted cap",()=>{
            owner.Skills.Focus.SetLockNoRelay(SkillLock.Up);var scroll=new ScrollOfTranscendence(SkillName.Focus,1.0);owner.Backpack.DropItem(scroll);scroll.Use(owner);
            Require(scroll.Deleted && owner.Skills.Focus.Base==1 && HavenPlayerCaps.Counted(owner.Skills)==12000);
        });
        check("NPC teaching can raise free skill at counted cap",()=>{
            var teacher=new AnimalTrainer();teacher.Skills.AnimalLore.Base=90;owner.Skills.AnimalLore.SetLockNoRelay(SkillLock.Up);
            int points=10;var result=teacher.CheckTeachSkills(SkillName.AnimalLore,owner,10,ref points,true);
            Require(result==BaseCreature.TeachResult.Success && owner.Skills.AnimalLore.Base>0 && HavenPlayerCaps.Counted(owner.Skills)==12000);teacher.Delete();
        });
    }
}





