using System;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenChampionCorpseCleanup
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);
        public static bool Eligible(Corpse corpse)
        {
            var creature = corpse == null ? null : corpse.Owner as BaseCreature;
            return corpse != null && !corpse.Deleted && creature != null && !creature.Player &&
                creature.IsChampionSpawn && !(creature is BaseChampion) &&
                !creature.Controlled && !creature.Summoned && !creature.IsBonded;
        }
        public static void Schedule(Corpse corpse)
        {
            if (!Eligible(corpse)) return;
            // Independent of the spawn controller's later BeginDecay call.
            Timer.DelayCall(Lifetime, () => { if (Eligible(corpse)) corpse.Delete(); });
        }
        public static void Initialize()
        {
            EventSink.CreatureDeath += e => { if (HavenPreview.Enabled) Schedule(e.Corpse as Corpse); };
            EventSink.ServerStarted += () => {
                if (HavenPreview.Enabled)
                    foreach (var corpse in World.Items.Values.OfType<Corpse>().ToArray()) Schedule(corpse);
            };
        }
    }
}
