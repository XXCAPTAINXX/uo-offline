using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        public void ShowCombatBar(Mobile owner)
        {
            if(!IsOwner(owner))return;
            owner.CloseGump(typeof(CompanionGump));
            owner.CloseGump(typeof(CompanionActivityGump));
            owner.CloseGump(typeof(CompanionResourceMissionGump));
            owner.CloseGump(typeof(CompanionCombatBarGump));
            owner.SendGump(new CompanionCombatBarGump(this));
        }
    }
    public class CompanionCombatBarGump:Gump
    {
        private readonly HavenCompanion _companion;
        public static void Initialize() {CommandSystem.Register("cc",AccessLevel.Player,e=>{
            if(!HavenPreview.Enabled)return;var companion=HavenCompanion.Claim(e.Mobile);if(companion!=null)companion.ShowCombatBar(e.Mobile);
        });}
        public CompanionCombatBarGump(HavenCompanion companion):base(35,160)
        {
            _companion=companion;
            AddBackground(0,0,420,104,0xA28);
            AddLabel(12,9,0,companion.Name);
            Button(320,8,7,"Menu");
            Button(12,38,1,"Follow");Button(120,38,2,"Guard");Button(230,38,3,"Attack");
            Button(12,70,4,"Stay");Button(120,70,5,"Heal");Button(230,70,6,"Recall");Button(320,70,8,"Pack");
        }
        private void Button(int x,int y,int id,string label) {AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);AddLabel(x+34,y,0,label);}
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var owner=sender.Mobile;if(!_companion.IsOwner(owner) || info.ButtonID==0)return;
            bool ok=true;
            switch(info.ButtonID) {
                case 1:ok=_companion.SetOrder(owner,OrderType.Follow);break;
                case 2:ok=_companion.SetOrder(owner,OrderType.Guard);break;
                case 3:if(_companion.CanCommand(owner))owner.Target=new HavenCompanion.AttackTarget(_companion);else ok=false;break;
                case 4:ok=_companion.SetOrder(owner,OrderType.Stay);break;
                case 5:ok=_companion.HealOwner(owner);break;
                case 6:ok=_companion.Recall(owner);break;
                case 7:_companion.Show(owner,true);return;
                case 8:_companion.OpenPack(owner);break;
                default:return;
            }
            if(!ok)owner.SendMessage("Command unavailable: check distance, health, casting or mission status. Recall requires leaving combat.");
            _companion.ShowCombatBar(owner);
        }
    }
}
