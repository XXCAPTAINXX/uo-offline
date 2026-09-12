using System;
using System.Collections.Generic;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Multis;
using Server.Regions;

namespace Server.HavenPrototype
{
    public class HavenMiniChamp : Item
    {
        public static readonly string[] Themes={"Wildwood uprising","Corsair raiders","Restless dead","Challenge: all variants"};
        private readonly List<Mobile> _foes=new List<Mobile>();
        private readonly HashSet<Mobile> _participants=new HashSet<Mobile>();
        private int _stage=-1, _theme;
        private DateTime _deadline, _cooldown, _emptySince;
        private Timer _timer;
        public bool Challenge { get { return _theme==3; } }
        public bool Active { get { return _stage>=0; } }
        public int Stage { get { return _stage; } }
        public int Remaining { get { return _foes.Count; } }
        public DateTime Cooldown { get { return _cooldown; } }
        public static void Initialize() {
            CommandSystem.Register("minichamp",AccessLevel.Player,e=>{if(HavenPreview.Enabled){var c=Find();if(c!=null)c.Show(e.Mobile);else e.Mobile.SendMessage("The encounter camp is not available yet.");}});
            EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(3),Ensure);};
        }
        public static HavenMiniChamp Find(){return World.Items.Values.OfType<HavenMiniChamp>().FirstOrDefault(x=>!x.Deleted);}
        private static bool HasStructure(Map map,int x,int y) {
            foreach(var tile in map.Tiles.GetStaticTiles(x,y,true))
                if((TileData.ItemTable[tile.ID & 0x3FFF].Flags & (TileFlag.Wall|TileFlag.Roof|TileFlag.Door))!=0)return true;
            return false;
        }
        public static bool SafeSite(Map map,Point3D p) {
            if(map==null || map==Map.Internal || HasStructure(map,p.X,p.Y) || !map.CanFit(p,16,false,false) || BaseHouse.FindHouseAt(p,map,20)!=null)return false;
            return !Region.Find(p,map).IsPartOf<GuardedRegion>();
        }
        public static bool SafeClearing(Map map,Point3D center) {
            if(!SafeSite(map,center))return false;
            int open=0;
            // Check every tile across the leash plus a four-tile building setback.
            for(int dx=-24;dx<=24;dx++)for(int dy=-24;dy<=24;dy++) {
                int x=center.X+dx,y=center.Y+dy;
                var p=new Point3D(x,y,map.GetAverageZ(x,y));
                if(HasStructure(map,x,y) || BaseHouse.FindHouseAt(p,map,20)!=null || Region.Find(p,map).IsPartOf<GuardedRegion>())return false;
                bool safe=Math.Abs(p.Z-center.Z)<=6 && map.CanFit(p,16,false,false);
                if(Math.Abs(dx)<=2 && Math.Abs(dy)<=2 && !safe)return false;
                if(safe)open++;
            }
            return open>=1921; // At least 80 percent walkable, allowing scattered natural trees.
        }
        public static void Ensure() {
            var existing=Find();
            if(existing!=null)HavenExpeditionMarker.Ensure(existing);
            if(existing!=null && existing.Active){Timer.DelayCall(TimeSpan.FromSeconds(30),Ensure);return;}
            if(existing!=null && SafeClearing(existing.Map,existing.Location)){HavenExpeditionMarker.Ensure(existing);return;}
            for(int x=3300;x<=3800;x+=10)for(int y=2400;y<=3080;y+=10) {
                var p=new Point3D(x,y,Map.Trammel.GetAverageZ(x,y));
                if(!SafeClearing(Map.Trammel,p))continue;
                var camp=existing ?? new HavenMiniChamp();camp.MoveToWorld(p,Map.Trammel);HavenExpeditionMarker.Ensure(camp);
                Console.WriteLine("Haven mini champion outdoor camp ready: "+p);return;
            }
            Console.WriteLine("Haven mini champion: no building-free wilderness clearing found.");
        }
        public HavenMiniChamp():base(0x1F14){Name="Corsair expedition camp";Movable=false;Visible=false;StartTimer();}
        public HavenMiniChamp(Serial serial):base(serial){}
        private void StartTimer(){if(_timer!=null)_timer.Stop();_timer=Timer.DelayCall(TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(2),Tick);}
        public void Show(Mobile from){if(!HavenMarks.CanUse(from))return;from.CloseGump(typeof(HavenMiniChampGump));from.SendGump(new HavenMiniChampGump(this,from));}
        public bool Travel(Mobile from) {
            if(!HavenPreview.CanTravel(from))return false;
            Point3D landing;
            var d=new HavenPreview.Destination("Expedition camp",Map,X,Y,Z);
            if(!HavenPreview.FindLanding(d,out landing))return false;
            BaseCreature.TeleportPets(from,landing,Map);from.MoveToWorld(landing,Map);return true;
        }
        public string StartError(Mobile from,int theme){
            if(!HavenPreview.Enabled||Deleted)return "This camp is unavailable.";
            if(from==null||!HavenMarks.CanUse(from))return "You must be alive and signed in to start an expedition.";
            if(theme<0||theme>=Themes.Length)return "Choose one of the listed expeditions.";
            if(Active)return "An expedition is already active: wave "+(_stage+1)+", "+Remaining+" enemies remaining.";
            if(DateTime.UtcNow<_cooldown)return "Camp cooldown: "+Math.Ceiling((_cooldown-DateTime.UtcNow).TotalSeconds)+" seconds remaining.";
            if(from.Map!=Map||(!from.InRange(this,8)&&!HavenExpeditionMarker.Nearby(this,from)))return "Move beside the camp sign or supplies, or use Travel to camp. You are "+(from.Map==Map?Math.Max(Math.Abs(from.X-X),Math.Abs(from.Y-Y)).ToString()+" tiles from the clearing.":"on another facet.");
            var target=from.Combatant as Mobile;
            if(target!=null&&!target.Deleted&&target.Alive&&target.Map==from.Map&&from.InRange(target,18))return "Still engaged with "+target.Name+" nearby. Finish combat before starting.";
            var recent=from.Aggressors.Concat(from.Aggressed).Where(a=>!a.Expired).ToArray();
            if(recent.Length>0)return "Recent combat: wait up to "+Math.Max(1,Math.Ceiling(recent.Max(a=>(a.LastCombatTime+AggressorInfo.ExpireDelay-DateTime.UtcNow).TotalSeconds)))+" seconds after the last attack.";
            return null;
        }
        public bool Begin(Mobile from,int theme) {
            var error=StartError(from,theme);if(error!=null){from?.SendMessage(error);return false;}
            if(!SafeClearing(Map,Location)){from.SendMessage("This camp needs relocating to a clear outdoor area. Please reopen the menu after relocation.");Ensure();return false;}
            _theme=theme;_stage=0;_deadline=DateTime.UtcNow.AddMinutes(theme==3?30:20);_emptySince=DateTime.MinValue;_participants.Clear();
            SpawnWave();return Active;
        }
        private bool SpawnPoint(out Point3D p) {
            for(int tries=0;tries<80;tries++) {
                int x=X+Utility.RandomMinMax(-12,12),y=Y+Utility.RandomMinMax(-12,12);
                p=new Point3D(x,y,Map.GetAverageZ(x,y));
                if(SafeSite(Map,p) && Map.CanFit(p,16,false,true))return true;
            }
            p=Point3D.Zero;return false;
        }
        private void SpawnWave() {
            if(!Active)return;
            for(int i=0;i<(_stage==3?(Challenge?3:1):(Challenge?15:5));i++) {
                Point3D p;if(!SpawnPoint(out p)){Abort();return;}
                var enemy=new HavenMiniEnemy(this,Challenge?i%3:_theme,_stage);_foes.Add(enemy);enemy.MoveToWorld(p,Map);
            }
            foreach(var player in _participants)if(player!=null && !player.Deleted)player.SendMessage(_stage==3?"The expedition boss has arrived!":"Expedition wave "+(_stage+1)+" has begun.");
        }
        internal void Credit(HavenMiniEnemy enemy,Mobile attacker,int damage) {
            if(!Active || !_foes.Contains(enemy) || attacker==null || damage<=0)return;
            var owner=attacker.GetDamageMaster(enemy) ?? attacker;
            if(owner is PlayerMobile && !owner.Deleted && owner.Account is Account)_participants.Add(owner);
        }
        internal void Defeated(HavenMiniEnemy enemy) {
            if(!Active || !_foes.Remove(enemy))return;
            if(_foes.Count!=0)return;
            if(_stage==3){Win();return;}
            _stage++;
            Timer.DelayCall(TimeSpan.FromSeconds(3),()=>{if(Active && _foes.Count==0)SpawnWave();});
        }
        private void Win() {
            if(!Active)return;
            _stage=-1;_cooldown=DateTime.UtcNow.AddMinutes(2);
            foreach(var player in _participants.ToArray()) {
                if(player==null || player.Deleted || !(player.Account is Account))continue;
                for(int theme=Challenge?0:_theme;theme<=(Challenge?2:_theme);theme++)new HavenMiniPrize(player,theme).Deliver(player);
                if(Challenge){var bonus=new HavenMiniPrize(player,Utility.Random(3));bonus.DropItem(new AstralShard(1));bonus.Deliver(player);}
                HavenMarks.Award(player,Challenge?80:20);Increment(player,"Wins");
                player.SendMessage(Challenge?"Challenge won: four reward sets, 80 Marks, one bonus Astral Shard and ship supplies. Full-pack rewards remain pending.":"Expedition won: 20 Marks and themed rewards. Corsair raiders also award ship supplies. Full-pack rewards remain pending.");
            }
            _participants.Clear();
        }
        public static int Wins(Mobile p) {int n;var a=p.Account as Account;return a!=null && int.TryParse(a.GetTag("Haven.Mini.Wins:"+p.Serial.Value),out n)?n:0;}
        private static void Increment(Mobile p,string key) {var a=(Account)p.Account;int n;string tag="Haven.Mini."+key+":"+p.Serial.Value;int.TryParse(a.GetTag(tag),out n);a.SetTag(tag,(n+1).ToString());}
        private void Abort() {
            _stage=-1;_cooldown=DateTime.UtcNow.AddMinutes(2);
            foreach(var m in _foes.ToArray())if(m!=null && !m.Deleted)m.Delete();_foes.Clear();
            foreach(var p in _participants)if(p!=null && !p.Deleted)p.SendMessage("The expedition dispersed. No completion rewards were awarded.");
            _participants.Clear();
        }
        private void Tick() {
            if(Deleted){if(_timer!=null)_timer.Stop();return;}
            if(!Active)return;
            if(DateTime.UtcNow>=_deadline || _foes.Any(m=>m==null || m.Deleted)){Abort();return;}
            bool present=false;var nearby=Map.GetMobilesInRange(Location,32);
            try{foreach(Mobile m in nearby)if(m is PlayerMobile && m.Alive && m.NetState!=null){present=true;break;}}finally{nearby.Free();}
            if(present)_emptySince=DateTime.MinValue;
            else if(_emptySince==DateTime.MinValue)_emptySince=DateTime.UtcNow;
            else if(DateTime.UtcNow-_emptySince>TimeSpan.FromMinutes(2))Abort();
        }
        public override void OnDelete(){HavenExpeditionMarker.Remove(this);Abort();if(_timer!=null)_timer.Stop();base.OnDelete();}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_stage);w.Write(_theme);w.Write(_deadline);w.Write(_cooldown);w.Write(_foes.Count);foreach(var m in _foes)w.Write(m);w.Write(_participants.Count);foreach(var m in _participants)w.Write(m);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_stage=r.ReadInt();_theme=r.ReadInt();_deadline=r.ReadDateTime();_cooldown=r.ReadDateTime();int count=r.ReadInt();for(int i=0;i<count;i++)_foes.Add(r.ReadMobile());count=r.ReadInt();for(int i=0;i<count;i++){var p=r.ReadMobile();if(p!=null)_participants.Add(p);}StartTimer();if(Active && _foes.Count==0)Timer.DelayCall(TimeSpan.FromSeconds(3),SpawnWave);}
    }
    public class HavenMiniEnemy : BaseCreature
    {
        public override bool CanFlee { get { return false; } }
        private HavenMiniChamp _camp;private bool _boss;
        public HavenMiniEnemy(HavenMiniChamp camp,int theme,int stage):base(stage==3 && theme==2?AIType.AI_Mage:AIType.AI_Melee,FightMode.Closest,12,1,0.2,0.4) {
            _camp=camp;_boss=stage==3;
            Body=theme==0?47:theme==1?17:(_boss?24:50);
            Name=theme==0?(_boss?"the Blighted Heartwood":"a wildwood ravager"):theme==1?(_boss?"Captain Ironhook":"a corsair raider"):(_boss?"the Grave Regent":"a restless warrior");
            Hue=_boss?0x489:0;BaseSoundID=theme==0?442:theme==1?0x45A:0x48D;
            SetStr(_boss?300:100+stage*35);SetDex(_boss?100:65);SetInt(_boss?250:40);
            SetHits(_boss?1200:120+stage*65);SetDamage(_boss?14:6+stage*2,_boss?22:10+stage*2);
            SetResistance(ResistanceType.Physical,_boss?45:20);SetResistance(ResistanceType.Fire,20);SetResistance(ResistanceType.Cold,20);SetResistance(ResistanceType.Poison,20);SetResistance(ResistanceType.Energy,20);
            SetSkill(SkillName.Wrestling,_boss?100:65+stage*10);SetSkill(SkillName.Tactics,_boss?100:65+stage*10);SetSkill(SkillName.MagicResist,70);
            if(_boss && theme==2){SetSkill(SkillName.Magery,90);SetSkill(SkillName.EvalInt,90);SetSkill(SkillName.Meditation,90);}
            Karma=-5000;Fame=_boss?5000:500;Tamable=false;Home=camp.Location;RangeHome=16;
        }
        public HavenMiniEnemy(Serial serial):base(serial){}
        public override void OnDamage(int amount,Mobile from,bool willKill){if(_camp!=null)_camp.Credit(this,from,amount);base.OnDamage(amount,from,willKill);}
        public override void OnThink(){base.OnThink();if(_camp==null || _camp.Deleted){Delete();return;}if(Map!=_camp.Map || !InRange(_camp,20)){Combatant=null;MoveToWorld(_camp.Location,_camp.Map);}}
        public override void OnDeath(Container corpse){base.OnDeath(corpse);if(!_boss && corpse is Corpse)((Corpse)corpse).BeginDecay(TimeSpan.FromSeconds(45));if(_camp!=null)_camp.Defeated(this);}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_camp);w.Write(_boss);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_camp=r.ReadItem() as HavenMiniChamp;_boss=r.ReadBool();}
    }
    public class HavenMiniPrize : Container
    {
        private Mobile _owner;
        public HavenMiniPrize(Mobile owner,int theme):base(0xE76){_owner=owner;Movable=false;Visible=false;DropItem(new BankCheck(10000));if(theme==1){DropItem(new Cannonball(25));DropItem(new PowderCharge(25));DropItem(new FuseCord(25));}DropItem(new HavenResourceDeed(theme==0?12:theme==1?0:34,250));for(int i=0;i<5;i++)DropItem(PowerScroll.CreateRandomNoCraft(5,10));DropItem(new ScrollOfAlacrity(theme==0?SkillName.Lumberjacking:theme==1?SkillName.Tactics:SkillName.MagicResist));DropItem(new ScrollOfTranscendence(theme==0?SkillName.Lumberjacking:theme==1?SkillName.Tactics:SkillName.MagicResist,Utility.RandomMinMax(5,20)/10.0));}
        public HavenMiniPrize(Serial serial):base(serial){}
        public bool Deliver(Mobile from){if(Deleted || from!=_owner || !HavenMarks.CanUse(from) || from.Backpack==null)return false;int count=0,weight=0;foreach(var item in Items){if(!from.Backpack.CheckHold(from,item,false,true,count,weight))return false;count+=1+item.TotalItems;weight+=item.PileWeight+item.TotalWeight;}foreach(var item in Items.ToArray())from.Backpack.DropItem(item);Delete();return true;}
        public static void Collect(Mobile p){foreach(var parcel in World.Items.Values.OfType<HavenMiniPrize>().Where(x=>x._owner==p).ToArray())parcel.Deliver(p);}
        public static int Pending(Mobile p){return World.Items.Values.OfType<HavenMiniPrize>().Count(x=>!x.Deleted && x._owner==p);}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_owner);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_owner=r.ReadMobile();}
    }
    public class HavenMiniChampGump : HavenMenuGump
    {
        private readonly HavenMiniChamp _camp;
        public HavenMiniChampGump(HavenMiniChamp camp,Mobile p):base(45,45){
            _camp=camp;AddBackground(0,0,570,455,0x13BE);AddLabel(20,16,1152,"Corsair expeditions - mini champion");
            AddHtml(20,50,530,70,"<BASEFONT COLOR=#FFFFFF>Three waves of five enemies, then a boss. Everyone who damages an enemy earns the completion reward; companion and pet damage counts for their owner. Run away to abandon the fight.</BASEFONT>",false,false);
            AddLabel(20,127,1152,camp.Active?"Active: wave "+(camp.Stage+1)+" / 4, "+camp.Remaining+" enemies":"Ready in "+Math.Max(0,Math.Ceiling((camp.Cooldown-DateTime.UtcNow).TotalSeconds))+" seconds");
            for(int i=0;i<4;i++)FlatButton(20,160+i*30,265,10+i,HavenMiniChamp.Themes[i]);
            AddHtml(300,160,250,105,"<BASEFONT COLOR=#FFFFFF>Each participant: 10,000 gold, 20 Marks, 250 themed resources as a deed. Five 105/110 Power Scrolls, Alacrity and Transcendence. Full packs keep rewards pending.</BASEFONT>",false,false);
            AddHtml(20,290,530,52,"<BASEFONT COLOR=#3B2A1A>Challenge: three waves of 15 mixed enemies, then all three bosses together. Four reward sets, 80 Marks and +1 Astral Shard per player. Includes Corsair ship ammunition.</BASEFONT>",false,false);
            AddLabel(20,347,1152,"Wins: "+HavenMiniChamp.Wins(p)+" | Pending rewards: "+HavenMiniPrize.Pending(p));
            Button(20,385,1,"Travel to camp");Button(300,385,2,"Collect pending rewards");Button(20,423,3,"Refresh");Button(450,423,0,"Close");
        }
        private void Button(int x,int y,int id,string text){FlatButton(x,y,id==0?100:id==2?245:230,id,text);}
        public override void OnResponse(NetState sender,RelayInfo info){var p=sender.Mobile;if(info.ButtonID==0 || _camp.Deleted || !HavenMarks.CanUse(p))return;if(info.ButtonID==1 && !_camp.Travel(p))p.SendMessage("Leave combat and wait for recent combat to expire before travelling.");else if(info.ButtonID==2)HavenMiniPrize.Collect(p);else if(info.ButtonID>=10 && info.ButtonID<=13)_camp.Begin(p,info.ButtonID-10);_camp.Show(p);}
    }
}






