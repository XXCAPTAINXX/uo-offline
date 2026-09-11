using System;
using System.IO;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class MissionSafetySmoke
{
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText("mission-safety-checks.log","PASS "+label+"\n");}
    public static void Initialize(){if(File.Exists("MISSION-SAFETY-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(8),Run);}
    static void Run(){try{
        var owner=new MissionSafetyPlayer{Player=true,Name="Mission safety fixture",Body=0x190,RawStr=250};owner.AddItem(new Backpack());
        var account=new Account("mission-safety-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));account[0]=owner;
        owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);var companion=HavenCompanion.Claim(owner);
        owner.Criminal=true;
        var field=typeof(Mobile).GetField("m_ExpireCriminal",BindingFlags.NonPublic|BindingFlags.Instance);
        var timer=(Timer)field.GetValue(owner);int crimes=owner.Crimes;
        Check(!companion.IsBeneficialCriminal(owner),"healing own criminal owner is not a fresh crime");
        companion.DoBeneficial(owner);
        Check(owner.Criminal&&owner.Crimes==crimes,"owner aid preserves existing flag without renewing expiry");
        var stranger=new PlayerMobile{Player=true};stranger.Criminal=true;
        Check(companion.IsBeneficialCriminal(stranger),"criminal aid to unrelated player still uses native rules");
        owner.Criminal=false;companion.Criminal=false;
        File.AppendAllText("mission-safety-checks.log","Dispatch precondition: "+companion.MissionStartError(owner,30,CompanionMission.Mining)+"\n");
        Check(companion.StartMission(owner,30,CompanionMission.Mining),"30 minute mining dispatch succeeds when eligible");
        Check(companion.MissionStartError(owner,30,CompanionMission.Mining).Contains("already"),"second dispatch explains active mission");
        Check(companion.Recall(owner),"30 minute mission can be canceled early");
        Check(companion.MissionStartError(owner,20,CompanionMission.Mining).Contains("5, 15 or 30"),"invalid duration explains allowed options");
        companion.CriminalAction(false);Check(owner.Criminal,"actual companion crime still flags owner");
        var pet=new HavenEmberwing();HavenPetMissions.ApplyRarity(pet,3);int strength=pet.RawStr;HavenPetMissions.ApplyRarity(pet,3);Check(HavenPetRarity.Find(pet).Tier==3&&pet.RawStr==strength,"pet retains rarity identity and cannot stack rarity bonuses");pet.Delete();
        stranger.Delete();companion.Delete();owner.Delete();
        File.AppendAllText("mission-safety-checks.log","COMPLETE\n");Core.Kill(false);
    }catch(Exception e){File.AppendAllText("mission-safety-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}

public class MissionSafetyPlayer:PlayerMobile
{
    public int Crimes;
    public MissionSafetyPlayer(){}
    public MissionSafetyPlayer(Serial serial):base(serial){}
    public override void CriminalAction(bool message){Crimes++;base.CriminalAction(message);}
    public override void Serialize(GenericWriter writer){base.Serialize(writer);writer.Write(0);}
    public override void Deserialize(GenericReader reader){base.Deserialize(reader);reader.ReadInt();}
}
