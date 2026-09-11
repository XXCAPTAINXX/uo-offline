using System.Linq;
using Server;
using Server.Guilds;
namespace Server.HavenPrototype {
 public class HavenGuildResourceLedger:HavenResourceLedger {
  int _guildId;
  public HavenGuildResourceLedger(BaseGuild guild) { _guildId=guild.Id; Name="Guild resource ledger"; Movable=false; Internalize(); }
  public HavenGuildResourceLedger(Serial serial):base(serial){}
  public static HavenGuildResourceLedger For(Mobile from) { var guild=from==null?null:from.Guild; if(guild==null||guild.Disbanded)return null; return World.Items.Values.OfType<HavenGuildResourceLedger>().FirstOrDefault(x=>!x.Deleted&&x._guildId==guild.Id)??new HavenGuildResourceLedger(guild); }
  public override bool CanUse(Mobile from) { return from!=null&&!from.Deleted&&from.Alive&&from.Guild!=null&&!from.Guild.Disbanded&&from.Guild.Id==_guildId; }
  public override void Serialize(GenericWriter writer){base.Serialize(writer);writer.Write(0);writer.Write(_guildId);}
  public override void Deserialize(GenericReader reader){base.Deserialize(reader);reader.ReadInt();_guildId=reader.ReadInt();}
 }
}
