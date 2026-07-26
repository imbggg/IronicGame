using UnityEngine;

public class Tile
{
    public enum Type
    {
        None,
        Floor,
        Wall
    }

    public static class PathCost
    {
        public const int Default = 10;
        public const int Floor = 9;
        public const int Wall = 15;
        public const int Corridor = 7;
        public const int MinCost = 1;
        public const int MaxCost = 15;
    }

    public int index;
    public Type type = Type.None;
    public Rect rect;
    public int cost = PathCost.Default;

    public Door door;
}
