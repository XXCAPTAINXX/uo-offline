using System;
using System.IO;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Network;

namespace Server.HavenPrototype
{
    public static class HavenPlaytest
    {
        // Stable IDs: append tests; do not reorder existing entries with saved results.
        public static readonly string[] Titles={"Starter supplies","Arcane supplies","Companion orders","Companion combat","Companion pack","Five-minute mining mission","Ledger transfer","Ledger withdrawal","Repair stone","Dungeon travel","Logout and return","Menus and readability"};
        public static readonly string[] Details={"Claim starter gear; skills stay unchanged; a second claim is refused.","Claim books and robe; equip them and try a spell you can cast.","Recruit with the stone; try follow, guard, all stay and all kill.","Outside combat choose a role, then fight an ordinary hostile.","Beside Alden, put an item in his pack and take it back out.","Send Mining for 5 minutes, then Recall: 100 ingots and 500 gold.","Transfer Alden's ledger balance to yours; check both totals.","Withdraw some resources, then absorb them again; totals agree.","Carry a damaged weapon or armor piece; repair restores durability.","Use the travel stone; visit a destination and return to Haven.","Log out/in; verify your character, companion and possessions.","Check text, buttons, clipping and whether services are understandable."};
        public static void Initialize() { CommandSystem.Register("haventest",AccessLevel.Player,e=>{if(HavenPreview.Enabled) e.Mobile.SendGump(new HavenPlaytestGump(e.Mobile,0));}); }
        private static string Key(Mobile from,int test) { return "HavenPlaytest:"+from.Serial.Value+":"+test; }
        public static int Status(Mobile from,int test)
        {
            var account=from==null?null:from.Account as Account; int value;
            return account!=null && test>=0 && test<Titles.Length && int.TryParse(account.GetTag(Key(from,test)),out value) && value>=0 && value<=3 ? value:0;
        }
        public static bool Record(Mobile from,int test,int status,string note)
        {
            var account=from==null?null:from.Account as Account;
            if(!HavenPreview.Enabled || account==null || !from.Player || test<0 || test>=Titles.Length || status<0 || status>3) return false;
            note=(note??"").Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');
            if(note.Length>240) note=note.Substring(0,240);
            // Local feedback only; no network transmission. Never place this log in Git.
            try { File.AppendAllText("haven-playtest-results.tsv",DateTime.UtcNow.ToString("O")+"\t"+from.Serial.Value+"\t"+test+"\t"+new[]{"Not tested","Pass","Fail","Blocked"}[status]+"\t"+Titles[test]+"\t"+note+Environment.NewLine); }
            catch(IOException) { from.SendMessage("Could not save feedback; please try again."); return false; }
            account.SetTag(Key(from,test),status.ToString());
            from.SendMessage("Saved: "+Titles[test]+" - "+new[]{"Not tested","Pass","Fail","Blocked"}[status]+". Your local feedback can now be reviewed.");
            return true;
        }
    }
    public class HavenPlaytestGump:Gump
    {
        private readonly int _page;
        public HavenPlaytestGump(Mobile from,int page):base(35,35)
        {
            _page=Math.Max(0,Math.Min(1,page));
            AddBackground(0,0,740,650,0xA28); AddLabel(24,20,0,"Haven playtest checklist - page "+(_page+1)+" / 2");
            AddHtml(24,50,690,48,"<BASEFONT COLOR=#202020>Each result saves immediately to this PC for review. Use Blocked if you cannot try a test. These are your observations, not automatic test results.</BASEFONT>",false,false);
            for(int row=0;row<6;row++)
            {
                int test=_page*6+row,y=112+row*57;
                AddLabel(24,y,0,HavenPlaytest.Titles[test]);
                AddLabel(265,y,0,new[]{"Not tested","PASS","FAIL","BLOCKED"}[HavenPlaytest.Status(from,test)]);
                Button(365,y,100+test*4+1,"Pass"); Button(465,y,100+test*4+2,"Fail"); Button(560,y,100+test*4+3,"Blocked");
                AddHtml(24,y+23,690,29,"<BASEFONT COLOR=#202020>"+HavenPlaytest.Details[test]+"</BASEFONT>",false,false);
            }
            AddLabel(24,469,0,"Optional note for the next result you click (up to 240 characters):");
            AddBackground(24,496,690,54,0xBB8); AddTextEntry(32,504,670,36,0,1,"");
            AddHtml(24,560,690,30,"<BASEFONT COLOR=#202020>Results stay with this character. I see them when I check the local log, not as an instant notification.</BASEFONT>",false,false);
            Button(24,604,1,_page==0?"Next page":"Previous page"); Button(610,604,0,"Close");
        }
        private void Button(int x,int y,int id,string label) { AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0); AddLabel(x+34,y,0,label); }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var from=sender.Mobile; if(!HavenPreview.Enabled || from==null || info.ButtonID==0) return;
            if(info.ButtonID==1) { from.SendGump(new HavenPlaytestGump(from,1-_page)); return; }
            int encoded=info.ButtonID-100,test=encoded/4,status=encoded%4;
            if(encoded>=0 && status>=1 && test/6==_page) HavenPlaytest.Record(from,test,status,info.GetTextEntry(1)?.Text);
            from.SendGump(new HavenPlaytestGump(from,_page));
        }
    }
}
