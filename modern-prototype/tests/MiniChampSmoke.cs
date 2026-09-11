using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;
public static class MiniChampSmoke {
    private static void Require(bool ok){if(!ok)throw new Exception("Mini champion regression");}
    private static void Set(HavenMiniChamp c,string name,object value){typeof(HavenMiniChamp).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,value);}
    private static void Call(HavenMiniChamp c,string name){typeof(HavenMiniChamp).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(c,null);}
    private static Mobile[] Foes(HavenMiniChamp c){return ((List<Mobile>)typeof(HavenMiniChamp).GetField("_foes",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(c)).ToArray();}
    private static PlayerMobile Player(string name,HavenMiniChamp camp){var p=new PlayerMobile {Player=true,Name=name,Body=0x190,RawStr=100};p.AddItem(new Backpack());new Account(name.Replace(" ","-"),Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(camp.Location,camp.Map);return p;}
    public static void Run(Action<string,Action> check,bool reload){
        if(reload){
            check("mini champion active wave and full-pack prize survive reload",()=>{
                var c=HavenMiniChamp.Find();var p=World.Mobiles.Values.OfType<PlayerMobile>().Single(x=>x.Name=="Mini owner");
                Require(c!=null && c.Active && c.Stage==0 && c.Remaining==5 && Foes(c).All(x=>x!=null && !x.Deleted));
                Require(HavenMiniPrize.Pending(p)==1 && HavenMarks.Balance(p)==20 && HavenMiniChamp.Wins(p)==1);
                p.MoveToWorld(c.Location,c.Map);p.Backpack.MaxItems=125;HavenMiniPrize.Collect(p);
                Require(HavenMiniPrize.Pending(p)==0 && p.Backpack.FindItemsByType(typeof(BankCheck)).Length==1);
                HavenMiniPrize.Collect(p);Require(p.Backpack.FindItemsByType(typeof(BankCheck)).Length==1);Call(c,"Abort");
            });return;
        }
        HavenMiniChamp.Ensure();var camp=HavenMiniChamp.Find();
        check("mini champion camp is open unguarded land and setup is idempotent",()=>{Require(camp!=null && HavenMiniChamp.SafeSite(camp.Map,camp.Location));HavenMiniChamp.Ensure();Require(World.Items.Values.OfType<HavenMiniChamp>().Count()==1);});
        if(camp==null)return;
        var owner=Player("Mini owner",camp);var friend=Player("Mini friend",camp);var spectator=Player("Mini spectator",camp);var companion=HavenCompanion.Claim(owner);
        owner.Backpack.DropItem(new Bag());owner.Backpack.MaxItems=1;
        check("mini champion rejects invalid starts and duplicate encounter",()=>{
            Require(!camp.Begin(owner,-1));owner.Internalize();Require(!camp.Begin(owner,0));owner.MoveToWorld(camp.Location,camp.Map);
            Require(camp.Begin(owner,0) && camp.Remaining==5 && !camp.Begin(owner,1));
        });
        HavenMiniEnemy boss=null;
        check("three waves and boss credit companion owner and all damage participants",()=>{
            for(int stage=0;stage<4;stage++){
                Require(camp.Stage==stage && camp.Remaining==(stage==3?1:5));
                foreach(var m in Foes(camp)){var e=(HavenMiniEnemy)m;e.Damage(1,companion);e.Damage(1,friend);if(stage==3)boss=e;e.Kill();}
                if(stage<3)Call(camp,"SpawnWave");
            }
            Require(!camp.Active && HavenMarks.Balance(owner)==20 && HavenMarks.Balance(friend)==20 && HavenMarks.Balance(spectator)==0);
            Require(HavenMiniChamp.Wins(owner)==1 && HavenMiniChamp.Wins(friend)==1 && HavenMiniPrize.Pending(owner)==1 && HavenMiniPrize.Pending(friend)==0);
            Require(friend.Backpack.FindItemByType(typeof(BankCheck))!=null && friend.Backpack.FindItemByType(typeof(HavenResourceDeed))!=null);
        });
        check("mini champion rewards cannot replay or be collected by stranger",()=>{
            camp.Defeated(boss);Require(HavenMarks.Balance(owner)==20 && HavenMiniChamp.Wins(owner)==1);
            var prize=World.Items.Values.OfType<HavenMiniPrize>().Single();Require(!prize.Deliver(spectator) && HavenMiniPrize.Pending(owner)==1);
        });
        check("administratively deleted enemies abort without completion rewards",()=>{
            owner.Combatant=null;owner.Aggressors.Clear();owner.Aggressed.Clear();Set(camp,"_cooldown",DateTime.MinValue);
            Require(camp.Begin(owner,1));Foes(camp)[0].Delete();Call(camp,"Tick");Require(!camp.Active && HavenMarks.Balance(owner)==20);
        });
        check("save an active undead wave without replaying its spawns",()=>{Set(camp,"_cooldown",DateTime.MinValue);Require(camp.Begin(owner,2) && camp.Remaining==5);});
        owner.Internalize();friend.Internalize();spectator.Internalize();companion.Internalize();
    }
}
