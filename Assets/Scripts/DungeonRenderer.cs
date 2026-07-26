using System.Collections.Generic;
using UnityEngine;

public class DungeonRenderer : MonoBehaviour
{
    private Transform tileRoot;
    private TileMap tileMap;

    private static List<Sprite> FloorInnerNormal = new List<Sprite>();
    private static List<Sprite> FloorCornerInnerLeftTop = new List<Sprite>();
    private static List<Sprite> FloorCornerInnerRightTop = new List<Sprite>();
    private static List<Sprite> FloorCornerInnerLeftBottom = new List<Sprite>();
    private static List<Sprite> FloorCornerInnerRightBottom = new List<Sprite>();
    private static List<Sprite> FloorHorizontalTop = new List<Sprite>();
    private static List<Sprite> FloorHorizontalBottom = new List<Sprite>();
    private static List<Sprite> FloorVerticalLeft = new List<Sprite>();
    private static List<Sprite> FloorVerticalRight = new List<Sprite>();

    private static List<Sprite> WallHorizontalTop = new List<Sprite>();
    private static List<Sprite> WallHorizontalBottom = new List<Sprite>();
    private static List<Sprite> WallVerticalTop = new List<Sprite>();
    private static List<Sprite> WallVerticalLeft = new List<Sprite>();
    private static List<Sprite> WallVerticalRight = new List<Sprite>();
    private static List<Sprite> WallVerticalSplit = new List<Sprite>();
    private static List<Sprite> WallCornerInnerLeftTop = new List<Sprite>();
    private static List<Sprite> WallCornerInnerRightTop = new List<Sprite>();
    private static List<Sprite> WallCornerInnerLeftBottom = new List<Sprite>();
    private static List<Sprite> WallCornerInnerRightBottom = new List<Sprite>();
    private static List<Sprite> WallCornerOuterLeftTop = new List<Sprite>();
    private static List<Sprite> WallCornerOuterRightTop = new List<Sprite>();

    private static Sprite DoorHorizontal = null;
    private static Sprite DoorVertical = null;

    private static bool spritesLoaded = false;

    public void Render(TileMap map)
    {
        Clear();
        LoadSprites();

        this.tileMap = map;

        GameObject rootObject = new GameObject("TileRoot");
        rootObject.transform.parent = transform;
        tileRoot = rootObject.transform;

        for (int i = 0; i < tileMap.width * tileMap.height; i++)
        {
            Tile tile = tileMap.GetTile(i);
            if (null == tile)
            {
                continue;
            }

            if (Tile.Type.None == tile.type)
            {
                continue;
            }

            Sprite sprite = null;
            if (Tile.Type.Floor == tile.type)
            {
                sprite = GetFloorSprite(tile);
            }
            else if (Tile.Type.Wall == tile.type)
            {
                sprite = GetWallSprite(tile);
            }

            if (null == sprite)
            {
                continue;
            }

            GameObject tileObject = new GameObject($"Tile_{tile.index}");
            tileObject.transform.parent = tileRoot;
            tileObject.transform.position = new Vector3(tile.rect.x + 0.5f, tile.rect.y + 0.5f, 0.0f);

            SpriteRenderer spriteRenderer = tileObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 10;
        }

        RenderDoors();
        FitCamera();
    }

    private void RenderDoors()
    {
        for (int i = 0; i < tileMap.width * tileMap.height; i++)
        {
            Tile tile = tileMap.GetTile(i);
            if (null == tile)
            {
                continue;
            }

            if (null == tile.door)
            {
                continue;
            }

            Sprite sprite = DoorVertical;
            if (Door.Direction.Horizontal == tile.door.direction)
            {
                sprite = DoorHorizontal;
            }

            if (null == sprite)
            {
                continue;
            }

            GameObject doorObject = new GameObject($"Door_{tile.index}");
            doorObject.transform.parent = tileRoot;
            doorObject.transform.position = new Vector3(tile.rect.x + 0.5f, tile.rect.y + 0.5f, 0.0f);

            SpriteRenderer spriteRenderer = doorObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 11;

            DoorView doorView = doorObject.AddComponent<DoorView>();
            doorView.Init(tile.door, spriteRenderer, sprite);
        }
    }

