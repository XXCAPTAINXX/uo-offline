namespace Server.UOOffline;

public static class HavenNewcomerTraining
{
    public static bool Applies(Mobile mobile, Skill skill) => HavenNewcomerLuck.GetBonus(mobile) > 0 && skill.BaseFixedPoint < 1000;
    public static double ChanceMultiplier(Mobile mobile, Skill skill) => Applies(mobile, skill) ? 5.0 : 1.0;
    public static int GainAmount(Mobile mobile, Skill skill, int normal) => Applies(mobile, skill)
        ? System.Math.Min(normal * 5, 1000 - skill.BaseFixedPoint) : normal;
}
