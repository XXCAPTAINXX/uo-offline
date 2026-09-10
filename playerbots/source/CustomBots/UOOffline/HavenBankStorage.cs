using System;
using Server.CustomBots;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public static class HavenBankStorage
{
    public const int ItemLimit = 2000;

    internal static void Expand(Mobile mobile)
    {
        if (mobile is not PlayerMobile { Deleted: false } || mobile is PlayerBot) { return; }
        var bank = mobile.BankBox;
        // Preserve explicitly unlimited or larger banks; update the existing container in place.
        if (bank.MaxItems > 0 && bank.MaxItems < ItemLimit) { bank.MaxItems = ItemLimit; }
    }

    public static void Initialize()
    {
        EventSink.Connected += Expand;
        Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
        {
            foreach (var state in NetState.Instances) { Expand(state.Mobile); }
        });
    }
}