    private void FitCamera()
    {
        Camera camera = Camera.main;
        if (null == camera)
        {
            return;
        }

        camera.transform.position = new Vector3(tileMap.width / 2.0f, tileMap.height / 2.0f, -10.0f);

        camera.orthographic = true;
        float sizeByHeight = tileMap.height / 2.0f;
        float sizeByWidth = tileMap.width / 2.0f / camera.aspect;
        camera.orthographicSize = 10.0f;
    }

    public void Clear()
    {
        if (null == tileRoot)
        {
            return;
        }

        tileRoot.parent = null;
        GameObject.DestroyImmediate(tileRoot.gameObject);
        tileRoot = null;
    }

    private bool IsWall(Tile tile)
    {
        if (null == tile)
        {
            return false;
        }

        return Tile.Type.Wall == tile.type;
    }

    private bool IsFloor(Tile tile)
    {
        if (null == tile)
        {
            return false;
        }

        return Tile.Type.Floor == tile.type;
    }

    private Sprite GetFloorSprite(Tile tile)
    {
        int x = (int)tile.rect.x;
        int y = (int)tile.rect.y;

        var top = tileMap.GetTile(x, y + 1);
        var left = tileMap.GetTile(x - 1, y);
        var right = tileMap.GetTile(x + 1, y);
        var bottom = tileMap.GetTile(x, y - 1);

        if (true == IsWall(top) && true == IsWall(left))
        {
            return GetRandomSprite(FloorCornerInnerLeftTop);
        }

        if (true == IsWall(top) && true == IsWall(right))
        {
            return GetRandomSprite(FloorCornerInnerRightTop);
        }

        if (true == IsWall(bottom) && true == IsWall(left))
        {
            return GetRandomSprite(FloorCornerInnerLeftBottom);
        }

        if (true == IsWall(bottom) && true == IsWall(right))
        {
            return GetRandomSprite(FloorCornerInnerRightBottom);
        }

        if (true == IsWall(top))
        {
            return GetRandomSprite(FloorHorizontalTop);
        }

        if (true == IsWall(left))
        {
            return GetRandomSprite(FloorVerticalLeft);
        }

        if (true == IsWall(right))
        {
            return GetRandomSprite(FloorVerticalRight);
        }

        if (true == IsWall(bottom))
        {
            return GetRandomSprite(FloorHorizontalBottom);
        }

        return GetRandomSprite(FloorInnerNormal);
    }

