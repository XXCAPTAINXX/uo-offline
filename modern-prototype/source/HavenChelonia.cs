using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype {
public partial class HavenChelonia : Item
{
    internal static readonly Point3D Site = new Point3D(4095, 3473, 0);
    internal static readonly Point3D Landing = new Point3D(4094, 3475, 0);
    internal static readonly HashSet<HavenChelonia> Registry = new HashSet<HavenChelonia>();
    public List<Item> Fixtures = new List<Item>();
    public List<HavenChelonian> Tortoises = new List<HavenChelonian>();
    public DateTime NextSpawn;
    private Timer _timer;
    [Constructable]
    public HavenChelonia() : base(0x1E5E) { Name = "Chelonia — sanctuary field guide"; Movable = false; }
    
    private void Register()
    {
        Registry.Add(this); _timer?.Stop();
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Tick);
    }
    internal void Build()
    {
        if (Fixtures.Count != 0) { return; }
        // The original offshore floor is absent on the modern map. Keep this
        // native shoreline open rather than copying unsupported offshore props.
        foreach(var offset in new[]{new Point2D(-2,-2),new Point2D(-2,5)}){
            var point=new Point3D(X+offset.X,Y+offset.Y,Map.GetAverageZ(X+offset.X,Y+offset.Y));
            if(Map.CanFit(point,16,false,false)){var plant=new Static(0xC93);plant.MoveToWorld(point,Map);Fixtures.Add(plant);}
        }
        var gate=new Moongate(new Point3D(3506,2570,14),Map.Trammel);gate.MoveToWorld(new Point3D(X-1,Y+2,Z),Map);Fixtures.Add(gate);
        Register(); Tick(); 
    }
    internal void Tick()
    {
        if (Deleted || Map != Map.Trammel) { return; }
        for (var i = Tortoises.Count - 1; i >= 0; i--)
        {
            var pet = Tortoises[i];
            if (pet == null || pet.Deleted || pet.Controlled || pet.Owners.Count > 0)
            { Tortoises.RemoveAt(i); NextSpawn = DateTime.UtcNow + TimeSpan.FromSeconds(15);  }
            else if (pet.Map != Map || !pet.InRange(this, 50))
            { pet.MoveToWorld(new Point3D(X + 6 + i * 3, Y + 4, 0), Map); }
        }
        if (Tortoises.Count >= 2 || DateTime.UtcNow < NextSpawn) { return; }
        var point = new Point3D(X + 6 + Tortoises.Count * 3, Y + 4, 0);
        if (!Map.CanSpawnMobile(point)) { return; }
        var created = new HavenChelonian(); var roll = Utility.RandomDouble(); var tier = roll < .45 ? 1 : roll < .80 ? 2 : 3;
        HavenPetMissions.ApplyRarity(created, tier); created.MinTameSkill = 80 + tier * 10;
        created.Home = point; created.RangeHome = 12; created.MoveToWorld(point, Map); Tortoises.Add(created); 
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(this, 4)) { return; }
        from.SendMessage("Chelonia's tide tortoises fight on land and at sea. Rare/Epic/Legendary need 90/100/110 Taming. Feed fish or produce. Wild replacements return in about 15 seconds; tamed pets are preserved.");
    }
    public HavenChelonia(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Fixtures.Count);foreach(var item in Fixtures)w.Write(item);w.Write(Tortoises.Count);foreach(var pet in Tortoises)w.Write(pet);w.Write(NextSpawn);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();int count=r.ReadInt();if(count<0||count>200)throw new InvalidOperationException("Invalid sanctuary fixture count");for(int i=0;i<count;i++){var item=r.ReadItem();if(item!=null)Fixtures.Add(item);}count=r.ReadInt();if(count<0||count>2)throw new InvalidOperationException("Invalid sanctuary pet count");for(int i=0;i<count;i++){var pet=r.ReadMobile() as HavenChelonian;if(pet!=null)Tortoises.Add(pet);}NextSpawn=r.ReadDateTime();if(NextSpawn>DateTime.UtcNow.AddSeconds(15))NextSpawn=DateTime.UtcNow.AddSeconds(15);Timer.DelayCall(TimeSpan.Zero,Register);}
    public override void OnDelete()
    {
        _timer?.Stop(); _timer = null; Registry.Remove(this);
        foreach (var item in Fixtures) { item?.Delete(); }
        foreach (var pet in Tortoises) { if (pet?.Deleted == false && !pet.Controlled && pet.Owners.Count == 0) { pet.Delete(); } }
        Fixtures.Clear(); Tortoises.Clear(); base.OnDelete();
    }
}

}
