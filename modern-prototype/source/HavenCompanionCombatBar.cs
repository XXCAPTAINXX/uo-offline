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
    public class CompanionCombatBarGump:HavenMenuGump
    {
        private readonly HavenCompanion _companion;
        public static void Initialize() {CommandSystem.Register("cc",AccessLevel.Player,e=>{
            if(!HavenPreview.Enabled)return;var companion=HavenCompanion.Claim(e.Mobile);if(companion!=null)companion.ShowCombatBar(e.Mobile);
        });}
        public CompanionCombatBarGump(HavenCompanion companion):base(35,160)
        {
            _companion=companion;
            Closable=false;
            AddBackground(0,0,440,124,0xA28);
            AddLabelCropped(20,12,210,24,0,companion.Name);
            Button(240,12,7,"Menu");
            Button(340,12,9,"Close");
            Button(20,48,1,"Follow");Button(125,48,2,"Guard");
            Button(230,48,4,"Stay");Button(335,48,3,"Attack");
            Button(20,86,5,"Heal");
            Button(125,86,10,companion.TamingAssistActive?"Cancel":"Tame");
            Button(230,86,6,"Recall");Button(335,86,8,"Pack");

        }
        private void Button(int x,int y,int id,string label) {AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);AddLabelCropped(x+34,y,70,24,0,label);}
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
                case 7:_companion.ShowCombatBar(owner);_companion.Show(owner,true);return;
                case 9:return;
                case 10:
                    if(!_companion.CanCommand(owner)){ok=false;break;}
                    if(!_companion.TamingAssistActive&&_companion.Role!=CompanionRole.Bard)
                    {
                        if(!_companion.SetRole(owner,CompanionRole.Bard)){owner.SendMessage("Finish combat and stand nearby to switch to Bard for taming.");break;}
                        owner.SendMessage("Switched to Bard for taming assistance.");
                    }
                    _companion.RequestTamingAssist(owner);break;
                case 8:_companion.OpenPack(owner);break;
                default:return;
            }
            if(!ok)owner.SendMessage("Command unavailable: check distance, health, casting or mission status. Recall requires leaving combat.");
            _companion.ShowCombatBar(owner);
        }
    }
}
