using UnityEngine;

/// <summary>
/// 보스방에 놓이는 제단. 평소에는 Idle을 반복 재생하고,
/// 보스 소환석 조각 3개를 사용하면 사라지는 연출 후 보스가 등장한다.
/// 프리팹 없이 DungeonGenerator가 코드로 생성한다.
/// </summary>
public class BossAltar : MonoBehaviour
{
    private const string IdlePath = "Effects/Altar_Idle";
    private const string DisappearPath = "Effects/Altar_Disappear";

    private const int FrameWidth = 200;
    private const int FrameHeight = 220;
    private const int IdleFrameCount = 6;
    private const int DisappearFrameCount = 13;

    // 제단 바닥이 셀 밑면으로부터 3px 위에 있다.
    private const float PivotY = 3.0f / FrameHeight;

    private const float IdleFps = 8.0f;
    private const float DisappearFps = 14.0f;

    /// <summary>플레이어가 이 거리 안에 있어야 소환할 수 있다.</summary>
    public const float UseDistance = 2.0f;

    /// <summary>다른 UI와 같은 폰트를 쓴다.</summary>
    private const string FontPath = "Fonts/Galmuri11-Bold";
    private const float ReferenceHeight = 900.0f;

    private static Sprite[] idleFrames;
    private static Sprite[] disappearFrames;

    private GUIStyle promptStyle;

    private SpriteRenderer spriteRenderer;
    private Player player;

    private bool disappearing;
    private float elapsed;
    private System.Action onDisappeared;

    public static BossAltar Instance { get; private set; }

    public bool IsBusy { get { return disappearing; } }

    public static void Spawn(Vector3 position, float pixelsPerUnit)
    {
        GameObject altarObject = new GameObject("BossAltar");
        altarObject.transform.position = new Vector3(position.x, position.y, 0.0f);

        BossAltar altar = altarObject.AddComponent<BossAltar>();
        altar.Setup(pixelsPerUnit);
    }

    private void Setup(float pixelsPerUnit)
    {
        Instance = this;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 17;

        LoadFrames(pixelsPerUnit);
        ApplyFrame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LoadFrames(float pixelsPerUnit)
    {
        if (null == idleFrames || 0 == idleFrames.Length)
        {
            idleFrames = Slice(IdlePath, IdleFrameCount, pixelsPerUnit);
        }

        if (null == disappearFrames || 0 == disappearFrames.Length)
        {
            disappearFrames = Slice(DisappearPath, DisappearFrameCount, pixelsPerUnit);
        }
    }

    private Sprite[] Slice(string resourcePath, int expectedCount, float pixelsPerUnit)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (null == texture)
        {
            Debug.LogError($"[BossAltar] Cannot load texture: Resources/{resourcePath}.png");
            return new Sprite[0];
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int count = Mathf.Min(expectedCount, texture.width / FrameWidth);
        Sprite[] frames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                texture,
                new Rect(i * FrameWidth, 0, FrameWidth, FrameHeight),
                new Vector2(0.5f, PivotY),
                pixelsPerUnit
            );
            frames[i].name = $"{texture.name}_{i}";
        }

