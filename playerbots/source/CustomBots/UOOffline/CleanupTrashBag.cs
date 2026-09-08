using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class CleanupTrashBag : Bag
{
    private Timer _timer;

    [SerializableField(0)]
    [InvalidateProperties]
    private int _cleanupPoints;

    public override string DefaultName => "Britannia cleanup trash bag";
    public override int DefaultMaxWeight => 0;

    [Constructible]
    public CleanupTrashBag()
    {
        Hue = 0x455;
        LootType = LootType.Blessed;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (Items.Count > 0)
        {
            StartEmptyTimer();
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Keep the cleanup bag in your backpack while using it.");
            return false;
        }

        if (!base.OnDragDrop(from, dropped))
        {
            return false;
        }

        from.SendMessage("The item will be cleaned up in three minutes.");
        StartEmptyTimer();
        return true;
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Keep the cleanup bag in your backpack while using it.");
            return false;
        }

        if (!base.OnDragDropInto(from, item, p))
        {
            return false;
        }

        from.SendMessage("The item will be cleaned up in three minutes.");
        StartEmptyTimer();
        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        base.OnDoubleClick(from);

        if (Items.Count > 0)
        {
            from.SendMessage($"{Items.Count} cleanup item(s) are waiting in this bag.");
        }
    }

    private void StartEmptyTimer()
    {
        _timer?.Stop();
        _timer = new EmptyTimer(this);
        _timer.Start();
    }

    public void EmptyNow()
    {
        var count = Items.Count;

        for (var i = Items.Count - 1; i >= 0; --i)
        {
            if (i < Items.Count)
            {
                Items[i].Delete();
            }
        }

        if (count > 0)
        {
            CleanupPoints += count;
            (RootParent as Mobile)?.SendMessage(
                $"Cleanup complete. {count} item(s) removed; total cleanup points: {CleanupPoints}."
            );
        }

        _timer?.Stop();
        _timer = null;
        InvalidateProperties();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Cleanup points: {CleanupPoints}");
        list.Add("Contents are deleted after three minutes");
    }

    public override void OnAfterDelete()
    {
        _timer?.Stop();
        _timer = null;
        base.OnAfterDelete();
    }

    private class EmptyTimer : Timer
    {
        private readonly CleanupTrashBag _bag;

        public EmptyTimer(CleanupTrashBag bag) : base(TimeSpan.FromMinutes(3.0)) => _bag = bag;

        protected override void OnTick() => _bag.EmptyNow();
    }
}
