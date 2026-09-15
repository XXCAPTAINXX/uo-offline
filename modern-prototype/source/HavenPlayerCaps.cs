using System;
using System.Linq;
using System.Collections.Generic;
using Server.Commands;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenPlayerCaps
    {
        // Union of InsaneUO and UOAlive published free skills, plus Haven's original four.
        // Sources and full player-facing list: FREE-SKILLS.md.
        private static readonly HashSet<SkillName> FreeSkills = new HashSet<SkillName> {
            SkillName.Alchemy, SkillName.AnimalLore, SkillName.AnimalTaming, SkillName.ArmsLore,
            SkillName.Begging, SkillName.Camping, SkillName.Cartography, SkillName.Cooking,
            SkillName.DetectHidden, SkillName.Fishing, SkillName.Fletching, SkillName.Focus,
            SkillName.Forensics, SkillName.Herding, SkillName.Hiding, SkillName.Inscribe,
            SkillName.ItemID, SkillName.Lockpicking, SkillName.Lumberjacking, SkillName.Mining,
            SkillName.Musicianship, SkillName.RemoveTrap, SkillName.Snooping, SkillName.TasteID,
            SkillName.Tracking
        };
        public static bool IsFree(Mobile owner,Skill skill) {
            return HavenPreview.Enabled && owner is PlayerMobile && skill!=null &&
                FreeSkills.Contains(skill.SkillName);
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
                                var names=FreeSkills.Select(x=>e.Mobile.Skills[x].Name).OrderBy(x=>x).ToArray();
                e.Mobile.SendMessage(names.Length+" free skills; train normally. Individual skill caps still apply.");
                for(int i=0;i<names.Length;i+=5)e.Mobile.SendMessage(string.Join(", ",names.Skip(i).Take(5)));
            });
        }
    }
}

