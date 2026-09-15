using Server.Commands;
using Server.Network;
using System;
using System.Buffers;
using System.Buffers.Binary;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Targeting;

namespace Server.UOOffline;

public static class HavenPetHotkeys
{
    public static void Initialize()
    {
        Register("PetFollow", "all follow me", 0x16C);
        Register("PetGuard", "all guard me", 0x16B);
        Register("PetStay", "all stay", 0x170);
        Register("PetStop", "all stop", 0x167);
        Register("PetAttack", "all kill", 0x168);
        Register("PetCome", "all come", 0x164);
        CommandSystem.Register("PetMenu", AccessLevel.Player, e =>
        { e.Mobile.SendMessage("Select your pet or its health bar to open its menu."); e.Mobile.Target = new MenuTarget(); });
    }
    private sealed class MenuTarget : Target
    {
        public MenuTarget() : base(18, false, TargetFlags.None) { }
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCreature pet || pet.ControlMaster != from || !pet.Controlled || from.NetState == null) { return; }
            Span<byte> data = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(data, pet.Serial.Value);
            ContextMenuSystem.ContextMenuRequest(from.NetState, new SpanReader(data));
        }
    }

    private static void Register(string command, string speech, int keyword)
    {
        CommandSystem.Register(command, AccessLevel.Player, e =>
        {
            if (e.Mobile.Alive)
            {
                // Use normal speech processing so range, ownership, obedience and targeting still apply.
                e.Mobile.DoSpeech(speech, new[] { keyword }, MessageType.Regular, e.Mobile.SpeechHue);
            }
        });
    }
}
