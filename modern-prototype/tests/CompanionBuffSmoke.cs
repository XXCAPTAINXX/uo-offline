using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.SkillMasteries;
using Server.HavenPrototype;

public static class CompanionBuffSmoke
{
    public static void Run(Mobile location, Action<string> report)
    {
        var hound = new HavenAncientHellhound(); HavenHellhoundBreath.Ensure(hound); if (PetTrainingHelper.GetAbilityProfile(hound).HasAbility(SpecialAbility.DragonBreath) || hound.TrainingDefinition.SpecialAbilities.Contains(SpecialAbility.DragonBreath)) throw new Exception("Innate breath still consumes a training ability"); hound.Delete();
        var bear = new HavenSnowBear(); foreach(var skill in PetTrainingHelper.MagicSkills.Concat(PetTrainingHelper.CombatSkills)) if(!PetTrainingHelper.ValidateTrainingPoint(bear,skill)) throw new Exception("Bear skill is excluded: "+skill); foreach(var ability in PetTrainingHelper.MagicalAbilities) if(!PetTrainingHelper.ValidateTrainingPoint(bear,ability)) throw new Exception("Bear magical ability is excluded: "+ability); bear.Delete();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        new Account("bard-check-" + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"))[0] = owner;
        owner.MoveToWorld(location.Location, location.Map);
        var companion = HavenCompanion.Claim(owner);
        if (companion == null || !companion.SetRole(owner, CompanionRole.Bard)) throw new Exception("Bard fixture failed");
        companion.Skills.Musicianship.Base = 150; companion.Skills.Peacemaking.Base = 150; companion.SetInt(1000); companion.Mana = 1000;
        Server.Items.BaseInstrument.SetInstrument(companion, companion.Backpack.FindItemByType(typeof(HavenCompanionLute), true) as Server.Items.BaseInstrument);
        var songs = new BardSpell[] { new ResilienceSpell(companion, null), new PerseveranceSpell(companion, null) };
        try
        {
            if (companion.ChooseBardMastery(null) != SkillName.Peacemaking) throw new Exception("Defensive song priority missing");
            foreach (var song in songs)
            {
                if (song.PartyRange != 24) throw new Exception("Companion bard range not extended");
                if (!song.CheckCast()) throw new Exception("Bard mastery cannot cast");
                companion.Spell = song; song.State = SpellState.Sequencing; song.OnCast();
                if (song.Timer == null || !song.PartyList.Contains(owner) || song.PropertyBonus() <= 0 || SkillMasterySpell.GetSpellForParty(owner, song.GetType()) != song) throw new Exception("Owner missing real song effect without party");
            }
            var buffs = (Dictionary<BuffIcon, BuffInfo>)typeof(PlayerMobile).GetField("m_BuffTable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
            if (buffs == null || !buffs.ContainsKey(BuffIcon.Resilience) || !buffs.ContainsKey(BuffIcon.Perseverance)) throw new Exception("Native bard buff icons missing");
            owner.MoveToWorld(new Point3D(location.X + 30, location.Y + 30, location.Z), location.Map);
            foreach (var song in songs) song.UpdateParty();
            if (buffs.ContainsKey(BuffIcon.Resilience) || buffs.ContainsKey(BuffIcon.Perseverance)) throw new Exception("Out-of-range icons remained");
            report("PASS real Resilience/Perseverance effects and native buff icons reach bound owner without party, then clear out of range");
            companion.Kill();
            if (!companion.IsDeadPet) throw new Exception("Death fixture did not create ghost companion");
            companion.MoveToWorld(new Point3D(1000, 1000, 0), Map.Felucca);
            if (!companion.Recall(owner) || companion.Map != owner.Map || companion.Location != owner.Location) throw new Exception("Dead cross-map companion recall failed");
            if (!companion.RecoverFromDeath(DateTime.UtcNow.AddSeconds(6)) || companion.IsDeadPet) throw new Exception("Recalled companion did not recover");
            companion.IsStabled = true; owner.Stabled.Add(companion); companion.Internalize();
            if (!companion.Recall(owner) || companion.IsStabled || owner.Stabled.Contains(companion)) throw new Exception("Stabled recall failed");
            var enemy = new Ogre(); enemy.MoveToWorld(owner.Location, owner.Map); companion.DoHarmful(enemy); owner.DoHarmful(enemy);
            if (!companion.Recall(owner)) throw new Exception("Combat recall failed");
            enemy.Delete();
            report("PASS companion Recall works dead, across maps, from stables and during combat; normal recovery restores the same companion");
        }
        finally { foreach (var song in songs) song.Expire(); companion.Delete(); owner.Delete(); }
    }
}
