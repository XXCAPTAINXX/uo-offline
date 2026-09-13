using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Gumps;
using Server.Network;
using Server.SkillHandlers;

namespace Server.HavenPrototype
{
    public static class HavenPetMissions
    {
        // Matches the original server's legendary skill roll eligibility exactly.
        public static readonly SkillName[] TrainableSkills = { SkillName.Wrestling, SkillName.Tactics, SkillName.Anatomy, SkillName.Healing,
            SkillName.MagicResist, SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.Focus,
            SkillName.Poisoning, SkillName.Parry, SkillName.Hiding, SkillName.DetectHidden,
            SkillName.Necromancy, SkillName.SpiritSpeak, SkillName.Spellweaving, SkillName.Mysticism,
            SkillName.Chivalry, SkillName.Bushido, SkillName.Ninjitsu, SkillName.Musicianship, SkillName.Discordance,
            SkillName.Peacemaking, SkillName.Provocation };
        public static readonly string[] Names={"Pack horse","Horse","Forest ostard","Giant beetle","Dragon","White wyrm","Emberwing ostard","Moonfang wolf","Stormscale drake","Frostmane steed","Verdant llama","Stormhorn kirin"};
        public static readonly double[] Requirements={11.1,29.1,29.1,29.1,93.9,96.3,65,95,110,70,80,105};
        public static bool Valid(CompanionMission kind){return (int)kind>=6&&(int)kind<18;}
        public static bool CanStart(HavenCompanion c,CompanionMission kind){int i=(int)kind-6;return Valid(kind)&&c.Skills.AnimalTaming.Base>=Requirements[i]&&c.Skills.AnimalLore.Base>=Requirements[i];}
        public static BaseCreature Create(int i){switch(i){case 0:return new PackHorse();case 1:return new Horse();case 2:return new ForestOstard();case 3:return new Beetle();case 4:return new Dragon();case 5:return new WhiteWyrm();case 6:return new HavenEmberwing();case 7:return new HavenMoonfang();case 8:return new HavenStormscale();case 9:return new HavenFrostmane();case 10:return new HavenVerdantLlama();case 11:return new HavenStormhorn();default:return null;}}
        public static void ApplyRarity(BaseCreature pet,int tier)
        {
            if(HavenPetRarity.Find(pet)!=null)return;
            HavenPetRarity.Attach(pet,tier);
            HavenPetAppearance.ApplyNaturalHue(pet);
            if(tier<=0)return;
            pet.Name=(tier==1?"Rare ":tier==2?"Epic ":"Legendary ")+pet.Name;
            pet.RawStr+=pet.RawStr*tier/10;pet.RawDex+=pet.RawDex*tier/10;pet.RawInt+=pet.RawInt*tier/10;
            pet.HitsMaxSeed=pet.HitsMax+pet.HitsMax*tier/10;pet.Hits=pet.HitsMax;
            foreach(var skill in new[]{SkillName.Wrestling,SkillName.Tactics,SkillName.MagicResist,SkillName.Magery,SkillName.EvalInt})if(pet.Skills[skill].Base>0){pet.Skills[skill].Cap=Math.Max(pet.Skills[skill].Cap,tier==3?120:100+tier*5);pet.Skills[skill].Base=Math.Min(pet.Skills[skill].Cap,pet.Skills[skill].Base+tier*5);}
            if(tier==3){pet.ControlSlots=1;HavenLegendaryPetSkills.Roll(pet); }
        }
    }
    public class HavenPetTicket:Item
    {
        public bool Favorite;public DateTime AcquiredAt=DateTime.UtcNow;
        public Mobile Owner;public BaseCreature Pet;public int Kind,Rarity;private Bag _supplies;
        public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)foreach(var ticket in World.Items.Values.OfType<HavenPetTicket>().ToArray())ticket.ApplySpeciesColor();};}
        private void ApplySpeciesColor()
        {
            if(Pet==null || Pet.Deleted)return;
            string[] types={"PackHorse","Horse","ForestOstard","Beetle","Dragon","WhiteWyrm","HavenEmberwing","HavenMoonfang","HavenStormscale","HavenFrostmane","HavenVerdantLlama","HavenStormhorn"};
            int[] hues={0x59B,0x972,0x59D,0x53D,0x21,0x47E,0x489,0x455,0x515,0x480,0x48F,0x48D};
            int species=Array.IndexOf(types,Pet.GetType().Name);
            Hue=species>=0?hues[species]:0x59B;
        }
        public void ShowLore(Mobile from)
        {
            if(from==null||!from.Alive||Deleted||Owner!=from||from.Backpack==null||!IsChildOf(from.Backpack)||!HavenResources.Accessible(from,this)||Pet==null||Pet.Deleted)return;
            HavenAnimalLoreGump.DisplayTo(from,Pet);
        }
        public Bag TakeSupplies(){var bag=_supplies;_supplies=null;return bag;}
        public HavenPetTicket(Mobile owner,int kind,int minutes=5,int minimumRarity=0):base(0x14F0)
        {
            Owner=owner;Kind=kind;Weight=1;Hue=0x59B;LootType=LootType.Blessed;
            _supplies=new Bag{Name="taming mission bonus supplies"};_supplies.Internalize();
            for(int i=0;i<HavenTamingSupplies.SearchRolls(minutes);i++){double roll=Utility.RandomDouble();int tier=kind<6?0:roll<0.4?0:roll<0.75?1:roll<0.95?2:3;Rarity=Math.Max(Rarity,tier);var bonus=HavenTamingSupplies.Bonus(Utility.RandomDouble());if(bonus!=null)_supplies.DropItem(bonus);}
            Rarity=Math.Max(Rarity,Math.Max(0,Math.Min(3,minimumRarity)));Pet=HavenPetMissions.Create(kind);AnimalTaming.ScaleSkills(Pet,0.90,true);if(Pet.StatLossAfterTame)AnimalTaming.ScaleStats(Pet,0.5);
            HavenPetMissions.ApplyRarity(Pet,Rarity);Pet.Internalize();Name="Pet claim: "+Pet.Name;ApplySpeciesColor();Internalize();
        }
        private HavenPetTicket(BaseCreature pet,Mobile owner):base(0x14F0){Pet=pet;Owner=owner;Kind=-1;Rarity=HavenPetDefenses.Tier(pet);Weight=1;Hue=0x59B;LootType=LootType.Blessed;Name="Pet claim: "+pet.Name;ApplySpeciesColor();}
        public static HavenPetTicket Store(BaseCreature pet,Mobile owner,Container pack){if(pet is HavenCompanion){if(owner!=null)owner.SendMessage("Use companion Recall instead of storing your companion as a pet ticket.");return null;}if(pet==null||pet.Deleted||owner==null||owner.Deleted||pack==null||pack.Deleted||pet.IsDeadPet||pet.Summoned||!(pet.ControlMaster==owner||(pet.ControlMaster is HavenCompanion&&((HavenCompanion)pet.ControlMaster).BoundOwner==owner)))return null;if(pet.IsBonded){var book=HavenPetBook.Ensure(owner);if(book==null)return null;pack=book;}var ticket=new HavenPetTicket(pet,owner);if(!pack.TryDropItem(owner,ticket,false)){ticket.Pet=null;ticket.Delete();return null;}var mount=pet as IMount;if(mount!=null)mount.Rider=null;pet.Combatant=null;pet.ControlTarget=null;pet.ControlOrder=OrderType.Stay;pet.Internalize();pet.SetControlMaster(null);pet.SummonMaster=null;return ticket;}
        public HavenPetTicket(Serial serial):base(serial){}
        public bool Claim(Mobile p)
        {
            if(Deleted||p!=Owner||p==null||!p.Alive||p.Backpack==null||!IsChildOf(p.Backpack)||!HavenResources.Accessible(p,this)||Pet==null||Pet.Deleted||p.Map==null||p.Map==Map.Internal)return false;
            if(p.Followers+Pet.ControlSlots>p.FollowersMax||!p.Map.CanFit(p.Location,16,false,false)||!Pet.SetControlMaster(p))return false;
            var pet=Pet;Pet=null;if(!pet.Owners.Contains(p))pet.Owners.Add(p);pet.Loyalty=BaseCreature.MaxLoyalty;pet.BondingBegin=DateTime.UtcNow-pet.BondingDelay-TimeSpan.FromSeconds(1);
            pet.ControlTarget=p;pet.ControlOrder=OrderType.Follow;pet.MoveToWorld(p.Location,p.Map);Delete();p.SendMessage("Your pet joined you. Feed it suitable food to bond when you meet its normal taming requirement.");return true;
        }
        public override void OnDoubleClick(Mobile p){if(!Claim(p))p.SendMessage("Keep your own ticket in your backpack and free enough follower slots. The ticket is preserved.");}
        public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Owner: "+(Owner==null?"none":Owner.Name));list.Add("Rarity: "+HavenPetRarity.Label(Pet==null?Rarity:HavenPetDefenses.Tier(Pet)));if(Pet!=null&&!Pet.Deleted){list.Add("Follower slots: "+Pet.ControlSlots+"; taming requirement: "+Pet.MinTameSkill.ToString("0.0"));list.Add("Hits "+Pet.HitsMax+"; Str "+Pet.RawStr+"; Dex "+Pet.RawDex+"; Int "+Pet.RawInt);list.Add("Double-click to claim this exact pet; no rerolls");}}
        public override void OnDelete(){if(_supplies!=null&&!_supplies.Deleted)_supplies.Delete();if(Pet!=null&&!Pet.Deleted)Pet.Delete();base.OnDelete();}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(2);w.Write(Owner);w.Write(Pet);w.Write(Kind);w.Write(Rarity);w.Write(_supplies);w.Write(Favorite);w.Write(AcquiredAt);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);int version=r.ReadInt();Owner=r.ReadMobile();Pet=r.ReadMobile() as BaseCreature;Kind=r.ReadInt();Rarity=r.ReadInt();if(version>=1)_supplies=r.ReadItem() as Bag;if(version>=2){Favorite=r.ReadBool();AcquiredAt=r.ReadDateTime();}else AcquiredAt=DateTime.MinValue;}
    }
    public partial class HavenCompanion
    {
        HavenPetTicket _scheduledPet;readonly List<HavenPetTicket> _pendingPets=new List<HavenPetTicket>();
        public int PendingPetTickets{get{return _pendingPets.Count(x=>x!=null&&!x.Deleted);}}
        void PreparePetMission(CompanionMission kind,int minutes){if(_scheduledPet!=null&&!_scheduledPet.Deleted)_scheduledPet.Delete();_scheduledPet=HavenPetMissions.Valid(kind)?new HavenPetTicket(BoundOwner,(int)kind-6,minutes):null;}
        void CompletePetMission(){if(_scheduledPet==null||_scheduledPet.Deleted)return;var supplies=_scheduledPet.TakeSupplies();if(supplies!=null){if(supplies.Items.Count>0)Backpack.DropItem(supplies);else supplies.Delete();}_pendingPets.Add(_scheduledPet);_scheduledPet=null;foreach(var skill in new[]{Skills.AnimalTaming,Skills.AnimalLore})skill.BaseFixedPoint=Math.Min(skill.CapFixedPoint,skill.BaseFixedPoint+HavenCompanionProgression.TamingTraining(skill.BaseFixedPoint,_missionMinutes,Utility.RandomDouble()));}
        void CancelPetMission(){if(_scheduledPet!=null&&!_scheduledPet.Deleted)_scheduledPet.Delete();_scheduledPet=null;}
        public void DeliverPetTickets(){var owner=BoundOwner;if(owner==null||owner.Deleted||owner.Backpack==null)return;foreach(var ticket in _pendingPets.ToArray()){if(ticket==null||ticket.Deleted){_pendingPets.Remove(ticket);continue;}var book=HavenPetBook.Ensure(owner);if(book==null||!book.TryDropItem(owner,ticket,false))break;ticket.AcquiredAt=DateTime.UtcNow;_pendingPets.Remove(ticket);owner.SendMessage("Your mission pet is in [petbook.");}}
        void SerializePetMissions(GenericWriter w){w.Write(_scheduledPet);w.Write(_pendingPets.Count);foreach(var ticket in _pendingPets)w.Write(ticket);}
        void DeserializePetMissions(GenericReader r){_scheduledPet=r.ReadItem() as HavenPetTicket;int count=r.ReadInt();if(count<0||count>50)throw new InvalidOperationException("Invalid pet ticket count");for(int i=0;i<count;i++){var ticket=r.ReadItem() as HavenPetTicket;if(ticket!=null)_pendingPets.Add(ticket);}}
    }
    public class HavenPetMissionGump:HavenPetMenuGump
    {
        readonly HavenCompanion _companion;readonly int _minutes;
        public HavenPetMissionGump(HavenCompanion c,int minutes=5):base(55,55){_companion=c;_minutes=minutes;AddBackground(0,0,610,570,0xA28);AddLabel(24,20,0,"Companion taming missions");AddLabel(24,52,0,"Taming "+c.Skills.AnimalTaming.Base.ToString("0.0")+" / Lore "+c.Skills.AnimalLore.Base.ToString("0.0")+"; both must meet the requirement.");Button(24,89,1,"5 min");Button(160,89,2,"15 min");Button(300,89,3,"30 min");AddLabel(436,89,0,"Selected: "+minutes+" min");for(int i=0;i<12;i++){int y=130+i*29;Button(24,y,100+i,HavenPetMissions.Names[i]);AddLabel(370,y,0,"Needs "+HavenPetMissions.Requirements[i].ToString("0.0")+" both");}AddLabel(24,489,0,"One owner-bound pet ticket per completed trip. Early recall awards none.");Button(24,530,4,"Collect pending ("+c.PendingPetTickets+")");Button(436,530,0,"Back");}
        void Button(int x,int y,int id,string label){FlatButton(x,y,id>=100?315:id==4?280:110,id,label);}
        public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(!_companion.IsOwner(p))return;int id=info.ButtonID;if(id==0){_companion.Show(p,true);return;}if(id>=1&&id<=3){p.SendGump(new HavenPetMissionGump(_companion,id==1?5:id==2?15:30));return;}if(id==4){_companion.DeliverPetTickets();p.SendGump(new HavenPetMissionGump(_companion,_minutes));return;}if(id>=100&&id<112){if(!_companion.StartMission(p,_minutes,(CompanionMission)(id-94))){p.SendMessage("Check both taming skills, distance, combat and pending ticket space.");p.SendGump(new HavenPetMissionGump(_companion,_minutes));}else _companion.Show(p);}}
    }
}
