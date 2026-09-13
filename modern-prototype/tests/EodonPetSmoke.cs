using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;

public static class EodonPetSmoke
{
    public static void Persistence(bool reload, Action<string> log)
    {
        if (!reload)
        {
            var owner = new PlayerMobile { Player = true, Body = 0x190, FollowersMax = 20 };
            var account = new Server.Accounting.Account("eodon-save-" + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N")); account[0] = owner;
            owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(3498,2582,14), Map.Trammel);
            var tricer = new HavenStonehornTriceratops(); var tiger = new HavenSunfangTiger();
            foreach(var pet in new HavenEodonMount[] {tricer,tiger}) { pet.MoveToWorld(owner.Location,owner.Map); pet.SetControlMaster(owner); pet.IsBonded=true; HavenPetMissions.ApplyRarity(pet,3); }
            tricer.Rider=owner;
            var ticket=HavenPetTicket.Store(tiger,owner,owner.Backpack);
            if(ticket==null)throw new Exception("Cannot persist pet book fixture");
            HavenEodonHabitats.Ensure();
            World.Save(false,false);
            System.IO.File.WriteAllLines("eodon-save-identities.txt",new[]{owner.Serial.Value,tricer.Serial.Value,tiger.Serial.Value,ticket.Serial.Value}.Select(x=>x.ToString()));
            log("PASS saved mounted triceratops, book-stored tiger, rarity and habitats");
        }
        else
        {
            var ids=System.IO.File.ReadAllLines("eodon-save-identities.txt").Select(int.Parse).ToArray();
            var owner=World.FindMobile((Serial)ids[0]) as PlayerMobile;
            var tricer=World.FindMobile((Serial)ids[1]) as HavenStonehornTriceratops;
            var tiger=World.FindMobile((Serial)ids[2]) as HavenSunfangTiger;
            var ticket=World.FindItem((Serial)ids[3]) as HavenPetTicket;
            if(owner==null||tricer==null||tiger==null||ticket==null||tricer.Rider!=owner||owner.Mount!=tricer||ticket.Pet!=tiger||!(ticket.Parent is HavenPetBook))throw new Exception("Pet persistence relationship failed");
            if(HavenPetDefenses.Tier(tricer)!=3||HavenPetDefenses.Tier(tiger)!=3||tricer.TrainingDefinition==null||tiger.TrainingDefinition==null)throw new Exception("Rarity or training lost");
            owner.MoveToWorld(new Point3D(3498,2582,14),Map.Trammel); tricer.Rider=null;if(!ticket.Claim(owner))throw new Exception("Stored tiger claim failed after reload");
            if(World.Items.Values.OfType<HavenEodonPetSpawner>().Count(s=>!s.Deleted)!=2)throw new Exception("Habitat persistence failed");
            log("PASS separate-process reload: rider, exact book pet, rarity, training and both habitats");
        }
    }
    public static void Run(Action<string> log)
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190, FollowersMax = 20 };
        var account = new Server.Accounting.Account("eodon-check-" + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N")); account[0] = owner;
        owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(3498, 2582, 14), Map.Trammel);
        try
        {
            foreach (var pet in new HavenEodonMount[] { new HavenStonehornTriceratops(), new HavenSunfangTiger() })
            {
                try
                {
                    pet.MoveToWorld(owner.Location, owner.Map); pet.SetControlMaster(owner); pet.IsBonded = true;
                    var definition = pet.TrainingDefinition;
                    if (definition == null) throw new Exception("Missing training definition: " + pet.GetType().Name);
                    var strength = pet.RawStr; HavenPetMissions.ApplyRarity(pet, 3);
                    if (pet.ControlSlots != 1 || pet.RawStr < strength || HavenPetDefenses.Tier(pet) != 3) throw new Exception("Rarity failed");
                    pet.Rider = owner;
                    var item = owner.FindItemOnLayer(Layer.Mount);
                    if (item == null || item.ItemID != pet.Body.BodyID || pet.Map != Map.Internal || pet.Rider != owner) throw new Exception("Mount identity failed");
                    if (BitConverter.ToUInt16(System.IO.File.ReadAllBytes(Core.FindDataFile("tiledata.mul")), 512*(4+32*30)+(item.ItemID/32)*(4+32*41)+4+(item.ItemID%32)*41+14) != 0) throw new Exception("Client graphic fallback requires zero animation ID");
                    pet.Rider = null;
                    if (pet.Map != owner.Map || owner.Mount != null) throw new Exception("Dismount failed");
                    var enemy = new Ogre(); enemy.MoveToWorld(owner.Location, owner.Map);
                    try
                    {
                        enemy.Stam = enemy.StamMax; pet.Combatant = enemy;
                        int before = enemy.Hits; int stamina = enemy.Stam;
                        if (!HavenPetSignatures.Activate(pet, enemy)) throw new Exception("Signature did not activate");
                        if (pet is HavenStonehornTriceratops && (HavenPetSignatures.GuardPercent(pet) != 24 || enemy.Stam >= stamina)) throw new Exception("Horn guard failed");
                        if (pet is HavenSunfangTiger && enemy.Hits >= before) throw new Exception("Pounce failed");
                        if (HavenPetSignatures.Activate(pet, enemy)) throw new Exception("Signature ignored cooldown");
                    }
                    finally { enemy.Delete(); }
                    var ticket = HavenPetTicket.Store(pet, owner, owner.Backpack); if(ticket == null || !(ticket.Parent is HavenPetBook) || !ticket.Claim(owner) || pet.ControlMaster != owner) throw new Exception("Pet book round trip failed");
                    log("PASS " + pet.GetType().Name + ": native body mount/dismount, Legendary rarity, training, signature and cooldown");
                }
                finally { pet.Rider = null; pet.Delete(); }
            }
            HavenEodonHabitats.Ensure();
            var sites = World.Items.Values.OfType<HavenEodonPetSpawner>().ToArray();
            foreach (var site in sites)
            {
                var pet = site.GetSpawn().OfType<BaseCreature>().Single(); pet.SetControlMaster(owner); pet.Owners.Add(owner);
                site.Spawn();
                if (site.GetSpawn().Contains(pet) || site.GetSpawn().Count() != 1 || pet.Deleted) throw new Exception("Tame blocked respawn or deleted pet");
                log("PASS habitat " + (site.Tiger ? "tiger" : "triceratops") + " " + site.Location + " " + site.Map + ": replaced tame without deleting it");
                pet.Delete(); site.Delete();
            }
            if (sites.Length != 2) throw new Exception("Expected two native habitat sites, found " + sites.Length);
        }
        finally { owner.Delete(); }
    }
}



