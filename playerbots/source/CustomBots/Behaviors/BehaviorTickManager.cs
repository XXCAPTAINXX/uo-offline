// =========================================================================
// BehaviorTickManager.cs — Global timer that ticks all PlayerBots.
//
// Wakes up every TickInterval and calls Behavior.Tick() on every active
// PlayerBot in the world. One timer for all bots — much cheaper than one
// timer per bot, and avoids ordering issues during world saves.
//
// Bots register on construction/load and unregister on deletion.
// The AI tick visits registered bots instead of scanning every world mobile.
// =========================================================================

using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Logging;

namespace Server.CustomBots
{
    public static class BehaviorTickManager
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(2);
        private static Timer _timer;
        private static readonly HashSet<PlayerBot> _registered = new();
        internal static int RegisteredCount => _registered.Count;
        internal static IEnumerable<PlayerBot> Registered => _registered;
        internal static void Register(PlayerBot bot) { if (!bot.Deleted) { _registered.Add(bot); } }
        internal static void Unregister(PlayerBot bot) { _registered.Remove(bot); Server.UOOffline.HavenDungeonCourtesy.Forget(bot); }

        // Reusable buffer for snapshot — saves on GC churn since we'd
        // otherwise allocate a new array every 2 seconds.
        private static readonly List<PlayerBot> _scratch = new();

        public static void Configure()
        {
            BehaviorRegistry.Configure();
            _timer = Timer.DelayCall(TickInterval, TickInterval, 0, OnTick);
        }

        private static void OnTick()
        {
            // Snapshot allows behaviors to create or delete bots during this pass.
            _scratch.Clear();
            foreach (var bot in _registered)
            {
                if (!bot.Deleted && bot.Map != Map.Internal) { _scratch.Add(bot); }
            }
            for (int i = 0; i < _scratch.Count; i++)
            {
                var bot = _scratch[i];
                // Re-check after snapshot — bot could have been deleted by
                // a prior iteration's side effect.
                if (bot.Deleted || bot.Map == Map.Internal)
                {
                    continue;
                }

                try
                {
                    // A real player may have invited this bot to a party
                    // since the last tick — answer before acting.
                    BotPlayerParty.CheckInvite(bot);
                    if (Server.UOOffline.HavenDungeonCourtesy.Tick(bot)) { continue; }
                    if (Server.UOOffline.HavenGuildCrew.Working(bot)) { continue; }
                    bot.Behavior?.Tick(bot);

                    // Judged from OUTSIDE the brain, after it has had its
                    // say: is this bot actually getting closer to wherever
                    // it claims to be going? Every other stuck detector
                    // belongs to one behavior and counts its own attempts,
                    // so a bot that keeps retrying resets them all and jams
                    // in silence. See BotNavWatch.
                    BotNavWatch.Observe(bot);
                }
                catch (Exception ex)
                {
                    LogFactory.GetLogger(typeof(BehaviorTickManager)).Error(ex, $"PlayerBot tick error on {bot.Name}");
                }
            }

            _scratch.Clear();  // release references so bots can be GC'd
        }
    }
}
