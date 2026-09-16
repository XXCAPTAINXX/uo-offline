using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.HavenPrototype
{
    public class HavenGearDurability : Item
    {
        static readonly Dictionary<int,HavenGearDurability> Records = new Dictionary<int,HavenGearDurability>();
        Item _equipment;
        public HavenGearDurability(Item equipment) : base(1) { _equipment=equipment;Visible=false;Movable=false;Internalize();Records[equipment.Serial.Value]=this; }
        public HavenGearDurability(Serial serial) : base(serial) { }
        public static void Apply(Item item)
        {
            if(item==null||item.Deleted||Records.ContainsKey(item.Serial.Value))return;
            int hits,max;Action<int,int> set;
            if(item is BaseWeapon){var gear=(BaseWeapon)item;hits=gear.HitPoints;max=gear.MaxHitPoints;set=(h,m)=>{gear.MaxHitPoints=m;gear.HitPoints=h;};}
            else if(item is BaseArmor){var gear=(BaseArmor)item;hits=gear.HitPoints;max=gear.MaxHitPoints;set=(h,m)=>{gear.MaxHitPoints=m;gear.HitPoints=h;};}
            else if(item is BaseClothing){var gear=(BaseClothing)item;hits=gear.HitPoints;max=gear.MaxHitPoints;set=(h,m)=>{gear.MaxHitPoints=m;gear.HitPoints=h;};}
            else if(item is BaseJewel){var gear=(BaseJewel)item;hits=gear.HitPoints;max=gear.MaxHitPoints;set=(h,m)=>{gear.MaxHitPoints=m;gear.HitPoints=h;};}
            else return;
            if(max<=0)return;
            new HavenGearDurability(item);
            if(max<255)set(Math.Max(0,255-Math.Max(0,max-hits)),255);
        }
        public static bool Supported(Item item) {return (item.Hue==0x489&&HavenMarks.Names.Contains(item.Name))||item is IHavenStarterGear||HavenAdvancedGear.AutoKind(item)>0||HavenAdvancedGear.Find(item)!=null||HavenEquipmentEvolution.Find(item)!=null;}
        public static void Initialize()
        {
            EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(5),()=>{if(HavenPreview.Enabled)foreach(var item in World.Items.Values.ToArray())if(Supported(item))Apply(item);});
            Timer.DelayCall(TimeSpan.FromMinutes(1),TimeSpan.FromMinutes(1),()=>{foreach(var record in Records.Values.ToArray())if(record._equipment==null||record._equipment.Deleted)record.Delete();});
        }
        public override void OnDelete(){if(_equipment!=null){HavenGearDurability record;if(Records.TryGetValue(_equipment.Serial.Value,out record)&&record==this)Records.Remove(_equipment.Serial.Value);}base.OnDelete();}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_equipment);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_equipment=r.ReadItem();if(_equipment!=null)Records[_equipment.Serial.Value]=this;}
    }
}
