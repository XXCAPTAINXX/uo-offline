using System;
using Server.Items;
using Server.Targeting;

namespace Server.UOOffline;

public partial class HavenCompanion
{
    internal bool EquipFromOwner(Mobile from, Item item)
    {
        if (from != BoundOwner || Deleted || IsDeadPet || Map != from.Map || !from.InRange(this, 3) ||
            item?.Deleted != false || item is not (BaseWeapon or BaseArmor or BaseClothing or BaseJewel or BaseTalisman or Spellbook) ||
            !(from.Backpack != null && item.IsChildOf(from.Backpack) || Backpack != null && item.IsChildOf(Backpack))) { return false; }
        if (!EquipSafely(item)) { from.SendMessage("Your companion cannot equip that item, or their pack is full."); return false; }        from.SendMessage($"Your companion equipped {item.Name ?? item.DefaultName}. Replaced gear is in their pack.");
        return true;
    }

    private bool EquipSafely(Item item)
    {
        var old = FindItemOnLayer(item.Layer);
        Item other = null;
        if (item is BaseWeapon && item.Layer == Layer.TwoHanded) { other = FindItemOnLayer(Layer.OneHanded); }
        else if (item.Layer == Layer.OneHanded && FindItemOnLayer(Layer.TwoHanded) is BaseWeapon weapon) { other = weapon; }
        var needed = (old == null ? 0 : 1) + (other == null ? 0 : 1);
        if (Backpack.TotalItems + needed > Backpack.MaxItems ||
            old != null && !Backpack.CheckHold(this, old, false) || other != null && !Backpack.CheckHold(this, other, false)) { return false; }
        if (old != null) { Backpack.DropItem(old); }
        if (other != null) { Backpack.DropItem(other); }
        if (!EquipItem(item))
        {
            if (old != null) { EquipItem(old); }
            if (other != null) { EquipItem(other); }
            return false;
        }
        if (old != null) { old.Movable = true; }
        if (other != null) { other.Movable = true; }
        return true;
    }

    private HavenCompanionRole? _combatRole;
    private DateTime _nextEquipmentCheck;
    private Serial _lastMelee;
    private Serial _lastRanged;
    private Serial _lastSpellbook;
    internal void ConfigureCombatRole(DateTime? equipmentTime = null)
    {
        if (Deleted || IsDeadPet || Backpack == null || Expedition != null) { return; }
        var changed = _combatRole != Role;
        RememberCombatArms();
        var now = equipmentTime ?? Core.Now;
        if (!changed && now < _nextEquipmentCheck) { return; }
        _nextEquipmentCheck = now + TimeSpan.FromSeconds(2);
        if (changed)
        {
            _combatRole = Role;
            AI = Role == HavenCompanionRole.Archer ? Server.Mobiles.AIType.AI_Archer : Server.Mobiles.AIType.AI_Melee;
            RangeFight = Role is HavenCompanionRole.Archer or HavenCompanionRole.Caster ? 6 : 1;
        }
        if (Role == HavenCompanionRole.Archer)
        {
            if (Weapon is not BaseRanged)
            {
                var bow = StoredArm<BaseRanged>(_lastRanged) ?? Backpack.FindItemByType<BaseRanged>();
                if (bow == null) { bow = new HavenCompanionBow(); Backpack.DropItem(bow); }
                EquipSafely(bow);
            }
            if (Backpack.FindItemByType<Arrow>() == null) { Backpack.DropItem(new Arrow(1) { Movable = false }); }
        }
        else if (Role == HavenCompanionRole.Caster)
        {
            if (FindItemOnLayer(Layer.OneHanded) is not Spellbook)
            {
                var book = StoredArm<Spellbook>(_lastSpellbook);
                if (book == null)
                {
                    foreach (var candidate in Backpack.FindItemsByType<Spellbook>())
                    { if (candidate.CanEquip(this)) { book = candidate; break; } }
                }
                if (book == null) { book = new ApprenticeGrimoire { BoundTo = this }; Backpack.DropItem(book); }
                EquipSafely(book);
            }
        }
        else if (Weapon is BaseRanged || FindItemOnLayer(Layer.OneHanded) is Spellbook ||
                 FindItemOnLayer(Layer.OneHanded) is not BaseWeapon && FindItemOnLayer(Layer.TwoHanded) is not BaseWeapon)
        {
            var melee = StoredArm<BaseWeapon>(_lastMelee);
            if (melee == null)
            {
                foreach (var candidate in Backpack.FindItemsByType<BaseWeapon>())
                {
                    if (candidate is not BaseRanged) { melee = candidate; break; }
                }
            }
            if (melee == null) { melee = new HavenCompanionBlade(); Backpack.DropItem(melee); }
            EquipSafely(melee);
        }
        RememberCombatArms();
    }
    private T StoredArm<T>(Serial serial) where T : Item =>
        World.FindItem(serial) is T item && !item.Deleted && item.IsChildOf(Backpack) ? item : null;
    private void RememberCombatArms()
    {
        RememberCombatArm(FindItemOnLayer(Layer.OneHanded));
        RememberCombatArm(FindItemOnLayer(Layer.TwoHanded));
    }
    private void RememberCombatArm(Item item)
    {
        switch (item)
        {
            case BaseRanged ranged: _lastRanged = ranged.Serial; break;
            case BaseWeapon melee: _lastMelee = melee.Serial; break;
            case Spellbook book: _lastSpellbook = book.Serial; break;
        }
    }
    internal void RequestEquipment(Mobile from)
    {
        if (from != BoundOwner) { return; }
        from.SendMessage("Choose equipment from your backpack or your companion's shared pack.");
        from.Target = new EquipmentTarget(this);
    }

    private sealed class EquipmentTarget : Target
    {
        private readonly HavenCompanion _companion;
        public EquipmentTarget(HavenCompanion companion) : base(3, false, TargetFlags.None) => _companion = companion;
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item item && !_companion.EquipFromOwner(from, item))
            {
                from.SendMessage("Stand within 3 tiles and choose gear from your pack or your companion's pack.");
            }
            HavenCompanionGump.DisplayTo(from, _companion, 3);
        }
    }
}
