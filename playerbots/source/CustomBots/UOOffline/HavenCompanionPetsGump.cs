using System;
using System.Linq;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

public sealed class HavenCompanionPetsGump : Gump
{
    private readonly HavenCompanion _companion;
    private readonly HavenCompanionAssignedPet[] _pets;
    private readonly int _page;
    public static void Initialize() => CommandSystem.Register("CompanionPets",AccessLevel.Player,e=>DisplayTo(e.Mobile,HavenCompanionGearAssignment.Find(e.Mobile)));
    internal static bool CanUse(Mobile owner,HavenCompanion companion) => owner?.Deleted==false && owner.Alive &&
        companion?.Deleted==false && companion.BoundOwner==owner && companion.Map==owner.Map && owner.InRange(companion,12);
    public static void DisplayTo(Mobile owner,HavenCompanion companion,int page=0)
    {
        if(!CanUse(owner,companion)) { owner?.SendMessage("Your companion must be nearby to manage its pets.");return; }
        owner.CloseGump<HavenCompanionPetsGump>();owner.SendGump(new HavenCompanionPetsGump(companion,page));
    }
    public HavenCompanionPetsGump(HavenCompanion companion,int page=0) : base(50,50)
    {
        _companion=companion;
        var all=companion.Backpack.Items.OfType<HavenCompanionAssignedPet>().Where(p=>!p.Deleted && p.Pet?.Deleted==false).ToArray();
        _page=Math.Clamp(page,0,Math.Max(0,(all.Length-1)/5));_pets=all.Skip(_page*5).Take(5).ToArray();
        AddBackground(0,0,620,505,9270);AddBackground(10,10,600,485,3000);
        AddLabel(25,25,0,"Companion pets");
        AddLabel(25,55,0,$"Follower slots: {companion.Followers} / {companion.FollowersMax}");
        AddHtml(25,85,565,60,"<BASEFONT COLOR=#181818>Assign a pet you control, or use its Transfer command on your companion. Stand within 3 tiles, out of combat. Reclaimed pets become tickets in the companion pack.</BASEFONT>",false,false);
        AddButton(25,150,4005,4007,1);AddLabel(65,152,0,"Assign my pet...");
        for(var i=0;i<_pets.Length;i++)
        {
            var entry=_pets[i];var y=195+i*48;
            AddLabelCropped(25,y,265,20,0,entry.Pet.Name ?? "Pet");
            AddButton(300,y,4005,4007,100+i*2);AddLabel(340,y+2,0,entry.AutoMount ? "Ride: on" : "Ride: off");
            AddButton(455,y,4005,4007,101+i*2);AddLabel(495,y+2,0,"Reclaim");
            AddLabel(25,y+21,0,entry.Pet.IsDeadPet ? "Needs resurrection" : entry.Pet is BaseMount { Rider: not null } ? "Mounted" : entry.Parked ? "Waiting for mission return" : "Following / fighting");
        }
        if(all.Length==0) { AddLabel(25,210,0,"No pets assigned. Taming claims are stored separately in the pack."); }
        AddButton(25,465,4014,4016,2);AddLabel(65,467,0,"Previous");AddLabel(230,467,0,$"Page {_page+1}");
        AddButton(350,465,4005,4007,3);AddLabel(390,467,0,"Next");AddButton(510,465,4017,4019,0);AddLabel(545,467,0,"Close");
    }
    public override void OnResponse(NetState sender,in RelayInfo info)
    {
        var owner=sender.Mobile;if(info.ButtonID==0 || !CanUse(owner,_companion)) { return; }
        if(info.ButtonID==1) { owner.Target=new AssignTarget(_companion);owner.SendMessage("Target a living pet you control, standing close to your companion.");return; }
        if(info.ButtonID==2 || info.ButtonID==3) { DisplayTo(owner,_companion,_page+(info.ButtonID==2 ? -1 : 1));return; }
        var index=(info.ButtonID-100)/2;
        if(info.ButtonID>=100 && index<_pets.Length)
        {
            var pet=_pets[index];
            if(!pet.Deleted && pet.Parent==_companion.Backpack && pet.Owner==owner && pet.Companion==_companion && pet.Pet?.ControlMaster==_companion)
            {
                if((info.ButtonID-100)%2==0) { pet.AutoMount=!pet.AutoMount;pet.Tick(); }
                else if(!pet.ClaimBack(owner)) { owner.SendMessage("The pet cannot be reclaimed yet; resurrect it and ensure there is room in the pack."); }
            }
        }
        DisplayTo(owner,_companion,_page);
    }
    private sealed class AssignTarget(HavenCompanion companion) : Target(3,false,TargetFlags.None)
    {
        protected override void OnTarget(Mobile from,object targeted)
        {
            if(!CanUse(from,companion) || targeted is not BaseCreature pet || !companion.AcceptAssignedPet(from,pet))
            { from.SendMessage("Assignment refused. Check ownership, distance, combat, follower space, and companion Taming/Lore."); }
            DisplayTo(from,companion);
        }
    }
}
