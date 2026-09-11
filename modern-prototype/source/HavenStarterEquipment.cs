using System;
using Server.Items;
namespace Server.HavenPrototype {
 public class HavenApprenticeBlade:Longsword,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 0;}}
  [Constructable] public HavenApprenticeBlade(){Name="apprentice blade";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenApprenticeBlade(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
  public override void OnHit(Mobile attacker,IDamageable defender,double damageBonus){base.OnHit(attacker,defender,damageBonus);HavenStarterGear.Hit(this,attacker,defender as Mobile);}
 }
 public class HavenApprenticeFencer:Kryss,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 1;}}
  [Constructable] public HavenApprenticeFencer(){Name="apprentice fencer";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenApprenticeFencer(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
  public override void OnHit(Mobile attacker,IDamageable defender,double damageBonus){base.OnHit(attacker,defender,damageBonus);HavenStarterGear.Hit(this,attacker,defender as Mobile);}
 }
 public class HavenApprenticeMace:WarMace,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 2;}}
  [Constructable] public HavenApprenticeMace(){Name="apprentice mace";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenApprenticeMace(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
  public override void OnHit(Mobile attacker,IDamageable defender,double damageBonus){base.OnHit(attacker,defender,damageBonus);HavenStarterGear.Hit(this,attacker,defender as Mobile);}
 }
 public class HavenApprenticeBow:Bow,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 3;}}
  [Constructable] public HavenApprenticeBow(){Name="apprentice bow";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenApprenticeBow(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
  public override void OnHit(Mobile attacker,IDamageable defender,double damageBonus){base.OnHit(attacker,defender,damageBonus);HavenStarterGear.Hit(this,attacker,defender as Mobile);}
 }
 public class HavenAdventurersRobe:Robe,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 4;}}
  [Constructable] public HavenAdventurersRobe(){Name="New Haven adventurer's robe";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenAdventurersRobe(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
 }
 public class HavenApprenticeGrimoire:Spellbook,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 5;}}
  [Constructable] public HavenApprenticeGrimoire():base(ulong.MaxValue){Name="apprentice grimoire";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenApprenticeGrimoire(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
 }
 public class HavenEvolvingCape:Cloak,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 6;}}
  [Constructable] public HavenEvolvingCape(){Name="Haven adventurer's leveling cape";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenEvolvingCape(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
 }
 public class HavenEvolvingSash:BodySash,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 7;}}
  [Constructable] public HavenEvolvingSash(){Name="Haven adventurer's evolving sash";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenEvolvingSash(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
 }
 public class HavenFortuneEarrings:GoldEarrings,IHavenStarterGear {
  private HavenStarterProgress _progress=new HavenStarterProgress();
  public HavenStarterProgress Progress {get{return _progress;}}
  public int Kind {get{return 8;}}
  [Constructable] public HavenFortuneEarrings(){Name="Starter Fortune Earrings";Hue=0x59B;LootType=LootType.Blessed;HavenStarterGear.Apply(this);}
  public HavenFortuneEarrings(Serial serial):base(serial){}
  public override bool CanEquip(Mobile from){return (_progress.Owner==null || _progress.Owner==from) && base.CanEquip(from);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);HavenStarterGear.Properties(this,list);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);_progress.Write(w);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_progress=new HavenStarterProgress(r);}
 }
}
