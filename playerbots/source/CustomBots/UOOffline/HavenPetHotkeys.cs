using Server.Commands;
using Server.Network;

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