    private Sprite GetWallSprite(Tile tile)
    {
        int x = (int)tile.rect.x;
        int y = (int)tile.rect.y;

        var leftTop = tileMap.GetTile(x - 1, y + 1);
        var top = tileMap.GetTile(x, y + 1);
        var rightTop = tileMap.GetTile(x + 1, y + 1);
        var left = tileMap.GetTile(x - 1, y);
        var right = tileMap.GetTile(x + 1, y);
        var leftBottom = tileMap.GetTile(x - 1, y - 1);
        var bottom = tileMap.GetTile(x, y - 1);
        var rightBottom = tileMap.GetTile(x + 1, y - 1);

        bool[] floorsAroundWall = new bool[9] {
            IsFloor(leftTop),
            IsFloor(top),
            IsFloor(rightTop),
            IsFloor(left),
            false,
            IsFloor(right),
            IsFloor(leftBottom),
            IsFloor(bottom),
            IsFloor(rightBottom)
        };

        bool[] f = new bool[4] { false, false, false, false };

        if (true == floorsAroundWall[0]) { f[0] = true; }
        if (true == floorsAroundWall[1]) { f[0] = true; f[1] = true; }
        if (true == floorsAroundWall[2]) { f[1] = true; }
        if (true == floorsAroundWall[3]) { f[0] = true; f[2] = true; }
        if (true == floorsAroundWall[5]) { f[1] = true; f[3] = true; }
        if (true == floorsAroundWall[6]) { f[2] = true; }
        if (true == floorsAroundWall[7]) { f[2] = true; f[3] = true; }
        if (true == floorsAroundWall[8]) { f[3] = true; }

        if (true == f[0] && false == f[1] && false == f[2] && false == f[3])
        {
            return GetRandomSprite(WallCornerInnerRightBottom);
        }

        if (false == f[0] && true == f[1] && false == f[2] && false == f[3])
        {
            return GetRandomSprite(WallCornerInnerLeftBottom);
        }

        if (false == f[0] && false == f[1] && true == f[2] && false == f[3])
        {
            return GetRandomSprite(WallCornerInnerRightTop);
        }

        if (false == f[0] && false == f[1] && false == f[2] && true == f[3])
        {
            return GetRandomSprite(WallCornerInnerLeftTop);
        }

        if (true == f[0] && true == f[1] && false == f[2] && false == f[3])
        {
            return GetRandomSprite(WallHorizontalBottom);
        }

        if (true == f[0] && false == f[1] && true == f[2] && false == f[3])
        {
            return GetRandomSprite(WallVerticalRight);
        }

        if (true == f[0] && false == f[1] && false == f[2] && true == f[3])
        {
            return GetRandomSprite(WallCornerOuterRightTop);
        }

        if (false == f[0] && true == f[1] && true == f[2] && false == f[3])
        {
            return GetRandomSprite(WallCornerOuterLeftTop);
        }

        if (false == f[0] && true == f[1] && false == f[2] && true == f[3])
        {
            return GetRandomSprite(WallVerticalLeft);
        }

        if (false == f[0] && false == f[1] && true == f[2] && true == f[3])
        {
            if (true == IsWall(bottom))
            {
                return GetRandomSprite(WallVerticalSplit);
            }
            return GetRandomSprite(WallHorizontalTop);
        }

        if (true == f[0] && true == f[1] && true == f[2] && false == f[3])
        {
            return GetRandomSprite(WallCornerOuterLeftTop);
        }

        if (true == f[0] && true == f[1] && false == f[2] && true == f[3])
        {
            return GetRandomSprite(WallCornerOuterRightTop);
        }

        if (true == f[0] && false == f[1] && true == f[2] && true == f[3])
        {
            if (true == IsWall(bottom))
            {
                return GetRandomSprite(WallVerticalSplit);
            }
            return GetRandomSprite(WallHorizontalTop);
        }

        if (false == f[0] && true == f[1] && true == f[2] && true == f[3])
        {
            if (true == IsWall(bottom))
            {
                return GetRandomSprite(WallVerticalSplit);
            }
            return GetRandomSprite(WallHorizontalTop);
        }

        if (true == f[0] && true == f[1] && true == f[2] && true == f[3])
        {
            if (true == IsWall(top) && false == IsWall(bottom))
            {
                return GetRandomSprite(WallHorizontalTop);
            }

            if (true == IsWall(top) && true == IsWall(bottom))
            {
                return GetRandomSprite(WallVerticalSplit);
            }

            if (true == IsWall(bottom))
            {
                return GetRandomSprite(WallVerticalTop);
            }

            if (true == IsWall(right))
            {
                return GetRandomSprite(WallHorizontalTop);
            }

            if (true == IsWall(left))
            {
                return GetRandomSprite(WallHorizontalTop);
            }
        }

        return GetRandomSprite(WallHorizontalTop);
    }

    private void Add(List<Sprite> list, string name)
    {
        Sprite sprite = Resources.Load<Sprite>("Sprites/" + name);
        if (null == sprite)
        {
            return;
        }
        list.Add(sprite);
    }

