namespace Bloodcraft.Utilities;
internal static class Progression
{
    const float EXP_CONSTANT = 0.1f;
    const float EXP_POWER = 2f;
    public static int ConvertXpToLevel(float xp)
    {
        return (int)(EXP_CONSTANT * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / EXP_CONSTANT, EXP_POWER);
    }
}
