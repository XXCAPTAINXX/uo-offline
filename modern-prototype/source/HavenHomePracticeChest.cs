using System;
using Server.Items;
using Server.Multis;
using Server.SkillHandlers;

namespace Server.HavenPrototype
{
    public class HavenHomePracticeChest : WoodenChest, IRemoveTrapTrainingKit
    {
        public HavenHomePracticeChest() { Name = "Lockpicking and Remove Trap trainer"; Movable = false; Locked = true; }
        public HavenHomePracticeChest(Serial serial) : base(serial) { }
        private bool CanPractice(Mobile from)
        {
            var house = BaseHouse.FindHouseAt(this);
            return from.Alive && from.Map == Map && from.InRange(this, 2) && from.InLOS(this) && house != null && house.IsOwner(from);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!CanPractice(from)) return;
            int skill = (int)from.Skills.Lockpicking.Value;
            RequiredSkill = Math.Max(0, skill - 10);
            LockLevel = skill - 20;
            MaxLockLevel = skill + 20;
            Locked = true;
            from.SendMessage("Practice lock reset to your skill. Use lockpicks here, or use Remove Trap and target this chest. The practice trap deals no damage.");
        }
        public override void LockPick(Mobile from)
        {
            if (!CanPractice(from)) return;
            base.LockPick(from);
            from.SendMessage("Practice lock opened. Double-click the trainer to reset it.");
        }
        public void OnRemoveTrap(Mobile from)
        {
            if (!CanPractice(from)) return;
            double skill = from.Skills.RemoveTrap.Value;
            bool success = from.CheckTargetSkill(SkillName.RemoveTrap, this, skill - 20, skill + 20);
            from.SendMessage(success ? "You disarm the practice mechanism. It resets safely for your next attempt." : "The practice mechanism clicks. Try again; it cannot hurt you.");
        }
        public override bool OnDragDrop(Mobile from, Item dropped) { return false; }
        public override bool OnDragDropInto(Mobile from, Item item, Point3D point) { return false; }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