    private void LoadSprites()
    {
        if (true == spritesLoaded)
        {
            return;
        }
        spritesLoaded = true;

        Add(FloorInnerNormal, "Floor.InnerNormal_1");
        Add(FloorInnerNormal, "Floor.InnerNormal_2");
        Add(FloorCornerInnerLeftTop, "Floor.CornerInnerLeftTop_1");
        Add(FloorCornerInnerRightTop, "Floor.CornerInnerRightTop_1");
        Add(FloorCornerInnerLeftBottom, "Floor.CornerInnerLeftBottom_1");
        Add(FloorCornerInnerRightBottom, "Floor.CornerInnerRightBottom_1");
        Add(FloorHorizontalTop, "Floor.HorizontalTop_1");
        Add(FloorHorizontalTop, "Floor.HorizontalTop_2");
        Add(FloorHorizontalBottom, "Floor.HorizontalBottom_1");
        Add(FloorHorizontalBottom, "Floor.HorizontalBottom_2");
        Add(FloorVerticalLeft, "Floor.VerticalLeft_1");
        Add(FloorVerticalRight, "Floor.VerticalRight_1");

        Add(WallHorizontalTop, "Wall.HorizontalTop_1");
        Add(WallHorizontalTop, "Wall.HorizontalTop_2");
        Add(WallHorizontalTop, "Wall.HorizontalTop_3");
        Add(WallHorizontalTop, "Wall.HorizontalTop_4");
        Add(WallHorizontalBottom, "Wall.HorizontalBottom_1");
        Add(WallHorizontalBottom, "Wall.HorizontalBottom_2");
        Add(WallHorizontalBottom, "Wall.HorizontalBottom_3");
        Add(WallHorizontalBottom, "Wall.HorizontalBottom_4");
        Add(WallVerticalTop, "Wall.VerticalTop_1");
        Add(WallVerticalTop, "Wall.VerticalTop_2");
        Add(WallVerticalTop, "Wall.VerticalTop_3");
        Add(WallVerticalLeft, "Wall.VerticalLeft_1");
        Add(WallVerticalLeft, "Wall.VerticalLeft_2");
        Add(WallVerticalLeft, "Wall.VerticalLeft_3");
        Add(WallVerticalRight, "Wall.VerticalRight_1");
        Add(WallVerticalRight, "Wall.VerticalRight_2");
        Add(WallVerticalRight, "Wall.VerticalRight_3");
        Add(WallVerticalSplit, "Wall.VerticalSplit_1");
        Add(WallVerticalSplit, "Wall.VerticalSplit_2");
        Add(WallVerticalSplit, "Wall.VerticalSplit_3");
        Add(WallVerticalSplit, "Wall.VerticalSplit_4");
        Add(WallCornerInnerLeftTop, "Wall.CornerInnerLeftTop_1");
        Add(WallCornerInnerRightTop, "Wall.CornerInnerRightTop_1");
        Add(WallCornerInnerLeftBottom, "Wall.CornerInnerLeftBottom_1");
        Add(WallCornerInnerRightBottom, "Wall.CornerInnerRightBottom_1");
        Add(WallCornerOuterLeftTop, "Wall.CornerOuterLeftTop_1");
        Add(WallCornerOuterLeftTop, "Wall.CornerOuterLeftTop_2");
        Add(WallCornerOuterRightTop, "Wall.CornerOuterRightTop_1");
        Add(WallCornerOuterRightTop, "Wall.CornerOuterRightTop_2");

        DoorHorizontal = Resources.Load<Sprite>("Sprites/Door.Horizontal.New");
        DoorVertical = Resources.Load<Sprite>("Sprites/Door.Vertical.New");

        if (0 == FloorInnerNormal.Count || 0 == WallHorizontalTop.Count)
        {
            Debug.LogError("스프라이트를 찾을 수 없습니다. Assets/Resources/Sprites 경로를 확인하세요.");
        }
    }

    private Sprite GetRandomSprite(List<Sprite> sprites)
    {
        if (0 == sprites.Count)
        {
            return null;
        }

        return sprites[Random.Range(0, sprites.Count)];
    }
}
