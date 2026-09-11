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
            Closable=false;
            AddBackground(0,0,350,112,3600);
            AddAlphaRegion(0,0,350,112);
            AddLabelCropped(12,8,165,20,53,companion.Name);
            Button(184,8,7,"Menu");Button(266,8,9,"Close");
            Button(12,39,1,"Follow");Button(94,39,2,"Guard");
            Button(176,39,4,"Stay");Button(258,39,3,"Attack");
            Button(12,69,5,"Heal");Button(94,69,10,companion.TamingAssistActive?"Cancel":"Tame");
            Button(176,69,6,"Recall");Button(258,69,8,"Pack");
        }
        private void Button(int x,int y,int id,string label)
        {
            // Four adjoining native hit areas make the whole labeled rectangle clickable.
            for(int offset=0;offset<76;offset+=19)AddButton(x+offset,y,210,210,id,GumpButtonType.Reply,0);
            AddImageTiled(x,y,76,19,5058);
            bool selected=(id==1&&_companion.ControlOrder==OrderType.Follow)||(id==2&&_companion.ControlOrder==OrderType.Guard)||(id==4&&_companion.ControlOrder==OrderType.Stay)||(id==10&&_companion.TamingAssistActive);
            AddHtml(x,y+1,76,18,"<CENTER><BASEFONT COLOR="+(selected?"#FFE399":"#FFFFFF")+">"+label+"</BASEFONT></CENTER>",false,false);
        }

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
