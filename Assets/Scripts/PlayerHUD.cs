using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    // The same reference resolution as the title menus keeps text readable in QHD.
    private const float Width = 380;
    private const int PotionsPerRow = 9;
    [SerializeField, Range(0.55f, 1.1f)]
    [Tooltip("플레이어 HUD 전체 크기입니다. 0.75는 기존 크기의 75%이며, 작게 조절하면 게임 화면을 더 넓게 볼 수 있습니다.")]
    private float hudScale = 0.75f;

    private static PlayerHUD instance;
    private DungeonGenerator generator;
    private TitleMenu titleMenu;
    private Player player;
    private GUIStyle textStyle;
    private Texture2D potionIcon, emptyPotionIcon, fragmentIcon, emptyFragmentIcon, heartIcon, clockIcon;

    private static readonly Color Ink = new Color32(224, 211, 224, 255);
    private static readonly Color Muted = new Color32(157, 140, 162, 255);
    private static readonly Color Red = new Color32(192, 68, 96, 255);
    private static readonly Color Violet = new Color32(163, 138, 207, 255);
    private static readonly Color Gold = new Color32(208, 173, 105, 255);
    private static readonly Color Good = new Color32(171, 218, 137, 255);
    private static readonly Color Bad = new Color32(241, 115, 119, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAutomatically()
    {
        if (FindAnyObjectByType<PlayerHUD>() != null) return;
        new GameObject("PlayerHUD").AddComponent<PlayerHUD>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        DestroyIcon(potionIcon);
        DestroyIcon(emptyPotionIcon);
        DestroyIcon(fragmentIcon);
        DestroyIcon(emptyFragmentIcon);
        DestroyIcon(heartIcon);
        DestroyIcon(clockIcon);
    }

    private void Update()
    {
        titleMenu = FindAnyObjectByType<TitleMenu>();
        if (titleMenu != null && titleMenu.enabled) { player = null; return; }
        if (generator == null) generator = FindAnyObjectByType<DungeonGenerator>();
        if (generator == null) { player = null; return; }
        if (player == null) player = FindAnyObjectByType<Player>();

        // OnGUI runs more than once per frame; the timer must stay in Update.
        if (player != null && !player.IsDead) GameData.elapsedTime += Time.deltaTime;
    }

    private void OnGUI()
    {
        if (player == null || generator == null || (titleMenu != null && titleMenu.enabled)) return;
        if (GameEndUI.IsShowing || IronicRewardUI.IsShowing || Time.timeScale <= 0) return;

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        try
        {
            EnsureStyle();
            GUI.color = Color.white;
            GUI.depth = 10;
            int capacity = player.Inventory != null ? player.Inventory.ItemCapacity : PotionsPerRow;
            int potionRows = Mathf.CeilToInt(capacity / (float)PotionsPerRow);
            float extraHeight = Mathf.Max(0, potionRows - 1) * 40;
            float totalHeight = 442 + extraHeight;
            float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f))
                * Mathf.Clamp(hudScale, 0.55f, 1.1f);
            scale = Mathf.Min(scale, Screen.height / (totalHeight + 48));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

            const float x = 24;
            const float y = 24;
            DrawVitals(x, y);
            if (player.Inventory != null)
            {
                DrawInventory(x, y + 170, player.Inventory, extraHeight);
                DrawStats(x, y + 340 + extraHeight);
            }
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }

    private void DrawVitals(float x, float y)
    {
        Plate(new Rect(x, y, Width, 158));
        Icon(clockIcon, new Rect(x + 16, y + 14, 20, 20));
        Text(new Rect(x + 44, y + 10, 190, 28), "경과  " + GameData.FormatElapsedTime(), 19, Ink);
        Text(new Rect(x + 254, y + 10, 108, 28), GameData.GetDifficultyName(), 18, Muted, TextAnchor.MiddleRight);

        Icon(heartIcon, new Rect(x + 16, y + 51, 24, 24));
        Text(new Rect(x + 49, y + 46, 100, 30), "체력", 21, Ink);
        bool lowHealth = player.CurrentHealth <= player.MaxHealth * 0.3f;
        Text(new Rect(x + 170, y + 44, 192, 34), $"{player.CurrentHealth} / {player.MaxHealth}", 24,
            lowHealth ? Bad : Ink, TextAnchor.MiddleRight);
        float health = player.MaxHealth > 0 ? player.CurrentHealth / (float)player.MaxHealth : 0;
        Bar(new Rect(x + 18, y + 82, Width - 36, 12), health, Red);

        Key(new Rect(x + 18, y + 108, 24, 24), "X", Violet);
        Text(new Rect(x + 51, y + 103, 150, 32), "특수공격", 20, Ink);
        bool ready = player.SkillCooldownRemaining <= 0;
        string status = ready ? "사용 가능" : $"{player.SkillCooldownRemaining:0.0}초";
        Text(new Rect(x + 220, y + 103, 142, 32), status, 20, ready ? Good : Violet, TextAnchor.MiddleRight);
        Bar(new Rect(x + 18, y + 140, Width - 36, 5), 1 - player.SkillCooldownRatio, ready ? Violet : Muted);
    }

    private void DrawInventory(float x, float y, PlayerInventory inventory, float extraHeight)
    {
        Plate(new Rect(x, y, Width, 158 + extraHeight));
        Key(new Rect(x + 18, y + 12, 24, 24), "1", Red);
        Text(new Rect(x + 51, y + 8, 184, 32), "체력 포션", 20, Ink);
        Text(new Rect(x + 243, y + 8, 119, 32), $"체력 +{inventory.HealthPotionHealAmount} 회복", 16, Muted, TextAnchor.MiddleRight);

        // One bright bottle per potion; empty glass is an unused inventory slot.
        // Additional rows support a changed capacity without hiding any items.
        for (int i = 0; i < inventory.ItemCapacity; i++)
        {
            float px = x + 20 + (i % PotionsPerRow) * 38;
            float py = y + 43 + (i / PotionsPerRow) * 40;
            Icon(i < inventory.HealthPotionCount ? potionIcon : emptyPotionIcon, new Rect(px, py, 28, 36));
        }

        float lineY = y + 90 + extraHeight;
        Fill(new Rect(x + 18, lineY, Width - 36, 1), new Color32(70, 52, 75, 150));
        Key(new Rect(x + 18, lineY + 14, 24, 24), "2", Gold);
        Text(new Rect(x + 51, lineY + 9, 226, 34), "보스 소환석 조각", 19, Ink);
        for (int i = 0; i < PlayerInventory.BossFragmentsRequired; i++)
        {
            Icon(i < inventory.BossFragmentCount ? fragmentIcon : emptyFragmentIcon,
                new Rect(x + 281 + i * 27, lineY + 12, 22, 28));
        }
        bool ready = inventory.BossFragmentCount >= PlayerInventory.BossFragmentsRequired;
        Text(new Rect(x + 18, lineY + 40, 250, 26),
            ready ? "보스방 제단에서 [2] 소환" : "조각 3개를 모아 제단으로", 16, ready ? Gold : Muted);
        Text(new Rect(x + 273, lineY + 39, 89, 28),
            $"{inventory.BossFragmentCount} / {PlayerInventory.BossFragmentsRequired}", 18, ready ? Gold : Muted, TextAnchor.MiddleRight);
    }

    private void DrawStats(float x, float y)
    {
        Plate(new Rect(x, y, Width, 102));
        float columnWidth = (Width - 36) / 3;
        Stat(x + 18, y + 8, columnWidth, "이동 속도", $"{player.moveSpeed:0.0}");
        Stat(x + 18 + columnWidth, y + 8, columnWidth, "공격력", $"x{player.AttackPowerMultiplier:0.00}");
        Stat(x + 18 + columnWidth * 2, y + 8, columnWidth, "특수 대기", $"{player.SkillCooldownDuration:0.0}초");
        Fill(new Rect(x + 18, y + 64, Width - 36, 1), new Color32(70, 52, 75, 150));
        bool changed = !string.IsNullOrEmpty(player.LastIronicModifierDescription);
        Text(new Rect(x + 18, y + 70, 79, 25), "최근 변화", 15, Muted);
        string change = changed ? player.LastIronicModifierDescription : "아직 없음";
        Color changeColor = changed ? (player.LastIronicModifierWasPositive ? Good : Bad) : Muted;
        Text(new Rect(x + 107, y + 68, 255, 29), change, 18, changeColor, TextAnchor.MiddleRight);
    }

    private void Stat(float x, float y, float width, string label, string value)
    {
        Text(new Rect(x, y, width, 22), label, 16, Muted, TextAnchor.MiddleCenter);
        Text(new Rect(x, y + 22, width, 31), value, 23, Ink, TextAnchor.MiddleCenter);
    }

    private void EnsureStyle()
    {
        if (textStyle != null) return;
        Font font = Resources.Load<Font>("Fonts/Galmuri11-Bold");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textStyle = new GUIStyle
        {
            font = font, fontStyle = FontStyle.Normal, wordWrap = false,
            clipping = TextClipping.Clip, padding = new RectOffset(), margin = new RectOffset()
        };

        // Code-native pixel icons avoid extra asset dependencies and remain crisp.
        string[] bottle = {
            "....cccc....", "....cCCc....", "....oooo....", "....oggo....",
            "...oggggo...", "..oghggggo..", ".oghggggggo.", ".oghrrrrrgo.",
            ".ogrhrrrrgo.", ".ogrhrrrrgo.", ".ogrrrrrrgo.", ".ogrrrrrdgo.",
            ".ogrrrrddgo.", "..oggggggo..", "...oooooo...", "............"
        };
        potionIcon = PixelIcon(bottle, false);
        emptyPotionIcon = PixelIcon(bottle, true);
        string[] crystal = {
            ".....o......", "....oho.....", "...ohhgo....", "..ohhgggo...",
            "..ohggdddo..", ".ohggdddgo..", ".ohggddgo...", "..ogddgo....",
            "...odgo.....", "....oo......", "............", "............"
        };
        fragmentIcon = PixelIcon(crystal, false, true);
        emptyFragmentIcon = PixelIcon(crystal, true, true);
        heartIcon = PixelIcon(new[] {
            "............", "..ooo..ooo..", ".ohhroorrrro", ".ohrrrrrrrro",
            ".orrrrrrrrdo", "..orrrrrrdo.", "...orrrrdo..", "....orrdo...",
            ".....odo....", "......o.....", "............", "............"
        }, false);
        clockIcon = PixelIcon(new[] {
            "....oooo....", "..ooggggoo..", ".oggggggggo.", ".ogggoggggo.",
            "oggggogggggo", "oggggohggggo", "oggggohhgggo", "oggggggggggo",
            ".oggggggggo.", ".oggggggggo.", "..ooggggoo..", "....oooo...."
        }, false, true);
    }

    private void Text(Rect rect, string text, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        textStyle.fontSize = size;
        textStyle.alignment = alignment;
        GUIContent content = new GUIContent(text);
        // Protect longer localized roulette descriptions without clipping.
        while (textStyle.fontSize > 13 && textStyle.CalcSize(content).x > rect.width) textStyle.fontSize--;
        textStyle.normal.textColor = new Color(0.02f, 0.01f, 0.03f, 0.9f);
        GUI.Label(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), content, textStyle);
        textStyle.normal.textColor = color;
        GUI.Label(rect, content, textStyle);
    }

    private void Key(Rect rect, string key, Color accent)
    {
        Fill(rect, new Color32(48, 33, 51, 235));
        Fill(new Rect(rect.x, rect.yMax - 2, rect.width, 2), accent);
        Text(rect, key, 17, Ink, TextAnchor.MiddleCenter);
    }

    private static void Plate(Rect rect)
    {
        Fill(new Rect(rect.x + 3, rect.y + 4, rect.width, rect.height), new Color(0.02f, 0.01f, 0.03f, 0.32f));
        Fill(rect, new Color32(20, 14, 26, 224));
        // A muted accent replaces the old bright rectangular border.
        Fill(new Rect(rect.x + 18, rect.y, rect.width - 36, 1), new Color32(122, 88, 115, 105));
    }

    private static void Bar(Rect rect, float value, Color color)
    {
        Fill(rect, new Color32(54, 34, 51, 255));
        float width = rect.width * Mathf.Clamp01(value);
        if (width <= 0) return;
        Fill(new Rect(rect.x, rect.y, width, rect.height), color);
        Fill(new Rect(rect.x, rect.y, width, Mathf.Min(2, rect.height)), Color.Lerp(color, Color.white, 0.25f));
    }

    private static void Icon(Texture2D texture, Rect rect)
    {
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static Texture2D PixelIcon(string[] rows, bool empty, bool gold = false)
    {
        int width = rows[0].Length;
        var texture = new Texture2D(width, rows.Length, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var pixels = new Color32[width * rows.Length];
        for (int y = 0; y < rows.Length; y++)
        for (int x = 0; x < width; x++)
        {
            char pixel = x < rows[y].Length ? rows[y][x] : '.';
            Color32 color;
            switch (pixel)
            {
                case 'o': color = empty ? new Color32(72, 61, 80, 255) : new Color32(43, 26, 42, 255); break;
                case 'c': color = new Color32(106, 68, 63, 255); break;
                case 'C': color = new Color32(178, 130, 91, 255); break;
                case 'g': color = gold ? new Color32(206, 151, 80, 255) : new Color32(151, 135, 167, 255); break;
                case 'h': color = gold ? new Color32(255, 224, 158, 255) : new Color32(242, 210, 224, 255); break;
                case 'r': color = new Color32(201, 54, 90, 255); break;
                case 'd': color = gold ? new Color32(128, 83, 53, 255) : new Color32(123, 36, 68, 255); break;
                default: color = new Color32(0, 0, 0, 0); break;
            }
            if (empty && pixel != '.' && pixel != 'o')
                color = pixel == 'h' || pixel == 'g' ? new Color32(87, 74, 97, 165) : new Color32(41, 30, 49, 140);
            pixels[(rows.Length - 1 - y) * width + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static void DestroyIcon(Texture2D icon)
    {
        if (icon != null) Destroy(icon);
    }
}
