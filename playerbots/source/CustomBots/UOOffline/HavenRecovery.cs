using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public static class HavenRecovery
{
    public static readonly Point3D BankLocation = new(3491, 2584, 20);
    public static readonly Point3D HealerLocation = new(3491, 2582, 20);

    public static void GoToBank(Mobile from)
    {
        if (from == null || from.Deleted || !from.Player) { return; }
        EnsureServices();
        HavenBankHealer healer = null;
        foreach (var candidate in Map.Trammel.GetMobilesInRange<HavenBankHealer>(HealerLocation, 8))
        {
            if (!candidate.Deleted) { healer = candidate; break; }
        }
        if (healer == null || !FindLocation(new Point3D(healer.X, healer.Y + 1, healer.Z), out var destination, 1))
        {
            from.SendMessage("The bank arrival area is blocked. Please try again.");
            return;
        }
        BaseCreature.TeleportPets(from, destination, Map.Trammel);
        from.MoveToWorld(destination, Map.Trammel);
        from.PlaySound(0x1FE);
        from.SendMessage("Welcome to New Haven bank. The healer and corpse summoner are beside the bank.");
    }

    public static void EnsureServices()
    {
        EnsureNpc<HavenBankHealer>(HealerLocation, "Elias Thorne", "the healer - free resurrection");
        EnsureNpc<HavenCorpseSummoner>(new Point3D(3494, 2582, 20), "Silas Grey", "the spirit guide - free corpse recovery");
        EnsureNpc<HavenPetHealer>(new Point3D(3497, 2582, 20), "Mira Willow", "the veterinarian - free pet resurrection");
    }

    private static void EnsureNpc<T>(Point3D preferred, string name, string title) where T : BaseCreature, new()
    {
        foreach (var npc in Map.Trammel.GetMobilesInRange<T>(preferred, 8))
        {
            if (!npc.Deleted)
            {
                npc.Name = name;
                npc.Title = title;
                npc.Female = npc is HavenPetHealer;
                npc.Body = npc.Female ? 0x191 : 0x190;
                if (npc.HairItemID == 0) { npc.HairItemID = npc.Female ? 0x203C : 0x203B; npc.HairHue = 0x455; }
                if (npc.Location != preferred && FindLocation(preferred, out var moved)) { npc.MoveToWorld(moved, Map.Trammel); }
                return;
            }
        }
        if (!FindLocation(preferred, out var location))
        {
            Server.Logging.LogFactory.GetLogger(typeof(HavenRecovery)).Warning(
                "Could not place Haven recovery NPC {NpcType}", typeof(T).Name);
            return;
        }
        var healer = new T();
        healer.Name = name;
        healer.Title = title;
        healer.Female = healer is HavenPetHealer;
        healer.Body = healer.Female ? 0x191 : 0x190;
        healer.HairItemID = healer.Female ? 0x203C : 0x203B;
        healer.HairHue = 0x455;
        healer.MoveToWorld(location, Map.Trammel);
    }

    internal static bool FindLocation(Point3D preferred, out Point3D location, int maxRadius = 5)
    {
        var map = Map.Trammel;
        for (var radius = 0; radius <= maxRadius; radius++)
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
    private DateTime _nextSpeech = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 75));
    [Constructible]
    public HavenBankHealer()
    {
        Name = "Elias Thorne";
        Title = "the healer - free resurrection";
        CantWalk = true;
    }

    public override bool CheckResurrect(Mobile m) => true;

    public override void OnThink()
    {
        base.OnThink();
        HavenServiceSpeech.TrySpeak(this, ref _nextSpeech, "Rest a moment, traveler. You are safe here.", "If your spirit needs a body, come closer. There is no charge.", "Silas can bring back what you left behind. I will help with the rest.");
    }

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
    private DateTime _nextSpeech = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(50, 100));
    [Constructible]
    public HavenCorpseSummoner() : base(AIType.AI_Animal)
    {
        FightMode = FightMode.None;
        Name = "Silas Grey";
        Title = "the spirit guide - free corpse recovery";
        Body = 0x190;
        Hue = 0x83EA;
        CantWalk = true;
        Blessed = true;
        AddItem(new Robe(0x482));
        AddItem(new Sandals());
    }

    public override void OnThink()
    {
        base.OnThink();
        HavenServiceSpeech.TrySpeak(this, ref _nextSpeech, "Lost your way back to your body? I can help.", "Elias handles the living. I keep an eye on what they leave behind.", "The spirits travel light. Adventurers rarely do.");
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
