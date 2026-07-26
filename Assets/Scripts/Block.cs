using System.Collections.Generic;
using UnityEngine;

public class Block
{
    public const int MinSize = 5;

    public enum Type
    {
        None,
        Corridor,
        Room
    }

    public int index;
    public Type type;
    public Rect rect;

    public List<Block> neighbors = new List<Block>();

    public Block(int index, float x, float y, float width, float height)
    {
        this.index = index;
        this.type = Type.None;
        this.rect = new Rect(x, y, width, height);
    }
}
