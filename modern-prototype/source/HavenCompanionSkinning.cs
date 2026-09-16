using System;
using System.Linq;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Engines.PartySystem;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        private DateTime _nextSkinning;
        internal bool AutoSkinning
        {
            get { var a=BoundOwner==null?null:BoundOwner.Account as Account;return a==null||a.GetTag("HavenSkinning:"+BoundOwner.Serial)!="off"; }
            set { var a=BoundOwner==null?null:BoundOwner.Account as Account;if(a!=null)a.SetTag("HavenSkinning:"+BoundOwner.Serial,value?"on":"off"); }
        }
        private static bool SkinningCombat(Mobile m) { var target=m.Combatant as Mobile;return target!=null&&!target.Deleted&&target.Alive; }
        internal bool CanSkinWhileHunting()
        {
            return HavenPreview.Enabled&&!Deleted&&Alive&&!IsDeadPet&&!IsStabled&&!OnMission&&!TamingAssistActive&&!ArmoryHelpActive&&
                Controlled&&ControlMaster==BoundOwner&&AutoSkinning&&BoundOwner!=null&&!BoundOwner.Deleted&&BoundOwner.Alive&&
                Map!=null&&Map!=Map.Internal&&BoundOwner.Map==Map&&InRange(BoundOwner,12)&&
                (ControlOrder==OrderType.Follow||ControlOrder==OrderType.Guard)&&!SkinningCombat(this)&&!SkinningCombat(BoundOwner)&&
                Hits==HitsMax&&BoundOwner.Hits==BoundOwner.HitsMax&&!Poisoned&&!BoundOwner.Poisoned&&Spell==null;
        }
        private bool HuntingParticipant(Mobile m)
        {
            var creature=m as BaseCreature;if(creature!=null)m=creature.GetMaster()??m;
            var party=Server.Engines.PartySystem.Party.Get(BoundOwner);
            return m!=null&&(m==BoundOwner||m==this||(party!=null&&party.Contains(m)));
        }
        internal bool CanSkinCorpse(Corpse corpse)
        {
            if(corpse==null||corpse.Deleted||corpse.Animated||corpse.Map!=Map||BoundOwner==null)return false;
            var animal=corpse.Owner as BaseCreature;
            if(animal==null||(animal.Hides<=0&&animal.Meat<=0&&animal.Feathers<=0&&animal.Scales<=0&&!(animal is WildTiger))||animal.Controlled||animal.Summoned||animal.IsBonded||animal.Body.IsHuman||corpse.IsCriminalAction(BoundOwner))return false;
            if(!HuntingParticipant(corpse.Killer)&&!corpse.Aggressors.Any(HuntingParticipant))return false;
            return !corpse.Carved||corpse.Items.Any(i=>i.Movable);
        }
        internal bool SkinCorpse(Corpse corpse)
        {
            if(!CanSkinWhileHunting()||!CanSkinCorpse(corpse)||!InRange(corpse,2)||!InLOS(corpse)||Backpack==null)return false;
            if(!corpse.Carved)
            {
                var knife=new SkinningKnife();
                try { ((BaseCreature)corpse.Owner).OnCarve(BoundOwner,corpse,knife); }
                finally { knife.Delete(); }
            }
            bool gathered=false;var scissors=new Scissors();
            try
            {
                foreach(var item in corpse.Items.ToArray())
                {
                    if(!item.Movable)continue;
                    if(!Backpack.TryDropItem(this,item,false))continue;
                    var hides=item is BaseHides?item as IScissorable:null;if(hides!=null)hides.Scissor(this,scissors);
                    gathered=true;
                }
            }
            finally { scissors.Delete(); }
            if(gathered){var ledger=EnsureResourceLedger();if(ledger!=null)foreach(var resource in Backpack.Items.ToArray())ledger.AbsorbCarriedResource(this,resource);PlaySound(0x248);}
            return gathered;
        }
        private void ThinkSkinning()
        {
            if(BoundOwner==null||BoundOwner.NetState==null||DateTime.UtcNow<_nextSkinning||!CanSkinWhileHunting())return;
            _nextSkinning=DateTime.UtcNow.AddSeconds(1);
            if(BoundOwner.Aggressors.Any(a=>!a.Expired&&a.Attacker!=null&&!a.Attacker.Deleted&&a.Attacker.Alive&&a.Attacker.Map==Map&&InRange(a.Attacker,12)))return;
            var items=Map.GetItemsInRange(Location,2);
            try { foreach(Item item in items){var corpse=item as Corpse;if(corpse!=null&&SkinCorpse(corpse))break;} }
            finally { items.Free(); }
        }
    }
}
