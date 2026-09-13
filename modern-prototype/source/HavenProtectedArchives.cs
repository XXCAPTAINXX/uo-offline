using Server;
namespace Server.HavenPrototype {
 public static class HavenProtectedArchives {
  public static bool KeepNested(Mobile owner,Item item){
   if(!HavenPreview.Enabled||owner==null||item==null||item.RootParent!=owner)return false;
   for(var parent=item.Parent as Item;parent!=null;parent=parent.Parent as Item){
    var book=parent as HavenPetBook;
    if((book!=null&&book.Owner==owner)||parent is HavenChampionCodex)return true;
   }
   return false;
  }
 }
}
