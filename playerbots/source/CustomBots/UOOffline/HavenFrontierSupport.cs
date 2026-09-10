using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.CustomBots;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.UOOffline;

public static class HavenFrontierSupport
{
    internal static Point3D ShadowLanding => HavenOriginalDungeons.Installed ? HavenOriginalDungeons.ShadowEntrance : new Point3D(4760, 3242, 0);
    internal static Map ShadowMap => HavenOriginalDungeons.Installed ? Map.TerMur : Map.Trammel;
    internal static Point3D RiftLanding => HavenOriginalDungeons.Installed ? HavenOriginalDungeons.BlackthornLanding : new Point3D(4888, 3466, 0);
    internal static PlayerMobile Player(Mobile actor)
    {
        if (actor is HavenCompanion companion) { return companion.BoundOwner as PlayerMobile; }
        if (actor is BaseCreature pet) { return pet.GetMaster() as PlayerMobile; }
        return actor is PlayerBot bot ? HavenBotLoot.PlayerOwner(bot) ?? bot : actor as PlayerMobile;
    }
    internal static bool Travel(Mobile from, Point3D point, Map map = null)
    {
        map ??= point == ShadowLanding ? ShadowMap : Map.Trammel;
        if (from?.Deleted != false || !from.Alive || from.Criminal || from.Spell != null || SpellHelper.CheckCombat(from) ||
            !SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom, out _)) { return false; }
        for (var radius = 0; radius <= 3; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    var candidate = new Point3D(point.X + dx, point.Y + dy, point.Z);
                    if (!map.CanSpawnMobile(candidate) || !SpellHelper.CheckTravel(from, map, candidate, TravelCheckType.RecallTo, out _)) { continue; }
                    BaseCreature.TeleportPets(from, candidate, map); from.MoveToWorld(candidate, map); return true;
                }
            }
        }
        return false;
    }
    internal static void Return(Mobile member, Point3D point, Map map = null)
    {
        map ??= point == ShadowLanding ? ShadowMap : Map.Trammel;
        if (member?.Deleted != false) { return; }
        if (member.Map == Map.Internal)
        { member.LogoutLocation = point; member.LogoutMap = map; }
        else { BaseCreature.TeleportPets(member, point, map); member.MoveToWorld(point, map); }
    }
    internal static void Deliver(PlayerMobile player, Item reward)
    {
        if (player?.Deleted != false) { reward.Delete(); return; }
        if (player is PlayerBot bot) { HavenBotLoot.Receive(bot, reward); return; }
        if (player.Backpack == null) { player.AddItem(new Backpack()); }
        // Earned event deliveries survive a full pack and never spill at an offline location.
        player.Backpack.DropItem(reward);
    }
    internal static void Reward(PlayerMobile player, int gold, int marks, int shards)
    {
        Deliver(player, new Gold(gold));
        if (marks > 0) { Deliver(player, new HavenMark(marks)); }
        if (shards > 0) { Deliver(player, new AstralShard(shards)); }
        player.Backpack.FindItemByType<AdventurersWallet>()?.DepositBackpackShards(player);
    }
    internal static Item Relic(int choice, string source)
    {
        Item item;
        switch (choice)
        {
            case 0:
                var book = new Spellbook(ulong.MaxValue); book.Attributes.SpellDamage = 25; book.Attributes.LowerManaCost = 8;
                book.Attributes.CastSpeed = 1; book.Attributes.CastRecovery = 2; book.Attributes.RegenMana = 3; item = book; break;
            case 1:
                var ring = new GoldRing(); ring.Attributes.AttackChance = 15; ring.Attributes.DefendChance = 15;
                ring.Attributes.WeaponDamage = 25; ring.Attributes.SpellDamage = 15; ring.Attributes.LowerManaCost = 5; item = ring; break;
            default:
                var boots = new Boots(); boots.Attributes.Luck = 150; boots.Attributes.BonusDex = 5;
                boots.Attributes.RegenHits = 2; boots.Attributes.RegenStam = 3; boots.Attributes.RegenMana = 2; item = boots; break;
        }
        item.Name = $"{source} {(choice == 0 ? "grimoire" : choice == 1 ? "signet" : "wayfarer's boots")}";
        item.AddItem(new HavenLegendaryArtifact()); return item;
    }
}