        return frames;
    }

    /// <summary>
    /// 플레이어가 제단 앞에 서 있는지.
    /// </summary>
    public bool IsPlayerNear(Player target)
    {
        if (null == target)
        {
            return false;
        }

        return Vector2.Distance(transform.position, target.transform.position) <= UseDistance;
    }

    /// <summary>
    /// 사라지는 연출을 시작한다. 연출이 끝나면 onFinished가 호출된다.
    /// </summary>
    public bool BeginDisappear(System.Action onFinished)
    {
        if (true == disappearing)
        {
            return false;
        }

        disappearing = true;
        elapsed = 0.0f;
        onDisappeared = onFinished;
        return true;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (null == player)
        {
            player = FindAnyObjectByType<Player>();
        }

        ApplyFrame();

        if (false == disappearing)
        {
            return;
        }

        if (elapsed >= DisappearFrameCount / DisappearFps)
        {
            System.Action callback = onDisappeared;
            onDisappeared = null;

            Destroy(gameObject);

            if (null != callback)
            {
                callback();
            }
        }
    }

    private void ApplyFrame()
    {
        Sprite[] frames = true == disappearing ? disappearFrames : idleFrames;
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        float fps = true == disappearing ? DisappearFps : IdleFps;
        int index = Mathf.FloorToInt(elapsed * fps);

        if (true == disappearing)
        {
            index = Mathf.Min(index, frames.Length - 1);
        }
        else
        {
            index %= frames.Length;
        }

        spriteRenderer.sprite = frames[index];
    }

    private void EnsurePromptStyle()
    {
        if (null != promptStyle)
        {
            return;
        }

        Font font = Resources.Load<Font>(FontPath);
        if (null == font)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        promptStyle = new GUIStyle();
        promptStyle.font = font;
        promptStyle.fontStyle = FontStyle.Normal;
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.wordWrap = false;
        promptStyle.clipping = TextClipping.Overflow;
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void OnGUI()
    {
        if (true == disappearing || null == player || null == Camera.main)
        {
            return;
        }

        if (false == IsPlayerNear(player) || true == player.IsDead)
        {
            return;
        }

        if (true == GameEndUI.IsShowing)
        {
            return;
        }

        PlayerInventory inventory = player.Inventory;
        if (null == inventory)
        {
            return;
        }

        EnsurePromptStyle();

        int have = inventory.BossFragmentCount;
        int need = PlayerInventory.BossFragmentsRequired;
        bool ready = have >= need;

        string text = ready
            ? "[2]  보스 소환"
            : $"보스 소환석 조각  {have} / {need}";

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(
            transform.position + Vector3.up * 2.2f
        );

        if (0.0f >= screenPosition.z)
        {
            return;
        }

        screenPosition.y = Screen.height - screenPosition.y;

        float scale = Mathf.Max(0.6f, Screen.height / ReferenceHeight);
        promptStyle.fontSize = Mathf.RoundToInt(20.0f * scale);

        Vector2 textSize = promptStyle.CalcSize(new GUIContent(text));
        float boxWidth = textSize.x + 44.0f * scale;
        float boxHeight = textSize.y + 22.0f * scale;

        Rect box = new Rect(
            Mathf.Clamp(screenPosition.x - boxWidth * 0.5f, 6.0f,
                Mathf.Max(6.0f, Screen.width - boxWidth - 6.0f)),
            Mathf.Clamp(screenPosition.y - boxHeight - 8.0f, 6.0f,
                Mathf.Max(6.0f, Screen.height - boxHeight - 6.0f)),
            boxWidth,
            boxHeight
        );

        // 준비되면 은은하게 숨 쉬듯 밝아진다.
        float pulse = ready
            ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.0f)
            : 0.0f;

        Color edge = ready
            ? Color.Lerp(new Color32(150, 108, 46, 255), new Color32(255, 214, 110, 255), pulse)
            : new Color32(92, 62, 92, 255);

        Fill(new Rect(box.x, box.y + 3.0f * scale, box.width, box.height),
            new Color(0.0f, 0.0f, 0.0f, 0.35f));
        Fill(box, new Color32(18, 11, 24, 242));

        float thickness = Mathf.Max(1.0f, 2.0f * scale);
        Fill(new Rect(box.x, box.y, box.width, thickness), edge);
        Fill(new Rect(box.x, box.yMax - thickness, box.width, thickness), edge);
        Fill(new Rect(box.x, box.y, thickness, box.height), edge);
        Fill(new Rect(box.xMax - thickness, box.y, thickness, box.height), edge);

        promptStyle.normal.textColor = new Color(0.03f, 0.01f, 0.04f, 0.9f);
        GUI.Label(new Rect(box.x + 1.0f, box.y + 2.0f, box.width, box.height), text, promptStyle);

        promptStyle.normal.textColor = ready
            ? new Color(1.0f, 0.87f, 0.35f, 1.0f)
            : new Color(0.80f, 0.78f, 0.86f, 1.0f);
        GUI.Label(box, text, promptStyle);
    }
}
