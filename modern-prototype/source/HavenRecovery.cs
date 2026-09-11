using System;
using System.Linq;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
namespace Server.HavenPrototype {
    public static class HavenRecovery {
        public static void Initialize(){CommandSystem.Register("recovery",AccessLevel.Player,e=>{if(HavenPreview.Enabled)e.Mobile.SendGump(new HavenRecoveryGump());});EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(3),Ensure);};}
        public static void Ensure(){if(!HavenPreview.Enabled)return;Place<HavenPlazaHealer>(3506,2573,()=>new HavenPlazaHealer());Place<HavenRecoverySteward>(3508,2576,()=>new HavenRecoverySteward());}
        private static void Place<T>(int x,int y,Func<BaseCreature> create) where T:BaseCreature {
            if(World.Mobiles.Values.OfType<T>().Any(m=>!m.Deleted))return;
            Point3D p;if(!HavenPreview.FindLanding(new HavenPreview.Destination("Recovery",Map.Trammel,x,y,Map.Trammel.GetAverageZ(x,y)),out p))return;
            var npc=create();npc.Home=p;npc.RangeHome=0;npc.MoveToWorld(p,Map.Trammel);
        }
        public static HavenRecoverySteward Steward(){return World.Mobiles.Values.OfType<HavenRecoverySteward>().FirstOrDefault(x=>!x.Deleted);}
        public static bool CanUse(Mobile from){var s=Steward();return HavenPreview.CanTravel(from) && from.Account!=null && s!=null && from.Map==s.Map && from.InRange(s,3) && from.InLOS(s);}
        public static int ResurrectPets(Mobile from){if(!CanUse(from))return 0;int count=0;var near=from.GetMobilesInRange(12);try{foreach(Mobile m in near){var pet=m as BaseCreature;if(pet!=null && (pet.IsDeadPet || pet.Hits<pet.HitsMax || pet.Mana<pet.ManaMax || pet.Stam<pet.StamMax) && pet.ControlMaster==from && from.InLOS(pet) && pet.Map.CanFit(pet.Location,16,false,false)){if(pet.IsDeadPet)pet.ResurrectPet();pet.Hits=pet.HitsMax;pet.Mana=pet.ManaMax;pet.Stam=pet.StamMax;count++;}}}finally{near.Free();}return count;}
        public static bool RecoverCompanion(Mobile from){if(!CanUse(from))return false;var c=World.Mobiles.Values.OfType<HavenCompanion>().FirstOrDefault(x=>!x.Deleted && x.BoundOwner==from && x.ControlMaster==from && x.IsDeadPet && !x.IsStabled && !x.OnMission);if(c==null)return false;c.MoveToWorld(from.Location,from.Map);c.ResurrectPet();c.Hits=c.HitsMax;c.Mana=c.ManaMax;c.Stam=c.StamMax;c.SetOrder(from,OrderType.Follow);c.AIObject.Activate();return true;}
        public static string CompanionStatus(Mobile from){var c=World.Mobiles.Values.OfType<HavenCompanion>().FirstOrDefault(x=>!x.Deleted && x.BoundOwner==from);if(c!=null && !c.IsDeadPet && c.Alive)return "Your companion is already alive ("+c.Hits+"/"+c.HitsMax+" health). Use Heal / resurrect nearby pets to heal him.";return "No eligible dead companion was found.";}
        public static bool RecallCorpse(Mobile from){if(!CanUse(from))return false;var corpse=from.Corpse as Corpse;if(corpse==null || corpse.Deleted || corpse.Owner!=from || corpse.Parent!=null || corpse.Map==null || corpse.Map==Map.Internal || !from.Map.CanFit(from.Location,16,false,false))return false;corpse.MoveToWorld(from.Location,from.Map);corpse.BeginDecay(TimeSpan.FromMinutes(10));return true;}
    }
    public class HavenPlazaHealer:Healer {
        public HavenPlazaHealer(){Name="Ava";Title="the Haven healer";CantWalk=true;}
        public HavenPlazaHealer(Serial s):base(s){}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    }
    public class HavenRecoverySteward:BaseCreature {
        public override bool IsInvulnerable {get{return true;}}
        public HavenRecoverySteward():base(AIType.AI_Vendor,FightMode.None,10,1,0.2,0.4){Name="Mara";Title="the pet healer and recovery steward";Body=0x191;Female=true;CantWalk=true;SetStr(100);SetDex(100);SetInt(100);AddItem(new Robe(0x489));AddItem(new Sandals());}
        public HavenRecoverySteward(Serial s):base(s){}
        public override void OnDoubleClick(Mobile from){if(from.Map==Map && from.InRange(this,3))from.SendGump(new HavenRecoveryGump());else from.SendMessage("Stand beside Mara at Haven's plaza for recovery services.");}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    }
    public class HavenRecoveryGump:Gump {
        public HavenRecoveryGump():base(50,50){AddBackground(0,0,550,340,0x13BE);AddLabel(20,16,1152,"Haven recovery services");AddHtml(20,50,505,70,"<BASEFONT COLOR=#FFFFFF>Ava resurrects players approaching as ghosts. Stand beside Mara, alive and out of combat, for the free preview services below.</BASEFONT>",false,false);Button(20,128,1,"Heal / resurrect nearby pets");Button(20,170,2,"Recover my dead companion");Button(20,212,3,"Recall my corpse");AddHtml(20,251,505,48,"<BASEFONT COLOR=#FFFFFF>Pets must be yours and nearby. Dead companions can be brought back from elsewhere. Corpse recall moves your existing last corpse; decayed bodies cannot be restored.</BASEFONT>",false,false);Button(420,306,0,"Close");}
        private void Button(int x,int y,int id,string text){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);AddLabel(x+34,y,1152,text);}
        public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0)return;if(!HavenRecovery.CanUse(p)){p.SendMessage("Stand within three tiles of Mara, alive and out of combat, with no criminal flag.");return;}if(info.ButtonID==1)p.SendMessage("Healed or resurrected "+HavenRecovery.ResurrectPets(p)+" pet(s), including companions.");else if(info.ButtonID==2)p.SendMessage(HavenRecovery.RecoverCompanion(p)?"Your companion is alive and following you.":HavenRecovery.CompanionStatus(p));else if(info.ButtonID==3)p.SendMessage(HavenRecovery.RecallCorpse(p)?"Your corpse is here; loot it normally. It will remain for ten minutes.":"No surviving accessible corpse was found for you.");p.SendGump(new HavenRecoveryGump());}
    }
}


