using System.Collections.Generic;

public static class PropBlock
{
    private static HashSet<int> blocked = new HashSet<int>();

    public static void Clear()
    {
        blocked.Clear();
    }

    public static void Add(int tileIndex)
    {
        blocked.Add(tileIndex);
    }

    public static bool IsBlocked(int tileIndex)
    {
        return blocked.Contains(tileIndex);
    }
}
