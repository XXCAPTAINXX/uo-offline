using System;
using System.Linq;
using Server;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;

public static class RoleSmoke
{
    private static HavenCompanion _companion;
    private static Mobile _owner;
    private static Orc _enemy;
    private static Action<string,Action> _check;
    private static Action _next;
    private static int _hits;
    private static void Require(bool value,string message) { if (!value) throw new Exception(message); }
    private static void Clear()
    {
        if (_enemy != null) _enemy.Delete();
        _companion.Combatant = null; _owner.Combatant = null;
        _companion.Aggressors.Clear(); _companion.Aggressed.Clear(); _owner.Aggressors.Clear(); _owner.Aggressed.Clear();
        var movementLock = XmlAttach.FindAttachment(_companion,typeof(XmlData),"NoSpecials");
        if (movementLock != null) movementLock.Delete();
        _companion.MoveToWorld(_owner.Location,_owner.Map);
        _companion.SetOrder(_owner,OrderType.Stay);
    }
    public static void Run(HavenCompanion companion,Mobile owner,Mobile stranger,Action<string,Action> check,Action next)
    {
        _companion=companion; _owner=owner; _check=check; _next=next;
        check("role switch rejects stranger and invalid role",()=>Require(!companion.SetRole(stranger,CompanionRole.Caster) && !companion.SetRole(owner,(CompanionRole)99),"Invalid role request accepted"));
        check("full pack leaves role and equipped gear intact",()=>{
            var weapon=companion.FindItemOnLayer(Layer.OneHanded); var shield=companion.FindItemOnLayer(Layer.TwoHanded);
            companion.Backpack.MaxItems=companion.Backpack.TotalItems+1;
            Require(!companion.SetRole(owner,CompanionRole.Caster) && weapon.Parent==companion && shield.Parent==companion,"Partial role change");
            companion.Backpack.MaxItems=1000;
        });
        check("caster installs one native mage AI",()=>{
            Require(companion.SetRole(owner,CompanionRole.Caster),"Caster switch failed");
            var ai=companion.AIObject;
            Require(ai is HavenCompanionMageAI && companion.SetRole(owner,CompanionRole.Caster) && companion.AIObject==ai,"Duplicate or wrong AI");
        });
        StartRanged();
        Timer.DelayCall(TimeSpan.FromSeconds(20),AfterCaster);
    }
    private static void StartRanged()
    {
        _companion.Hits=_companion.HitsMax; _companion.Mana=_companion.ManaMax;
        _enemy=new Orc(); _enemy.SetHits(3000); _enemy.Hits=_enemy.HitsMax;
        XmlAttach.AttachTo(_enemy,new XmlData("NoSpecials","True"));
        _enemy.MoveToWorld(new Point3D(_owner.X+3,_owner.Y,_owner.Z),_owner.Map);
        _hits=_enemy.Hits;
        XmlAttach.AttachTo(_companion,new XmlData("NoSpecials","True"));
        // Fixture owner has no network connection, so its sector does not activate a new AI.
        _companion.AIObject.Activate();
        _check("ranged role accepts explicit enemy: "+_companion.Role,()=>Require(_companion.Attack(_owner,_enemy),"Attack failed"));
        _check("combat prevents changing role",()=>Require(!_companion.SetRole(_owner,CompanionRole.Warrior),"Combat role switch allowed"));
        _companion.AIObject.DoOrderAttack();
    }
    private static void AfterCaster()
    {
        _check("native mage AI deals damage outside melee range",()=>Require(_enemy.Hits<_hits && !_companion.InRange(_enemy,1),"No ranged spell damage; hits="+_enemy.Hits+" distance="+_companion.GetDistanceToSqrt(_enemy)+" spell="+_companion.Spell));
        Clear();
        _check("stay cancels queued casting and targets",()=>Require(_companion.Spell==null && _companion.Target==null,"Cast survived stop order"));
        Timer.DelayCall(TimeSpan.FromSeconds(3),StartArcher);
    }
    private static void StartArcher()
    {
        _check("archer equips native bow and AI",()=>Require(_companion.SetRole(_owner,CompanionRole.Archer) && _companion.Weapon is Bow && _companion.AIObject is HavenCompanionArcherAI,"Archer failed"));
        StartRanged();
        Timer.DelayCall(TimeSpan.FromSeconds(15),AfterArcher);
    }
    private static void AfterArcher()
    {
        _check("native archer damages target at range",()=>Require(_enemy.Hits<_hits && !_companion.InRange(_enemy,1),"No ranged bow damage"));
        Clear();
        _check("repeated roles preserve one sword shield and bow",()=>{
            for(int i=0;i<3;i++) { Require(_companion.SetRole(_owner,CompanionRole.Warrior),"Warrior failed"); Require(_companion.SetRole(_owner,CompanionRole.Archer),"Archer failed"); }
            var items=_companion.Items.Concat(_companion.Backpack.Items).ToArray();
            Require(items.Count(x=>x is Longsword)==1 && items.Count(x=>x is MetalShield)==1 && items.Count(x=>x is Bow)==1,"Equipment duplicated");
        });
        _companion.SetOrder(_owner,OrderType.Stay);
        _next();
    }
}

