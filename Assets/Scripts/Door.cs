using UnityEngine;

public class Door
{
    public enum State
    {
        Open,
        Close,
        Lock
    }

    public enum Direction
    {
        Horizontal,
        Vertical
    }

    public Tile tile;
    public State state;
    public Direction direction;

    public Door(Tile tile, Direction direction, State state = State.Close)
    {
        this.tile = tile;
        this.direction = direction;
        this.state = state;
    }

    public bool Open()
    {
        if (State.Lock == state)
        {
            return false;
        }

        state = State.Open;
        return true;
    }

    public bool Close()
    {
        if (State.Lock == state)
        {
            return false;
        }

        state = State.Close;
        return true;
    }

    public bool Unlock()
    {
        if (State.Lock != state)
        {
            return false;
        }

        state = State.Close;
        return true;
    }
}
