public static class GameData
{
    public const int MinimumRoomCount = 5;
    public const int MaximumRoomCount = 30;

    public static int selectedCharacter = 0;
    public static int difficulty = 1;
    public static int roomCount = 15;
    public static float elapsedTime = 0.0f;

    public static string GetDifficultyName()
    {
        switch (difficulty)
        {
            case 0:
                return "\uC26C\uC6C0";
            case 2:
                return "\uC5B4\uB824\uC6C0";
            default:
                return "\uBCF4\uD1B5";
        }
    }

    public static float GetMonsterAttackCooldownMultiplier()
    {
        switch (difficulty)
        {
            case 0:
                return 1.35f;
            case 2:
                return 0.75f;
            default:
                return 1.0f;
        }
    }

    public static int GetMonsterAttackDamage(int baseDamage)
    {
        switch (difficulty)
        {
            case 0:
                return System.Math.Max(1, baseDamage - 1);
            case 2:
                return baseDamage + 1;
            default:
                return baseDamage;
        }
    }

    public static string FormatElapsedTime()
    {
        int totalSeconds = UnityEngine.Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
