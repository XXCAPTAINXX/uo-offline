using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenTrialParticipants : Item
{
    [SerializableField(0)] private List<PlayerMobile> _players = new();
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenTrialParticipants() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "trial participants"; }
    internal static HavenTrialParticipants Get(HavenIslandTrial trial)
    {
        foreach (var item in trial.Items) { if (item is HavenTrialParticipants record) { return record; } }
        var created = new HavenTrialParticipants(); trial.AddItem(created); return created;
    }
    private void Add(HavenIslandTrial trial, Mobile mobile)
    {
        if (mobile is PlayerMobile player && player is not Server.CustomBots.PlayerBot && !player.Deleted &&
            player.Map == trial.Map && player.InRange(trial, 24) && !Players.Contains(player))
        { Players.Add(player); this.MarkDirty(); }
    }
    private void AddGroup(HavenIslandTrial trial, Mobile mobile)
    {
        if (mobile?.Deleted != false) { return; }
        Add(trial, mobile);
        var party = Party.Get(mobile);
        if (party == null) { return; }
        foreach (var member in party.Members) { Add(trial, member.Mobile); }
    }
    internal void Record(HavenIslandTrial trial, BaseCreature creature)
    {
        foreach (var right in BaseCreature.GetLootingRights(creature.DamageEntries, creature.HitsMax))
        { if (right.m_Damage > 0) { AddGroup(trial, right.m_Mobile); } }
        foreach (var entry in creature.DamageEntries)
        {
            if (entry.DamageGiven <= 0 || entry.HasExpired) { continue; }
            if (entry.Damager is HavenCompanion companion) { AddGroup(trial, companion.BoundOwner); }
            else if (entry.Damager is BaseCreature pet && pet.ControlMaster != null) { AddGroup(trial, pet.ControlMaster); }
        }
    }
    internal void Clear() { Players.Clear(); this.MarkDirty(); }
    internal void Award(int theme)
    {
        var recipients = Players.ToArray(); Clear();
        foreach (var player in recipients)
        {
            if (player.Deleted) { continue; }
            var bag = new Bag { Name = "island trial rewards" };
            bag.DropItem(new Gold(Utility.RandomMinMax(25000, 40000)));
            bag.DropItem(new HavenMark(20)); bag.DropItem(new AstralShard(5));
            for (var i = 0; i < 5; i++) { bag.DropItem(PowerScroll.CreateRandomNoCraft(5, 10)); }
            bag.DropItem(new ScrollofAlacrity(SkillsInfo.RandomSkill()));
            bag.DropItem(ScrollofTranscendence.CreateRandom(5, 20));
            bag.DropItem(HavenTrialTheme.ResourceDeed(theme, true));
            if (player.Backpack == null) { player.AddItem(new Backpack()); }
            // Earned event rewards must not disappear or spill onto the ground when a pack is full.
            foreach (var item in bag.Items.ToArray()) { player.Backpack.DropItem(item); }
            bag.Delete();
            player.Backpack.FindItemByType<AdventurersWallet>()?.DepositBackpackShards(player);
            player.SendMessage("Trial complete! Gold, five power scrolls, Alacrity, Transcendence, 20 Haven marks, five Astral shards and crafting materials are in your pack. Shards enter your wallet when present.");
        }
    }
    public override void OnAfterDelete() { Players.Clear(); base.OnAfterDelete(); }
}
