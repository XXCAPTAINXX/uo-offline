using System;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenDungeonWeapons
    {
        public static readonly string[] Names = { "Abyssward", "Cryptwarden", "Scalebreaker", "Webcleaver", "Thornbane", "Stormguard", "Primal Warden" };
        public static readonly string[] Routes = {
            "Hythloth, Fire, Blood and Stygian Abyss: demons and elementals",
            "Deceit, Covetous and Khaldun: undead and humanoids",
            "Destard, Ice and Fire: reptiles and elementals",
            "Terathan Keep, Despise and Wrong: arachnids and humanoids",
            "Twisted Weald, Blighted Grove and Prism of Light: fey and elementals",
            "Shame, Blackthorn's and Shadowguard: elementals and humanoids",
            "Eodon and Myrmidex hunting: Eodon creatures and arachnids"
        };
        public static readonly SlayerName[] First = { SlayerName.Exorcism, SlayerName.Silver, SlayerName.ReptilianDeath, SlayerName.ArachnidDoom, SlayerName.Fey, SlayerName.ElementalBan, SlayerName.Eodon };
        public static readonly SlayerName[] Second = { SlayerName.ElementalBan, SlayerName.Repond, SlayerName.ElementalBan, SlayerName.Repond, SlayerName.ElementalBan, SlayerName.Repond, SlayerName.ArachnidDoom };
        public static int Valid(int theme) { return Math.Max(0, Math.Min(Names.Length - 1, theme)); }
        public static void Outfit(BaseWeapon weapon, int theme, string form) { theme = Valid(theme); weapon.Name = Names[theme] + " " + form; weapon.Hue = 0x48D; weapon.Slayer = First[theme]; weapon.Slayer2 = Second[theme]; }
        public static Item Create(int theme, bool mace) { return mace ? (Item)new HavenDungeonMace(theme) : new HavenDungeonScimitar(theme); }
        public static void AddCatalog(System.Collections.Generic.List<HavenShopEntry> list)
        {
            for (int i = 0; i < Names.Length; i++) for (int form = 0; form < 2; form++)
            {
                int theme = i; bool mace = form == 1;
                list.Add(new HavenShopEntry(Names[i] + (mace ? " Mace" : " Scimitar"), 0, () => Create(theme, mace), Routes[i] + ". Two slayers; one-handed Whirlwind; evolves to level 20. Mixed dungeons may need another slayer pairing.", 225));
            }
        }
        public static void Award(BaseCreature victim, Mobile player)
        {
            if (!HavenStarterGear.Eligible(victim, player) || (victim.HitsMax < 4000 && !(victim is BaseChampion)) || victim is DemonKnight || victim is ShadowKnight || victim is DarknightCreeper || victim is FleshRenderer || victim is Impaler || victim is AbysmalHorror || Utility.RandomDouble() >= .04) return;
            var choices = Enumerable.Range(0, Names.Length).Where(i => SlayerGroup.GetEntryByName(First[i]).Slays(victim) || SlayerGroup.GetEntryByName(Second[i]).Slays(victim)).ToArray();
            if (choices.Length > 0) HavenAdvancedRewards.Deliver(player, victim, Create(choices[Utility.Random(choices.Length)], Utility.RandomBool()));
        }
    }
    public class HavenDungeonScimitar : HavenCycloneScimitar
    {
        int _theme;
        [Constructable] public HavenDungeonScimitar() : this(0) { }
        public HavenDungeonScimitar(int theme) { _theme = HavenDungeonWeapons.Valid(theme); HavenDungeonWeapons.Outfit(this, _theme, "Scimitar"); }
        public HavenDungeonScimitar(Serial serial) : base(serial) { }
        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); list.Add(HavenDungeonWeapons.Routes[HavenDungeonWeapons.Valid(_theme)]); }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(_theme); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); _theme = HavenDungeonWeapons.Valid(r.ReadInt()); }
    }
    public class HavenDungeonMace : HavenCycloneMace
    {
        int _theme;
        [Constructable] public HavenDungeonMace() : this(0) { }
        public HavenDungeonMace(int theme) { _theme = HavenDungeonWeapons.Valid(theme); HavenDungeonWeapons.Outfit(this, _theme, "Mace"); }
        public HavenDungeonMace(Serial serial) : base(serial) { }
        public override void GetProperties(ObjectPropertyList list) { base.GetProperties(list); list.Add(HavenDungeonWeapons.Routes[HavenDungeonWeapons.Valid(_theme)]); }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(_theme); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); _theme = HavenDungeonWeapons.Valid(r.ReadInt()); }
    }
}
