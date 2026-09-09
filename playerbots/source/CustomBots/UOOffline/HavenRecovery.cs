using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public static class HavenRecovery
{
    public static readonly Point3D BankLocation = new(3488, 2578, 20);

    public static void GoToBank(Mobile from)
    {
        if (from == null || from.Deleted || !from.Player) { return; }
        if (!FindLocation(BankLocation, out var destination))
        {
            from.SendMessage("The bank arrival area is blocked. Please try again.");
            return;
        }
        if (from.Alive) { BaseCreature.TeleportPets(from, destination, Map.Trammel); }
        from.MoveToWorld(destination, Map.Trammel);
        from.PlaySound(0x1FE);
        from.SendMessage("Welcome to New Haven bank. The healer and corpse summoner are beside the bank.");
    }

    public static void EnsureServices()
    {
        EnsureNpc<HavenBankHealer>(new Point3D(3491, 2580, 20));
        EnsureNpc<HavenCorpseSummoner>(new Point3D(3491, 2583, 20));
    }

    private static void EnsureNpc<T>(Point3D preferred) where T : BaseCreature, new()
    {
        foreach (var npc in Map.Trammel.GetMobilesInRange<T>(preferred, 8))
        {
            if (!npc.Deleted) { return; }
        }
        if (!FindLocation(preferred, out var location))
        {
            Server.Logging.LogFactory.GetLogger(typeof(HavenRecovery)).Warning(
                "Could not place Haven recovery NPC {NpcType}", typeof(T).Name);
            return;
        }
        var healer = new T();
        healer.MoveToWorld(location, Map.Trammel);
    }

    internal static bool FindLocation(Point3D preferred, out Point3D location)
    {
        var map = Map.Trammel;
        for (var radius = 0; radius <= 5; radius++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    if (Math.Max(Math.Abs(x), Math.Abs(y)) != radius) { continue; }
                    var px = preferred.X + x;
                    var py = preferred.Y + y;
                    if (map.CanSpawnMobile(px, py, preferred.Z))
                    {
                        location = new Point3D(px, py, preferred.Z);
                        return true;
                    }
                    var z = map.GetAverageZ(px, py);
                    if (map.CanSpawnMobile(px, py, z))
                    {
                        location = new Point3D(px, py, z);
                        return true;
                    }
                }
            }
        }
        location = default;
        return false;
    }

    internal static bool InServiceRange(Mobile from, Mobile npc) =>
        from != null && !from.Deleted && from.Player && npc != null && !npc.Deleted &&
        from.Map == Map.Trammel && npc.Map == from.Map && from.InRange(npc.Location, 3);

    internal static bool RecoverCorpse(Mobile from, HavenCorpseSummoner npc)
    {
        if (!InServiceRange(from, npc))
        {
            from?.SendMessage("Stand beside the corpse summoner to recover your body.");
            return false;
        }
        if (from.Corpse is not Corpse corpse || corpse.Deleted || corpse.Owner != from)
        {
            from.SendMessage("You have no surviving recent corpse to summon. Decayed bodies cannot be restored.");
            return false;
        }
        // Move the actual owned corpse: never copy its items or reset decay.
        corpse.MoveToWorld(from.Location, from.Map);
        from.PlaySound(0x1FE);
        from.SendMessage("Your corpse has been brought to your feet. Resurrect before collecting its remaining items.");
        return true;
    }
}

[SerializationGenerator(0)]
public partial class HavenBankHealer : Healer
{
    [Constructible]
    public HavenBankHealer()
    {
        Name = "New Haven resurrection healer";
        Title = "free resurrection";
        CantWalk = true;
    }

    public override bool CheckResurrect(Mobile m) => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (!HavenRecovery.InServiceRange(from, this))
        {
            from.SendMessage("Stand beside the healer for resurrection.");
            return;
        }
        if (from.Alive)
        {
            from.SendMessage("You are already alive. The corpse summoner can recover your body.");
            return;
        }
        OfferResurrection(from);
    }
}

[SerializationGenerator(0)]
public partial class HavenCorpseSummoner : BaseCreature
{
    [Constructible]
    public HavenCorpseSummoner() : base(AIType.AI_Animal)
    {
        FightMode = FightMode.None;
        Name = "New Haven corpse summoner";
        Title = "free corpse recovery";
        Body = 0x190;
        Hue = 0x83EA;
        CantWalk = true;
        Blessed = true;
        AddItem(new Robe(0x482));
        AddItem(new Sandals());
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!HavenRecovery.InServiceRange(from, this))
        {
            from.SendMessage("Stand beside the corpse summoner to recover your body.");
            return;
        }
        from.CloseGump<HavenCorpseRecoveryGump>();
        from.SendGump(new HavenCorpseRecoveryGump(this));
    }
}

public sealed class HavenCorpseRecoveryGump : Gump
{
    private readonly HavenCorpseSummoner _summoner;

    public HavenCorpseRecoveryGump(HavenCorpseSummoner summoner) : base(30, 30)
    {
        _summoner = summoner;
        AddBackground(0, 0, 460, 260, 5054);
        AddBackground(12, 12, 436, 236, 3000);
        AddHtml(30, 25, 400, 30, "<B>Recover your corpse — free</B>");
        AddHtml(30, 70, 400, 95, "Bring your most recent surviving corpse and its remaining items to your feet. This works while dead or alive. It cannot restore decayed bodies or items already removed.");
        AddButton(30, 200, 4005, 4007, 1);
        AddLabel(68, 202, 0, "Summon my corpse");
        AddButton(315, 200, 4005, 4007, 0);
        AddLabel(353, 202, 0, "Close");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1) { HavenRecovery.RecoverCorpse(sender.Mobile, _summoner); }
    }
}
