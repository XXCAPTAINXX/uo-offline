// =========================================================================
// StarterEvolution.cs — starter weapons, spellbook and robe grow with play.
// =========================================================================

using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.CustomBots
{
    public static class StarterEvolution
    {
        public const int MaxWeaponLevel = 30;
        public const int MaxBookLevel = 30;
        public const int MaxRobeLevel = 20;

        public static bool GainWeaponXp(
            BaseWeapon weapon,
            Mobile attacker,
            Mobile defender,
            ref int level,
            ref int xp)
        {
            if (weapon == null || attacker?.Player != true || defender == null || defender.Player ||
                defender is BaseCreature { Summoned: true } ||
                defender is BaseCreature { Controlled: true } ||
                level >= MaxWeaponLevel)
            {
                return false;
            }

            level = Math.Max(1, level);
            xp++;
            int needed = 20 + level * 10;

            if (xp < needed)
            {
                return false;
            }

            xp -= needed;
            level++;
            ApplyWeaponStats(weapon, level);
            attacker.SendMessage(0x35, $"{weapon.Name} evolved to level {level}!");
            GainRobeXp(attacker, 5);
            return true;
        }

        public static void ApplyWeaponStats(BaseWeapon weapon, int level)
        {
            level = Math.Clamp(level, 1, MaxWeaponLevel);
            weapon.LootType = LootType.Blessed;
            weapon.Attributes.WeaponDamage = 10 + (level - 1) / 3;
            weapon.Attributes.HitChance = 5 + (level - 1) / 5;
            weapon.Attributes.WeaponSpeed = (level - 1) / 6;
            weapon.InvalidateProperties();
        }

        public static void OnSpellCast(Mobile caster)
        {
            if (caster?.Player != true || caster.Backpack == null)
            {
                return;
            }

            var book = Spellbook.FindEquippedSpellbook(caster) as EvolvingStarterFullSpellbook
                       ?? caster.Backpack.FindItemByType<EvolvingStarterFullSpellbook>();

            if (book != null)
            {
                book.GainEvolutionXp(caster);
                GainRobeXp(caster, 1);
            }
        }

        public static void GainRobeXp(Mobile wearer, int amount)
        {
            if (wearer == null || amount <= 0)
            {
                return;
            }

            foreach (var item in wearer.Items)
            {
                if (item is EvolvingStarterAdventurerRobe robe)
                {
                    robe.GainEvolutionXp(wearer, amount);
                    return;
                }
            }
        }

        public static void ApplyBookStats(StarterFullSpellbook book, int level)
        {
            level = Math.Clamp(level, 1, MaxBookLevel);
            book.Attributes.SpellDamage = (level - 1) / 3;
            book.Attributes.LowerManaCost = Math.Min(10, 3 + (level - 1) / 4);
            book.Attributes.RegenMana = (level - 1) / 8;
            book.Attributes.CastRecovery = level >= 20 ? 1 : 0;
            book.Attributes.CastSpeed = level >= 30 ? 1 : 0;
            book.InvalidateProperties();
        }

        public static void ApplyRobeStats(StarterAdventurerRobe robe, int level)
        {
            level = Math.Clamp(level, 1, MaxRobeLevel);

            int statBonus = 5 + (level - 1) / 5;
            robe.Attributes.BonusStr = statBonus;
            robe.Attributes.BonusDex = statBonus;
            robe.Attributes.BonusInt = statBonus;
            robe.Attributes.RegenHits = 1 + (level - 1) / 10;
            robe.Attributes.RegenStam = 1 + (level - 1) / 10;
            robe.Attributes.RegenMana = 1 + (level - 1) / 10;
            robe.Attributes.LowerManaCost = 5 + (level - 1) / 4;
            robe.Attributes.NightSight = 1;
            robe.InvalidateProperties();
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EvolvingStarterBroadsword : Broadsword
    {
        [SerializableField(0)] private int _evolutionLevel = 1;
        [SerializableField(1)] private int _evolutionXp;

        [Constructible]
        public EvolvingStarterBroadsword()
        {
            Name = "Evolving Starter Broadsword";
            StarterEvolution.ApplyWeaponStats(this, _evolutionLevel);
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);
            if (StarterEvolution.GainWeaponXp(this, attacker, defender, ref _evolutionLevel, ref _evolutionXp))
            {
                Name = $"Evolving Starter Broadsword [Lv {_evolutionLevel}]";
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Evolution Level: {_evolutionLevel}/{StarterEvolution.MaxWeaponLevel}");
            list.Add($"Evolution XP: {_evolutionXp:N0}/{20 + Math.Max(1, _evolutionLevel) * 10:N0}");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EvolvingStarterKryss : Kryss
    {
        [SerializableField(0)] private int _evolutionLevel = 1;
        [SerializableField(1)] private int _evolutionXp;

        [Constructible]
        public EvolvingStarterKryss()
        {
            Name = "Evolving Starter Kryss";
            StarterEvolution.ApplyWeaponStats(this, _evolutionLevel);
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);
            if (StarterEvolution.GainWeaponXp(this, attacker, defender, ref _evolutionLevel, ref _evolutionXp))
            {
                Name = $"Evolving Starter Kryss [Lv {_evolutionLevel}]";
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Evolution Level: {_evolutionLevel}/{StarterEvolution.MaxWeaponLevel}");
            list.Add($"Evolution XP: {_evolutionXp:N0}/{20 + Math.Max(1, _evolutionLevel) * 10:N0}");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EvolvingStarterMace : Mace
    {
        [SerializableField(0)] private int _evolutionLevel = 1;
        [SerializableField(1)] private int _evolutionXp;

        [Constructible]
        public EvolvingStarterMace()
        {
            Name = "Evolving Starter Mace";
            StarterEvolution.ApplyWeaponStats(this, _evolutionLevel);
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);
            if (StarterEvolution.GainWeaponXp(this, attacker, defender, ref _evolutionLevel, ref _evolutionXp))
            {
                Name = $"Evolving Starter Mace [Lv {_evolutionLevel}]";
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Evolution Level: {_evolutionLevel}/{StarterEvolution.MaxWeaponLevel}");
            list.Add($"Evolution XP: {_evolutionXp:N0}/{20 + Math.Max(1, _evolutionLevel) * 10:N0}");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EvolvingStarterBow : Bow
    {
        [SerializableField(0)] private int _evolutionLevel = 1;
        [SerializableField(1)] private int _evolutionXp;

        [Constructible]
        public EvolvingStarterBow()
        {
            Name = "Evolving Starter Bow";
            StarterEvolution.ApplyWeaponStats(this, _evolutionLevel);
        }

        public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
        {
            base.OnHit(attacker, defender, damageBonus);
            if (StarterEvolution.GainWeaponXp(this, attacker, defender, ref _evolutionLevel, ref _evolutionXp))
            {
                Name = $"Evolving Starter Bow [Lv {_evolutionLevel}]";
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Evolution Level: {_evolutionLevel}/{StarterEvolution.MaxWeaponLevel}");
            list.Add($"Evolution XP: {_evolutionXp:N0}/{20 + Math.Max(1, _evolutionLevel) * 10:N0}");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EvolvingStarterFullSpellbook : StarterFullSpellbook
    {
        [SerializableField(0)] private int _evolutionLevel = 1;
        [SerializableField(1)] private int _evolutionXp;

        [Constructible]
        public EvolvingStarterFullSpellbook()
        {
            Name = "Evolving Starter Full Spellbook";
            StarterEvolution.ApplyBookStats(this, _evolutionLevel);
        }

        public void GainEvolutionXp(Mobile caster)
        {
            if (_evolutionLevel >= StarterEvolution.MaxBookLevel)
            {
                return;
            }

            _evolutionLevel = Math.Max(1, _evolutionLevel);
            _evolutionXp++;
            int needed = 15 + _evolutionLevel * 8;

            if (_evolutionXp < needed)
            {
                return;
            }

            _evolutionXp -= needed;
            _evolutionLevel++;
            StarterEvolution.ApplyBookStats(this, _evolutionLevel);
            caster?.SendMessage(0x35, $"Your starter spellbook evolved to level {_evolutionLevel}!");
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Evolution Level: {Math.Max(1, _evolutionLevel)}/{StarterEvolution.MaxBookLevel}");
            list.Add($"Evolution XP: {_evolutionXp:N0}/{15 + Math.Max(1, _evolutionLevel) * 8:N0}");
        }
    }

    [SerializationGenerator(0, false)]
    public partial class EvolvingStarterAdventurerRobe : StarterAdventurerRobe
    {
        [SerializableField(0)] private int _evolutionLevel = 1;
        [SerializableField(1)] private int _evolutionXp;

        [Constructible]
        public EvolvingStarterAdventurerRobe()
        {
            Name = "Evolving Starter Adventurer Robe";
            StarterEvolution.ApplyRobeStats(this, _evolutionLevel);
        }

        public void GainEvolutionXp(Mobile wearer, int amount)
        {
            if (_evolutionLevel >= StarterEvolution.MaxRobeLevel || amount <= 0)
            {
                return;
            }

            _evolutionLevel = Math.Max(1, _evolutionLevel);
            _evolutionXp += amount;
            int needed = 75 + _evolutionLevel * 25;

            if (_evolutionXp < needed)
            {
                return;
            }

            _evolutionXp -= needed;
            _evolutionLevel++;
            StarterEvolution.ApplyRobeStats(this, _evolutionLevel);
            wearer?.SendMessage(0x35, $"Your starter robe evolved to level {_evolutionLevel}!");
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Evolution Level: {Math.Max(1, _evolutionLevel)}/{StarterEvolution.MaxRobeLevel}");
            list.Add($"Evolution XP: {_evolutionXp:N0}/{75 + Math.Max(1, _evolutionLevel) * 25:N0}");
        }
    }
}
