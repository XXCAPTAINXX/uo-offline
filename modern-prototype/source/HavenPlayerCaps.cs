using System;
using System.Linq;
using Server.Commands;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenPlayerCaps
    {
        public static bool IsFree(Mobile owner,Skill skill) {
            return HavenPreview.Enabled && owner is PlayerMobile && skill!=null &&
                (skill.SkillName==SkillName.AnimalTaming || skill.SkillName==SkillName.AnimalLore || skill.SkillName==SkillName.Focus || skill.SkillName==SkillName.Snooping);
        }
        public static int Counted(Skills skills) {
            if(!HavenPreview.Enabled || !(skills.Owner is PlayerMobile))return skills.Total;
            int free=0;foreach(var skill in skills)if(IsFree(skills.Owner,skill))free+=skill.BaseFixedPoint;
            return Math.Max(0,skills.Total-free);
        }
        public static void Apply(Mobile owner) {
            if(!HavenPreview.Enabled || !(owner is PlayerMobile) || owner.Deleted)return;
            owner.StatCap=Math.Max(owner.StatCap,300);owner.Skills.Cap=12000;
        }
        public static void Initialize() {
            EventSink.Login+=e=>Apply(e.Mobile);
            EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)foreach(var owner in World.Mobiles.Values.OfType<PlayerMobile>().ToArray())Apply(owner);};
            CommandSystem.Register("skillbudget",AccessLevel.Player,e=>{
                if(!HavenPreview.Enabled)return;
                e.Mobile.SendMessage("Counted skills: "+(Counted(e.Mobile.Skills)/10.0).ToString("F1")+" / "+(e.Mobile.Skills.Cap/10.0).ToString("F1")+". Stat cap: "+e.Mobile.StatCap+".");
                e.Mobile.SendMessage("Free: Animal Taming, Animal Lore, Focus, Snooping. Individual skill caps still apply.");
            });
        }
    }
}