[SerializationGenerator(0)]
public partial class HavenFrontierRecord : Item
{
    [SerializableField(0)] private Mobile _owner;
    [SerializableField(1)] private int _rooms;
    [SerializableField(2)] private int _roofs;
    [SerializableField(3)] private int _minaxCredits;
    [SerializableField(4)] private int _doubloons;
    [SerializableField(5)] private int _rifts;
    [SerializableField(6)] private int _voyages;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenFrontierRecord() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "frontier expedition record"; }
    internal static HavenFrontierRecord Get(Mobile player)
    {
        foreach (var item in player.Items) { if (item is HavenFrontierRecord record && record.Owner == player) { return record; } }
        var created = new HavenFrontierRecord { Owner = player }; player.AddItem(created); return created;
    }
    internal bool Buy(Mobile from, int choice, bool pirate)
    {
        if (from != Owner || Deleted || Parent != from || !from.Alive || choice is < 0 or > 2) { return false; }
        const int cost = 50;
        if ((pirate ? Doubloons : MinaxCredits) < cost) { return false; }
        var item = HavenFrontierSupport.Relic(choice, pirate ? "Corsair's" : "Blackthorn's");
        if (from.Backpack?.TryDropItem(from, item, false) != true) { item.Delete(); return false; }
        if (pirate) { Doubloons -= cost; } else { MinaxCredits -= cost; }
        return true;
    }
    public override void OnDelete() { Owner = null; base.OnDelete(); }
    public static void Initialize() => CommandSystem.Register("expeditions", AccessLevel.Player, e =>
    { e.Mobile.CloseGump<HavenFrontierJournal>(); e.Mobile.SendGump(new HavenFrontierJournal(e.Mobile)); });
}

public sealed class HavenFrontierJournal : Gump
{
    public HavenFrontierJournal(Mobile from) : base(60, 60)
    {
        var record = HavenFrontierRecord.Get(from); AddBackground(0, 0, 490, 365, 9270);
        AddLabel(25, 20, 1152, "Frontier expeditions");
        AddLabel(25, 52, 2101, $"Shadowguard roofs {record.Roofs} · Blackthorn rifts {record.Rifts} · Voyages {record.Voyages}");
        AddLabel(25, 80, 1152, $"Minax credits: {record.MinaxCredits}     Doubloons: {record.Doubloons}");
        AddLabel(25, 111, 2101, "Relics evolve. Each costs 50 of its event currency.");
        var names = new[] { "Grimoire", "Signet", "Wayfarer's boots" };
        for (var i = 0; i < 3; i++)
        {
            var y = 151 + i * 45; AddLabel(25, y, 1152, names[i]);
            AddButton(180, y, 4005, 4007, 10 + i); AddLabel(218, y, 2101, "Minax");
            AddButton(312, y, 4005, 4007, 20 + i); AddLabel(350, y, 2101, "Doubloons");
        }
        AddLabel(25, 302, 1152, "Rewards require participation; companion kills count for you.");
        AddButton(365, 330, 4017, 4019, 0); AddLabel(402, 330, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 0) { return; }
        var pirate = info.ButtonID >= 20; var choice = info.ButtonID - (pirate ? 20 : 10);
        if (!HavenFrontierRecord.Get(state.Mobile).Buy(state.Mobile, choice, pirate))
        { state.Mobile.SendMessage("You need 50 credits and room in your pack."); }
        state.Mobile.SendGump(new HavenFrontierJournal(state.Mobile));
    }
}
