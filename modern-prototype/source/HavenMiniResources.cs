using System;
namespace Server.HavenPrototype {
 public static class HavenMiniResources {
  public static readonly int[][] Pools={
   new[]{0,1,2,3,4,5,6,7,8},new[]{9,10,11,12,13,14,15},new[]{16,17,18,19},
   new[]{20,21,22,23,24,25,26,27,28},new[]{29,30,31,32,33,34,35,83,84,85,86,87,88,89,90},
   new[]{71,72,73,74,75,76,77,78,79,80,81,82},new[]{36,37,38,39,40,41,42,43,44,45,46}
  };
  public static HavenResourceDeed Create(int family,int choice,int ordinaryUnits){
   family=Math.Max(0,Math.Min(Pools.Length-1,family));var pool=Pools[family];int id=pool[Math.Abs(choice%pool.Length)];
   int divisor=family>=5?25:family==3?5:(id>=6&&id<=8)||(id>=13&&id<=15)||id==19?4:1;
   return new HavenResourceDeed(id,Math.Max(1,ordinaryUnits/divisor));
  }
  public static HavenResourceDeed Boss(){return Create(Utility.Random(Pools.Length),Utility.Random(10000),100);}
  public static void Completion(Server.Items.Container parcel){
   int first=Utility.Random(Pools.Length),second=(first+1+Utility.Random(Pools.Length-1))%Pools.Length;
   parcel.DropItem(Create(first,Utility.Random(10000),125));parcel.DropItem(Create(second,Utility.Random(10000),125));
  }
 }
}
