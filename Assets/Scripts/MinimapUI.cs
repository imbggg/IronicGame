using UnityEngine;

public class MinimapUI : MonoBehaviour
{
    [SerializeField, Range(0.2f, 0.4f)] private float screenHeightRatio = 0.3f;
    [SerializeField] private float minimumPanelSize = 280.0f;
    [SerializeField] private float maximumPanelSize = 380.0f;
    [SerializeField] private float screenMargin = 18.0f;
    [SerializeField] private float markerRefreshInterval = 0.15f;

    private static MinimapUI instance;

    private DungeonGenerator generator;
    private TitleMenu titleMenu;
    private TileMap cachedTileMap;
    private Texture2D mapTexture;
    private Player player;
    private Boss boss;
    private Monster[] monsters = new Monster[0];
    private float refreshRemaining;
    private bool visible = true;

    private readonly Color32 emptyColor = new Color32(0, 0, 0, 0);
    private readonly Color32 floorColor = new Color32(59, 39, 77, 255);
    private readonly Color32 wallColor = new Color32(139, 112, 151, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAutomatically()
    {
        if (null != FindAnyObjectByType<MinimapUI>())
        {
            return;
        }

        GameObject minimapObject = new GameObject("MinimapUI");
        minimapObject.AddComponent<MinimapUI>();
    }

    private void Awake()
    {
        if (null != instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (null != mapTexture)
        {
            Destroy(mapTexture);
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            visible = false == visible;
        }

        // 타이틀 메뉴가 같은 씬에서 OnGUI로 덮여 그려지기 때문에
        // 던전이 이미 생성돼 있어도 미니맵은 숨겨야 한다.
        titleMenu = FindAnyObjectByType<TitleMenu>();

        if (null == generator)
        {
            generator = FindAnyObjectByType<DungeonGenerator>();
        }

        if (null == generator)
        {
            cachedTileMap = null;
            player = null;
            boss = null;
            monsters = new Monster[0];
            return;
        }

        if (null == generator.tileMap)
        {
            return;
        }

        if (cachedTileMap != generator.tileMap)
        {
            cachedTileMap = generator.tileMap;
            BuildStaticMap();
            RefreshMarkers();
        }

        refreshRemaining -= Time.unscaledDeltaTime;
        if (0.0f >= refreshRemaining)
        {
            RefreshMarkers();
            refreshRemaining = markerRefreshInterval;
        }
    }

    private void BuildStaticMap()
    {
        if (null == cachedTileMap || 0 >= cachedTileMap.width || 0 >= cachedTileMap.height)
        {
            return;
        }

        if (null != mapTexture)
        {
            Destroy(mapTexture);
        }

        mapTexture = new Texture2D(cachedTileMap.width, cachedTileMap.height, TextureFormat.RGBA32, false);
        mapTexture.name = "RuntimeMinimap";
        mapTexture.filterMode = FilterMode.Point;
        mapTexture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[cachedTileMap.width * cachedTileMap.height];
        for (int y = 0; y < cachedTileMap.height; y++)
        {
            for (int x = 0; x < cachedTileMap.width; x++)
            {
                Tile tile = cachedTileMap.GetTile(x, y);
                Color32 color = emptyColor;

                if (null != tile)
                {
                    if (Tile.Type.Floor == tile.type)
                    {
                        color = floorColor;
                    }
                    else if (Tile.Type.Wall == tile.type)
                    {
                        color = wallColor;
                    }
                }

                pixels[y * cachedTileMap.width + x] = color;
            }
        }

        mapTexture.SetPixels32(pixels);
        mapTexture.Apply(false, false);
    }

    private void RefreshMarkers()
    {
        if (null == player)
        {
            player = FindAnyObjectByType<Player>();
        }

        monsters = FindObjectsByType<Monster>();
        boss = FindAnyObjectByType<Boss>();
    }

    private void OnGUI()
    {
        if (false == visible || null == cachedTileMap || null == mapTexture)
        {
            return;
        }

        if (null != titleMenu && true == titleMenu.enabled)
        {
            return;
        }

        if (true == GameEndUI.IsShowing)
        {
            return;
        }

        float panelSize = Mathf.Clamp(
            Screen.height * screenHeightRatio,
            minimumPanelSize,
            maximumPanelSize
        );
        float headerHeight = 32.0f;
        float availableWidth = panelSize - 16.0f;
        float availableHeight = panelSize - headerHeight - 12.0f;
        float scale = Mathf.Min(
            availableWidth / cachedTileMap.width,
            availableHeight / cachedTileMap.height
        );

        float mapWidth = cachedTileMap.width * scale;
        float mapHeight = cachedTileMap.height * scale;
        float panelX = Screen.width - panelSize - screenMargin;
        float panelY = screenMargin;
        Rect panelRect = new Rect(panelX, panelY, panelSize, panelSize);
        Rect mapRect = new Rect(
            panelX + (panelSize - mapWidth) * 0.5f,
            panelY + headerHeight + (availableHeight - mapHeight) * 0.5f,
            mapWidth,
            mapHeight
        );

        DrawSolidRect(panelRect, new Color(0.025f, 0.015f, 0.045f, 0.88f));
        DrawBorder(panelRect, 2.0f, new Color(0.58f, 0.43f, 0.68f, 1.0f));

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.9f, 0.82f, 0.95f, 1.0f);
        GUI.Label(new Rect(panelX, panelY + 2.0f, panelSize, 28.0f), "지도  [M]", titleStyle);

        GUI.DrawTexture(mapRect, mapTexture, ScaleMode.StretchToFill, true);
        DrawDoors(mapRect);
        DrawMonsters(mapRect);

        if (null != boss)
        {
            DrawMarker(boss.transform.position, mapRect, 11.0f, new Color(1.0f, 0.72f, 0.05f, 1.0f));
            DrawMarker(boss.transform.position, mapRect, 5.0f, new Color(0.85f, 0.08f, 0.08f, 1.0f));
        }

        if (null != player)
        {
            DrawMarker(player.transform.position, mapRect, 10.0f, new Color(0.2f, 1.0f, 0.35f, 1.0f));
            DrawMarker(player.transform.position, mapRect, 4.0f, Color.white);
        }
    }

    private void DrawDoors(Rect mapRect)
    {
        if (null == generator || null == generator.doors)
        {
            return;
        }

        foreach (Door door in generator.doors)
        {
            if (null == door || null == door.tile)
            {
                continue;
            }

            Color color = Door.State.Open == door.state
                ? new Color(1.0f, 0.82f, 0.25f, 0.9f)
                : new Color(1.0f, 0.45f, 0.08f, 1.0f);

            Vector2 position = new Vector2(door.tile.rect.center.x, door.tile.rect.center.y);
            DrawMarker(position, mapRect, 4.0f, color);
        }
    }

    private void DrawMonsters(Rect mapRect)
    {
        if (null == monsters)
        {
            return;
        }

        foreach (Monster monster in monsters)
        {
            if (null != monster)
            {
                DrawMarker(monster.transform.position, mapRect, 6.0f, new Color(1.0f, 0.18f, 0.18f, 1.0f));
            }
        }
    }

    private void DrawMarker(Vector2 worldPosition, Rect mapRect, float size, Color color)
    {
        float normalizedX = Mathf.Clamp01(worldPosition.x / cachedTileMap.width);
        float normalizedY = Mathf.Clamp01(worldPosition.y / cachedTileMap.height);
        float x = mapRect.x + normalizedX * mapRect.width;
        float y = mapRect.yMax - normalizedY * mapRect.height;

        DrawSolidRect(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), color);
    }

    private void DrawBorder(Rect rect, float thickness, Color color)
    {
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawSolidRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawSolidRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawSolidRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawSolidRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
