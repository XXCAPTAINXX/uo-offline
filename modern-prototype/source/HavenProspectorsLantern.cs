using System;
using System.Globalization;
using Server.Accounting;
using Server.Commands;
using Server.Engines.UOStore;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public class HavenProspectorsLantern : Item
    {
        const string Key = "Haven.Prospector.Expires";
        public static void Initialize()
        {
            EventSink.ServerStarted += () => { if (HavenPreview.Enabled) UltimaStore.Register<HavenProspectorsLantern>(new TextDefinition[] { "Prospector's Lantern", "1 hour: +25% custom legendary drop chance" }, 0, 0xA25, 0, 0x8A5, 100, StoreCategory.Misc); };
            CommandSystem.Register("fortune", AccessLevel.Player, e => {
                var remaining = Remaining(e.Mobile);
                e.Mobile.SendMessage(remaining > TimeSpan.Zero ? "Prospector's Lantern: +25% custom legendary drop chance, " + Math.Ceiling(remaining.TotalMinutes) + " minutes remaining." : "No Prospector's Lantern is active. Available in the UO Store for 100 Sovereigns.");
            });
        }
        public static TimeSpan Remaining(Mobile from)
        {
            var account = from == null ? null : from.Account as Account; DateTime expires;
            if (account == null || !DateTime.TryParse(account.GetTag(Key), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out expires)) return TimeSpan.Zero;
            var remaining = expires.ToUniversalTime() - DateTime.UtcNow; return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        public static double Multiplier(Mobile from) { return HavenPreview.Enabled && Remaining(from) > TimeSpan.Zero ? 1.25 : 1; }
        [Constructable] public HavenProspectorsLantern() : base(0xA25) { Name = "Prospector's Lantern"; Hue = 0x8A5; Weight = 1; LootType = LootType.Blessed; }
        public HavenProspectorsLantern(Serial serial) : base(serial) { }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list); list.Add("Consume: 1 hour of +25% custom legendary world-drop chance.");
            list.Add("Account-wide. Real time, including offline. Does not stack.");
            list.Add("Does not affect dungeon artifacts, pet rarity or item strength. [fortune shows time remaining.");
        }
        public override void OnDoubleClick(Mobile from)
        {
            var account = from == null ? null : from.Account as Account;
            if (!HavenPreview.Enabled || Deleted || account == null || !from.Alive || !IsChildOf(from.Backpack)) return;
            if (Remaining(from) > TimeSpan.Zero) { from.SendMessage("A lantern is already active. This one was not consumed. Use [fortune to check its remaining time."); return; }
            account.SetTag(Key, DateTime.UtcNow.AddHours(1).ToString("O", CultureInfo.InvariantCulture));
            Delete(); from.SendMessage(0x48D, "Prospector's Lantern activated: +25% custom legendary world-drop chance for one hour. Use [fortune for remaining time.");
        }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
