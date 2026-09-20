using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    public int roomCount = 15;
    public int minRoomSize = 5;
    public int maxRoomSize = 12;
    public int roomPadding = 3;

    public List<Block> blocks = new List<Block>();
    public List<Block> rooms = new List<Block>();
    public TileMap tileMap;
    public CorridorGraph corridorGraph;
    public List<Door> doors = new List<Door>();

    private HashSet<Tile> corridorTiles = new HashSet<Tile>();

    private DungeonRenderer dungeonRenderer;
    [SerializeField] private GameObject[] playerPrefabs;
    [SerializeField] private GameObject bossPrefab;

    [Header("몬스터 스폰")]
    [SerializeField, Min(1)] private int minimumMonstersPerRoom = 1;
    [SerializeField, Min(1)] private int maximumMonstersPerRoom = 2;
    [SerializeField, Range(0.0f, 1.0f)] private float monsterRoomSpawnChance = 0.75f;
    [SerializeField, Min(1)] private int monsterTypeCount = 5;

    [Header("방 전투")]
    [SerializeField, InspectorName("방 전투 사용")]
    private bool enableRoomCombat = true;

    [SerializeField, InspectorName("아이러닉 능력 선택 사용")]
    private bool enableIronicRoomRewards = true;

    private Player player;
    private Boss activeBoss;
    private Block bossRoom;
    private RoomCombatSystem roomCombatSystem;
    private readonly List<Monster> monsters = new List<Monster>();

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        blocks.Clear();
        rooms.Clear();
        doors.Clear();
        corridorTiles.Clear();

        minRoomSize = Mathf.Min(maxRoomSize, minRoomSize);
        maxRoomSize = Mathf.Max(maxRoomSize, minRoomSize);
        minRoomSize = Mathf.Max(minRoomSize, Block.MinSize);
        maxRoomSize = Mathf.Max(maxRoomSize, Block.MinSize);

        var roomSizeWeightRandom = CreateRoomSizeWeightRandom(minRoomSize, maxRoomSize);
        var ratioWeightRandom = CreateRatioWeightRandom();

        CreateRooms(roomSizeWeightRandom, ratioWeightRandom);
        InsertCorridorBlocks(roomSizeWeightRandom);

        tileMap = new TileMap(blocks);
        corridorGraph = new CorridorGraph(rooms);

        GenerateCorridor();
        GenerateWall();

        GenerateDoor();
        VerifyConnectivity();

        if (null == dungeonRenderer)
        {
            dungeonRenderer = GetComponent<DungeonRenderer>();
            if (null == dungeonRenderer)
            {
                dungeonRenderer = gameObject.AddComponent<DungeonRenderer>();
            }
        }

        dungeonRenderer.Render(tileMap, rooms);

        SpawnPlayer();
        bossRoom = null != bossPrefab && null != player
            ? FindFarthestRoom(player.transform.position)
            : null;
        SpawnBossAltar();
        SpawnMonsters();
        ConfigureRoomCombat();
    }

    private void CreateRooms(WeightRandom<int> roomSizeWeightRandom, WeightRandom<Tuple<float, float>> ratioWeightRandom)
    {
        float areaOfBlocks = maxRoomSize * maxRoomSize * roomCount;
        float roomCreateRadius = Mathf.Sqrt(areaOfBlocks / Mathf.PI);

        for (int i = 0; i < roomCount; i++)
        {
            float theta = 2.0f * Mathf.PI * UnityEngine.Random.Range(0.0f, 1.0f);
            float radius = UnityEngine.Random.Range(0.0f, roomCreateRadius);

            int x = (int)(radius * Mathf.Cos(theta));
            int y = (int)(radius * Mathf.Sin(theta));
            int width = 0;
            int height = 0;
            var range = ratioWeightRandom.Random();

            if (0 == UnityEngine.Random.Range(0, 100) % 2)
            {
                width = roomSizeWeightRandom.Random();
                height = (int)(width * UnityEngine.Random.Range(range.Item1, range.Item2));
                height = Mathf.Max(minRoomSize, height);
                height = Mathf.Min(maxRoomSize, height);
            }
            else
            {
                height = roomSizeWeightRandom.Random();
                width = (int)(height * UnityEngine.Random.Range(range.Item1, range.Item2));
                width = Mathf.Max(minRoomSize, width);
                width = Mathf.Min(maxRoomSize, width);
            }

            Block block = new Block(blocks.Count, x, y, width, height);
            block.type = Block.Type.Room;
            blocks.Add(block);
            rooms.Add(block);
        }

        RepositionBlocks();
    }

    private void InsertCorridorBlocks(WeightRandom<int> roomSizeWeightRandom)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            Block room = rooms[i];
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (70 < UnityEngine.Random.Range(0, 100))
                {
                    continue;
                }

                Block neighbor = rooms[j];

                float distance = Vector3.Distance(room.rect.center, neighbor.rect.center);
                float roomRadius = Vector3.Distance(room.rect.center, new Vector3(room.rect.x, room.rect.y));
                float neighorRadius = Vector3.Distance(neighbor.rect.center, new Vector3(neighbor.rect.x, neighbor.rect.y));

                if (distance < roomRadius + neighorRadius)
                {
                    Vector3 interpolation = Vector3.Lerp(room.rect.center, neighbor.rect.center, 0.5f);
                    int width = roomSizeWeightRandom.Random() / 2;
                    int height = roomSizeWeightRandom.Random() / 2;
                    int x = (int)(interpolation.x - width / 2);
                    int y = (int)(interpolation.y - height / 2);

                    Block block = new Block(blocks.Count, x, y, width, height);
                    block.type = Block.Type.Corridor;
                    blocks.Add(block);
                }
            }
        }

        RepositionBlocks();
    }

    private WeightRandom<Tuple<float, float>> CreateRatioWeightRandom()
    {
        var weightRandom = new WeightRandom<Tuple<float, float>>();
        int weight = 1;
        for (float rate = 3.0f; rate >= 0.5f; rate -= 0.1f)
        {
            var range = new Tuple<float, float>(rate - 0.1f, rate);
            weightRandom.AddElement(weight, range);
            if (1.0f < rate)
            {
                weight++;
            }
            else
            {
                weight--;
            }
        }

        return weightRandom;
    }

    private WeightRandom<int> CreateRoomSizeWeightRandom(int min, int max)
    {
        var weightRandom = new WeightRandom<int>();

        int delta = 0;
        int elmtCount = max - min + 1;
        if (0 == elmtCount % 2)
        {
            delta = 0;
            for (int i = (min + max) / 2; i >= min; i--)
            {
                int weight = elmtCount / 2 - delta++;
                weightRandom.AddElement(weight, i);
            }

            delta = 0;
            for (int i = (min + max) / 2 + 1; i <= max; i++)
            {
                int weight = elmtCount / 2 - delta++;
                weightRandom.AddElement(weight, i);
            }
        }
        else
        {
            delta = 0;
            for (int i = (min + max) / 2; i >= min; i--)
            {
                int weight = elmtCount / 2 + 1 - delta++;
                weightRandom.AddElement(weight, i);
            }

            delta = 1;
            for (int i = (min + max) / 2 + 1; i <= max; i++)
            {
                int weight = elmtCount / 2 + 1 - delta++;
                weightRandom.AddElement(weight, i);
            }
        }

        return weightRandom;
    }

    private void GenerateCorridor()
    {
        foreach (var corridor in corridorGraph.corridors)
        {
            Block src = corridor.p1;
            Block dest = corridor.p2;

            Tile from = tileMap.GetTile((int)src.rect.center.x, (int)src.rect.center.y);
            Tile to = tileMap.GetTile((int)dest.rect.center.x, (int)dest.rect.center.y);

            if (null == from || null == to)
            {
                continue;
            }

            Rect searchLimitBoundary = new Rect();
            searchLimitBoundary.xMin = Mathf.Min(src.rect.xMin, dest.rect.xMin);
            searchLimitBoundary.xMax = Mathf.Max(src.rect.xMax, dest.rect.xMax);
            searchLimitBoundary.yMin = Mathf.Min(src.rect.yMin, dest.rect.yMin);
            searchLimitBoundary.yMax = Mathf.Max(src.rect.yMax, dest.rect.yMax);

            foreach (Block neighbor in src.neighbors)
            {
                searchLimitBoundary.xMin = Mathf.Min(searchLimitBoundary.xMin, neighbor.rect.xMin);
                searchLimitBoundary.xMax = Mathf.Max(searchLimitBoundary.xMax, neighbor.rect.xMax);
                searchLimitBoundary.yMin = Mathf.Min(searchLimitBoundary.yMin, neighbor.rect.yMin);
                searchLimitBoundary.yMax = Mathf.Max(searchLimitBoundary.yMax, neighbor.rect.yMax);
            }

            foreach (Block neighbor in dest.neighbors)
            {
                searchLimitBoundary.xMin = Mathf.Min(searchLimitBoundary.xMin, neighbor.rect.xMin);
                searchLimitBoundary.xMax = Mathf.Max(searchLimitBoundary.xMax, neighbor.rect.xMax);
                searchLimitBoundary.yMin = Mathf.Min(searchLimitBoundary.yMin, neighbor.rect.yMin);
                searchLimitBoundary.yMax = Mathf.Max(searchLimitBoundary.yMax, neighbor.rect.yMax);
            }

            AStarPathFinder pathFinder = new AStarPathFinder(tileMap, searchLimitBoundary, new AStarPathFinder.StraightLookup());
            var path = pathFinder.FindPath(from, to);

            Rect floorAreaOfSrc = new Rect(src.rect.xMin + 1, src.rect.yMin + 1, src.rect.width - 2, src.rect.height - 2);
            Rect floorAreaOfDest = new Rect(dest.rect.xMin + 1, dest.rect.yMin + 1, dest.rect.width - 2, dest.rect.height - 2);

            foreach (var tile in path)
            {
                tile.cost = Tile.PathCost.Corridor;

                if (true == floorAreaOfSrc.Contains(new Vector2(tile.rect.x, tile.rect.y)) ||
                    true == floorAreaOfDest.Contains(new Vector2(tile.rect.x, tile.rect.y)))
                {
                    continue;
                }

                corridor.tiles.Add(tile);
            }


        }
    }

    private void GenerateWall()
    {
        foreach (Block room in rooms)
        {
            for (int y = (int)room.rect.yMin; y < (int)room.rect.yMax - 1; y++)
            {
                for (int x = (int)room.rect.xMin; x < (int)room.rect.xMax - 1; x++)
                {
                    Tile tile = tileMap.GetTile(x, y);
                    if (null == tile) { continue; }
                    tile.type = Tile.Type.Floor;
                    tile.cost = Tile.PathCost.Floor;
                }
            }

            for (int y = (int)room.rect.yMin; y < (int)room.rect.yMax - 1; y++)
            {
                Tile left = tileMap.GetTile((int)room.rect.xMin, y);
                if (null != left) { left.type = Tile.Type.Wall; left.cost = Tile.PathCost.Wall; }

                Tile right = tileMap.GetTile((int)room.rect.xMax - 1, y);
                if (null != right) { right.type = Tile.Type.Wall; right.cost = Tile.PathCost.Wall; }
            }

            for (int x = (int)room.rect.xMin; x < (int)room.rect.xMax - 1; x++)
            {
                Tile up = tileMap.GetTile(x, (int)room.rect.yMin);
                if (null != up) { up.type = Tile.Type.Wall; up.cost = Tile.PathCost.Wall; }

                Tile down = tileMap.GetTile(x, (int)room.rect.yMax - 1);
                if (null != down) { down.type = Tile.Type.Wall; down.cost = Tile.PathCost.Wall; }
            }
        }

        foreach (var corridor in corridorGraph.corridors)
        {
            foreach (var tile in corridor.tiles)
            {
                tile.type = Tile.Type.Floor;
                tile.cost = Tile.PathCost.Floor;
                corridorTiles.Add(tile);
            }
        }

        Action<int, int> IfNotNullBuildWall = (int x, int y) =>
        {
            Tile tile = tileMap.GetTile(x, y);
            if (null == tile)
            {
                return;
            }

            if (Tile.Type.None != tile.type)
            {
                return;
            }

            tile.type = Tile.Type.Wall;
            tile.cost = Tile.PathCost.Wall;
        };

        foreach (var corridor in corridorGraph.corridors)
        {
            foreach (var tile in corridor.tiles)
            {
                int x = (int)tile.rect.x;
                int y = (int)tile.rect.y;

                IfNotNullBuildWall(x - 1, y - 1);
                IfNotNullBuildWall(x - 1, y);
                IfNotNullBuildWall(x - 1, y + 1);
                IfNotNullBuildWall(x, y - 1);
                IfNotNullBuildWall(x, y + 1);
                IfNotNullBuildWall(x + 1, y - 1);
                IfNotNullBuildWall(x + 1, y);
                IfNotNullBuildWall(x + 1, y + 1);
            }
        }
    }

    private bool IsWallTile(Tile tile)
    {
        if (null == tile)
        {
            return false;
        }

        return Tile.Type.Wall == tile.type;
    }

    private void GenerateDoor()
    {
        foreach (Block room in rooms)
        {
            int xMin = (int)room.rect.xMin;
            int xMax = (int)room.rect.xMax - 1;
            int yMin = (int)room.rect.yMin;
            int yMax = (int)room.rect.yMax - 1;

            for (int x = xMin; x <= xMax; x++)
            {
                ScanWallTile(x, yMin, Door.Direction.Vertical);
                ScanWallTile(x, yMax, Door.Direction.Vertical);
            }

            for (int y = yMin; y <= yMax; y++)
            {
                ScanWallTile(xMin, y, Door.Direction.Horizontal);
                ScanWallTile(xMax, y, Door.Direction.Horizontal);
            }
        }

        VerifyDoors();
    }

    private void ScanWallTile(int x, int y, Door.Direction fallbackDirection)
    {
        Tile tile = tileMap.GetTile(x, y);
        if (null == tile)
        {
            return;
        }

        if (Tile.Type.Floor != tile.type)
        {
            return;
        }

        if (null != tile.door)
        {
            return;
        }

        Tile left   = tileMap.GetTile(x - 1, y);
        Tile right  = tileMap.GetTile(x + 1, y);
        Tile top    = tileMap.GetTile(x, y + 1);
        Tile bottom = tileMap.GetTile(x, y - 1);

        bool leftWall   = IsWallTile(left);
        bool rightWall  = IsWallTile(right);
        bool topWall    = IsWallTile(top);
        bool bottomWall = IsWallTile(bottom);

        if (true == leftWall && true == rightWall && false == topWall && false == bottomWall)
        {
            PlaceDoor(tile, Door.Direction.Horizontal);
            return;
        }

        if (true == topWall && true == bottomWall && false == leftWall && false == rightWall)
        {
            PlaceDoor(tile, Door.Direction.Vertical);
            return;
        }

        int horizontalWallCount = (leftWall ? 1 : 0) + (rightWall ? 1 : 0);
        int verticalWallCount = (topWall ? 1 : 0) + (bottomWall ? 1 : 0);

        if (horizontalWallCount > verticalWallCount)
        {
            PlaceDoor(tile, Door.Direction.Horizontal);
            return;
        }

        if (verticalWallCount > horizontalWallCount)
        {
            PlaceDoor(tile, Door.Direction.Vertical);
            return;
        }

        PlaceDoor(tile, fallbackDirection);
    }

    private void PlaceDoor(Tile tile, Door.Direction direction)
    {
        if (null == tile)
        {
            return;
        }

        if (null != tile.door)
        {
            return;
        }

        Door door = new Door(tile, direction, Door.State.Close);
        tile.door = door;
        doors.Add(door);
    }

    private void VerifyDoors()
    {
        foreach (Block room in rooms)
        {
            int doorCount = 0;

            int xMin = (int)room.rect.xMin;
            int xMax = (int)room.rect.xMax - 1;
            int yMin = (int)room.rect.yMin;
            int yMax = (int)room.rect.yMax - 1;

            for (int x = xMin; x <= xMax; x++)
            {
                doorCount += CountDoor(x, yMin);
                doorCount += CountDoor(x, yMax);
            }

            for (int y = yMin; y <= yMax; y++)
            {
                doorCount += CountDoor(xMin, y);
                doorCount += CountDoor(xMax, y);
            }

            if (0 == doorCount)
            {
                Debug.LogWarning($"[Dungeon] Room {room.index} has no door. rect={room.rect}");
            }
        }
    }

    private int CountDoor(int x, int y)
    {
        Tile tile = tileMap.GetTile(x, y);
        if (null == tile)
        {
            return 0;
        }

        if (null == tile.door)
        {
            return 0;
        }

        return 1;
    }

    private void VerifyConnectivity()
    {
        if (0 == rooms.Count)
        {
            return;
        }

        Tile start = FindRoomFloorTile(rooms[0]);
        if (null == start)
        {
            Debug.LogWarning("[Dungeon] Cannot find a floor tile in the first room.");
            return;
        }

        HashSet<Tile> visited = new HashSet<Tile>();
        Queue<Tile> queue = new Queue<Tile>();

        visited.Add(start);
        queue.Enqueue(start);

        while (0 < queue.Count)
        {
            Tile current = queue.Dequeue();

            int x = (int)current.rect.x;
            int y = (int)current.rect.y;

            EnqueueIfFloor(x - 1, y, visited, queue);
            EnqueueIfFloor(x + 1, y, visited, queue);
            EnqueueIfFloor(x, y - 1, visited, queue);
            EnqueueIfFloor(x, y + 1, visited, queue);
        }

        int isolatedCount = 0;

        foreach (Block room in rooms)
        {
            Tile roomTile = FindRoomFloorTile(room);
            if (null == roomTile)
            {
                continue;
            }

            if (false == visited.Contains(roomTile))
            {
                isolatedCount++;
                Debug.LogWarning($"[Dungeon] Room {room.index} is isolated. rect={room.rect}");
            }
        }

        if (0 < isolatedCount)
        {
            Debug.LogWarning($"[Dungeon] {isolatedCount} isolated room(s) found.");
        }
    }

    private void EnqueueIfFloor(int x, int y, HashSet<Tile> visited, Queue<Tile> queue)
    {
        Tile tile = tileMap.GetTile(x, y);
        if (null == tile)
        {
            return;
        }

        if (Tile.Type.Floor != tile.type)
        {
            return;
        }

        if (true == visited.Contains(tile))
        {
            return;
        }

        visited.Add(tile);
        queue.Enqueue(tile);
    }

    private Tile FindRoomFloorTile(Block room)
    {
        // 방 중심에서 가까운 타일을 먼저 고른다.
        // 소품과 문 위에는 절대 세우지 않는다.
        Tile best = null;
        float bestDistance = float.MaxValue;

        Vector2 roomCenter = new Vector2(room.rect.center.x, room.rect.center.y);

        for (int y = (int)room.rect.yMin + 1; y < (int)room.rect.yMax - 1; y++)
        {
            for (int x = (int)room.rect.xMin + 1; x < (int)room.rect.xMax - 1; x++)
            {
                Tile tile = tileMap.GetTile(x, y);
                if (null == tile || Tile.Type.Floor != tile.type)
                {
                    continue;
                }

                if (null != tile.door || true == PropBlock.IsBlocked(tile.index))
                {
                    continue;
                }

                Vector2 position = new Vector2(tile.rect.x + 0.5f, tile.rect.y + 0.5f);
                float distance = Vector2.SqrMagnitude(position - roomCenter);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = tile;
                }
            }
        }

        if (null != best)
        {
            return best;
        }

        // 방 전체가 막혀 있는 예외 상황에서는 소품만이라도 무시하고 바닥을 찾는다.
        for (int y = (int)room.rect.yMin + 1; y < (int)room.rect.yMax - 1; y++)
        {
            for (int x = (int)room.rect.xMin + 1; x < (int)room.rect.xMax - 1; x++)
            {
                Tile tile = tileMap.GetTile(x, y);
                if (null != tile && Tile.Type.Floor == tile.type && null == tile.door)
                {
                    return tile;
                }
            }
        }

        return null;
    }

    private void SpawnPlayer()
    {
        if (null != player)
        {
            GameObject.DestroyImmediate(player.gameObject);
            player = null;
        }

        Block centerRoom = FindCenterRoom();
        if (null == centerRoom)
        {
            return;
        }

        Tile spawnTile = FindRoomFloorTile(centerRoom);
        if (null == spawnTile)
        {
            return;
        }

        GameObject playerObject = null;

        GameObject selectedPrefab = null;
        if (null != playerPrefabs && 0 < playerPrefabs.Length)
        {
            int index = Mathf.Clamp(GameData.selectedCharacter, 0, playerPrefabs.Length - 1);
            selectedPrefab = playerPrefabs[index];
        }

        if (null != selectedPrefab)
        {
            playerObject = Instantiate(selectedPrefab, transform);
            playerObject.name = "Player";

            SpriteRenderer prefabRenderer = playerObject.GetComponent<SpriteRenderer>();
            if (null != prefabRenderer)
            {
                prefabRenderer.sortingOrder = 20;
            }

            player = playerObject.GetComponent<Player>();
            if (null == player)
            {
                player = playerObject.AddComponent<Player>();
            }
        }
        else
        {
            playerObject = new GameObject("Player");
            playerObject.transform.parent = transform;

            SpriteRenderer spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateSquareSprite(Color.red);
            spriteRenderer.sortingOrder = 20;

            player = playerObject.AddComponent<Player>();
        }

        // 방 중심을 그대로 쓰면 그 자리에 소품이 있을 때 플레이어가 낀다.
        // 위에서 검증한 spawnTile 을 그대로 사용해야 한다.
        Vector2 spawnPosition = new Vector2(
            spawnTile.rect.x + 0.5f,
            spawnTile.rect.y + 0.5f
        );

        player.Init(tileMap, spawnPosition);
    }

    [Header("보스 제단")]
    [SerializeField]
    [Tooltip("제단 스프라이트의 Pixels Per Unit. 캐릭터와 같은 값으로 맞춘다.")]
    private float altarPixelsPerUnit = 108.0f;

    /// <summary>
    /// 보스방 중앙에 제단을 놓는다. 보스는 제단을 사용해야 등장한다.
    /// </summary>
    private void SpawnBossAltar()
    {
        if (null == bossRoom)
        {
            return;
        }

        Tile altarTile = FindRoomCenterFloorTile(bossRoom);
        if (null == altarTile)
        {
            altarTile = FindRoomFloorTile(bossRoom);
        }

        if (null == altarTile)
        {
            return;
        }

        BossAltar.Spawn(
            new Vector3(altarTile.rect.x + 0.5f, altarTile.rect.y + 0.5f, 0.0f),
            altarPixelsPerUnit
        );
    }

    public bool TrySummonBoss()
    {
        if (null != activeBoss || null != FindAnyObjectByType<Boss>())
        {
            return false;
        }

        if (null == player || null == bossRoom)
        {
            return false;
        }

        Vector2 playerPosition = player.transform.position;
        if (false == bossRoom.rect.Contains(playerPosition))
        {
            return false;
        }

        return SpawnBoss();
    }

    private bool SpawnBoss()
    {
        if (null == bossPrefab)
        {
            return false;
        }

        if (null == player)
        {
            return false;
        }

        if (null == bossRoom)
        {
            return false;
        }

        Tile spawnTile = FindRoomCenterFloorTile(bossRoom);
        if (null == spawnTile)
        {
            spawnTile = FindRoomFloorTile(bossRoom);
        }

        if (null == spawnTile)
        {
            return false;
        }

        GameObject bossObject = Instantiate(bossPrefab, transform);
        bossObject.name = "Boss";
        bossObject.transform.position = new Vector3(
            spawnTile.rect.x + 0.5f,
            spawnTile.rect.y + 0.5f,
            0.0f
        );

        Boss boss = bossObject.GetComponent<Boss>();
        if (null == boss)
        {
            boss = bossObject.AddComponent<Boss>();
        }

        // 보스가 플레이어를 매 프레임 찾지 않도록 참조를 넘겨준다.
        boss.Init(tileMap, bossObject.transform.position, player, bossRoom.index);
        activeBoss = boss;

        // 보스방은 몬스터가 없어서 RoomCombatSystem이 전투방으로 잡지 않는다.
        // 소환 시점에 직접 잠가 도망치지 못하게 한다.
        if (null != roomCombatSystem)
        {
            roomCombatSystem.LockRoomFromOutside(bossRoom.index);
        }

        return true;
    }

    private Tile FindRoomCenterFloorTile(Block room)
    {
        int centerX = (int)room.rect.center.x;
        int centerY = (int)room.rect.center.y;

        int maxRadius = (int)Mathf.Max(room.rect.width, room.rect.height);

        for (int radius = 0; radius <= maxRadius; radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (radius != Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)))
                    {
                        continue;
                    }

                    Tile tile = tileMap.GetTile(centerX + dx, centerY + dy);
                    if (null == tile)
                    {
                        continue;
                    }

                    if (Tile.Type.Floor != tile.type)
                    {
                        continue;
                    }

                    if (null != tile.door)
                    {
                        continue;
                    }

                    return tile;
                }
            }
        }

        return null;
    }

    private Block FindFarthestRoom(Vector3 fromPosition)
    {
        Block farthest = null;
        float maxDistance = -1.0f;

        foreach (Block room in rooms)
        {
            if (Block.Type.Room != room.type)
            {
                continue;
            }

            Vector2 roomCenter = new Vector2(room.rect.center.x, room.rect.center.y);
            float distance = Vector2.Distance(roomCenter, fromPosition);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = room;
            }
        }

        return farthest;
    }

    private void SpawnMonsters()
    {
        ClearMonsters();

        if (null == player || null == tileMap || 0 == rooms.Count)
        {
            return;
        }

        Block playerRoom = FindCenterRoom();

        HashSet<int> usedTiles = new HashSet<int>();

        foreach (Block room in rooms)
        {
            if (Block.Type.Room != room.type)
            {
                continue;
            }

            if (room == playerRoom || room == bossRoom)
            {
                continue;
            }

            if (UnityEngine.Random.value > monsterRoomSpawnChance)
            {
                continue;
            }

            int minimum = Mathf.Max(1, minimumMonstersPerRoom);
            int maximum = Mathf.Max(minimum, maximumMonstersPerRoom);
            int count = UnityEngine.Random.Range(minimum, maximum + 1);

            for (int i = 0; i < count; i++)
            {
                Tile spawnTile = FindMonsterSpawnTile(room, usedTiles);
                if (null == spawnTile)
                {
                    continue;
                }

                usedTiles.Add(spawnTile.index);

                int monsterType = UnityEngine.Random.Range(1, Mathf.Max(2, monsterTypeCount + 1));

                GameObject monsterObject = new GameObject($"Monster{monsterType}_{monsters.Count}");
                monsterObject.transform.parent = transform;

                SpriteRenderer renderer = monsterObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 19;

                CircleCollider2D collider = monsterObject.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.3f;

                Monster monster = monsterObject.AddComponent<Monster>();
                Vector2 spawnPosition = new Vector2(
                    spawnTile.rect.x + 0.5f,
                    spawnTile.rect.y + 0.5f
                );

                monster.Init(tileMap, spawnPosition, player, monsterType, room.index);
                monsters.Add(monster);
            }
        }

        AssignGuaranteedBossFragments();
    }

    private void ConfigureRoomCombat()
    {
        if (null == roomCombatSystem)
        {
            roomCombatSystem = GetComponent<RoomCombatSystem>();
        }

        if (false == enableRoomCombat)
        {
            if (null != roomCombatSystem)
            {
                roomCombatSystem.enabled = false;
            }
            return;
        }

        if (null == roomCombatSystem)
        {
            roomCombatSystem = gameObject.AddComponent<RoomCombatSystem>();
        }

        Block startRoom = FindCenterRoom();
        if (null == startRoom || null == player)
        {
            roomCombatSystem.enabled = false;
            return;
        }

        roomCombatSystem.Init(
            this,
            player,
            startRoom.index,
            enableIronicRoomRewards
        );
    }

    private void AssignGuaranteedBossFragments()
    {
        // 몬스터가 생성되지 않는 극단적인 경우에도 진행이 막히지 않게 한다.
        if (0 == monsters.Count)
        {
            LootChest.Spawn(
                player.transform.position + Vector3.right,
                PlayerInventory.BossFragmentsRequired
            );
            return;
        }

        // 어느 몬스터가 조각을 갖는지 매번 무작위로 섞되,
        // 던전 전체에서는 반드시 필요한 3개가 나오도록 숨겨 둔다.
        List<Monster> candidates = new List<Monster>(monsters);
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            Monster temporary = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temporary;
        }

        for (int i = 0; i < PlayerInventory.BossFragmentsRequired; i++)
        {
            candidates[i % candidates.Count].AddGuaranteedBossFragment();
        }
    }

    private Tile FindMonsterSpawnTile(Block room, HashSet<int> usedTiles)
    {
        int xMin = (int)room.rect.xMin + 1;
        int xMax = (int)room.rect.xMax - 1;
        int yMin = (int)room.rect.yMin + 1;
        int yMax = (int)room.rect.yMax - 1;

        if (xMin >= xMax || yMin >= yMax)
        {
            return null;
        }

        for (int attempt = 0; attempt < 40; attempt++)
        {
            int x = UnityEngine.Random.Range(xMin, xMax);
            int y = UnityEngine.Random.Range(yMin, yMax);
            Tile tile = tileMap.GetTile(x, y);

            if (null == tile || Tile.Type.Floor != tile.type)
            {
                continue;
            }

            if (null != tile.door
                || PropBlock.IsBlocked(tile.index)
                || usedTiles.Contains(tile.index))
            {
                continue;
            }

            // 플레이어 코앞에서 튀어나오지 않게 한다.
            Vector2 position = new Vector2(tile.rect.x + 0.5f, tile.rect.y + 0.5f);
            if (3.0f > Vector2.Distance(position, player.transform.position))
            {
                continue;
            }

            return tile;
        }

        return null;
    }

    private void ClearMonsters()
    {
        foreach (Monster monster in monsters)
        {
            if (null != monster)
            {
                GameObject.DestroyImmediate(monster.gameObject);
            }
        }

        monsters.Clear();
    }

    private Block FindCenterRoom()
    {
        if (0 == rooms.Count)
        {
            return null;
        }

        Vector2 center = new Vector2(tileMap.width / 2.0f, tileMap.height / 2.0f);

        Block nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Block room in rooms)
        {
            float distance = Vector2.Distance(room.rect.center, center);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = room;
            }
        }

        return nearest;
    }

    private Sprite CreateSquareSprite(Color color)
    {
        Texture2D texture = new Texture2D(16, 16);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    private void RepositionBlocks()
    {
        Vector2 center = Vector2.zero;
        foreach (Block block in blocks)
        {
            center += block.rect.center;
        }
        center /= blocks.Count;

        int safety = 0;
        while (safety++ < 10000)
        {
            bool overlap = false;

            for (int i = 0; i < blocks.Count; i++)
            {
                for (int j = i + 1; j < blocks.Count; j++)
                {
                    if (true == IsOverlap(blocks[i], blocks[j]))
                    {
                        ResolveOverlap(center, blocks[i], blocks[j]);
                        overlap = true;
                    }
                }
            }

            if (false == overlap)
            {
                break;
            }
        }
    }

    private bool IsOverlap(Block block1, Block block2)
    {
        int padding = roomPadding;
        if (Block.Type.Corridor == block1.type || Block.Type.Corridor == block2.type)
        {
            padding = 1;
        }

        Rect expanded = new Rect(
            block1.rect.x - padding,
            block1.rect.y - padding,
            block1.rect.width + padding * 2,
            block1.rect.height + padding * 2
        );

        return expanded.Overlaps(block2.rect);
    }

    private void ResolveOverlap(Vector2 center, Block block1, Block block2)
    {
        int dx = (int)Mathf.Min(
            Mathf.Abs(block1.rect.x + block1.rect.width - block2.rect.x),
            Mathf.Abs(block2.rect.x + block2.rect.width - block1.rect.x)
        );
        int dy = (int)Mathf.Min(
            Mathf.Abs(block1.rect.y + block1.rect.height - block2.rect.y),
            Mathf.Abs(block2.rect.y + block2.rect.height - block1.rect.y)
        );

        if (dx < dy)
        {
            if (block1.rect.x < block2.rect.x)
            {
                if (center.x < block2.rect.x)
                {
                    block2.rect.x += 1;
                }
                else
                {
                    block1.rect.x -= 1;
                }
            }
            else
            {
                if (center.x < block1.rect.x)
                {
                    block1.rect.x += 1;
                }
                else
                {
                    block2.rect.x -= 1;
                }
            }
        }
        else
        {
            if (block1.rect.y < block2.rect.y)
            {
                if (center.y < block2.rect.y)
                {
                    block2.rect.y += 1;
                }
                else
                {
                    block1.rect.y -= 1;
                }
            }
            else
            {
                if (center.y < block1.rect.y)
                {
                    block1.rect.y += 1;
                }
                else
                {
                    block2.rect.y -= 1;
                }
            }
        }
    }
}
