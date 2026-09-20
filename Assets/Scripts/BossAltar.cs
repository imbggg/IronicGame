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

    private static Sprite[] idleFrames;
    private static Sprite[] disappearFrames;

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

        PlayerInventory inventory = player.Inventory;
        if (null == inventory)
        {
            return;
        }

        int have = inventory.BossFragmentCount;
        int need = PlayerInventory.BossFragmentsRequired;

        string text = have >= need
            ? "[2] 보스 소환"
            : $"보스 소환석 조각 {have} / {need}";

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(
            transform.position + Vector3.up * 2.2f
        );

        if (0.0f >= screenPosition.z)
        {
            return;
        }

        screenPosition.y = Screen.height - screenPosition.y;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.020f), 10, 26);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = false;
        style.clipping = TextClipping.Overflow;
        style.normal.textColor = have >= need
            ? new Color(1.0f, 0.87f, 0.35f, 1.0f)
            : new Color(0.80f, 0.78f, 0.86f, 1.0f);

        Vector2 textSize = style.CalcSize(new GUIContent(text));
        float labelWidth = Mathf.Max(240.0f, textSize.x + 40.0f);
        float labelHeight = Mathf.Max(56.0f, textSize.y + 26.0f);

        Rect labelRect = new Rect(
            Mathf.Clamp(screenPosition.x - labelWidth * 0.5f, 6.0f,
                Mathf.Max(6.0f, Screen.width - labelWidth - 6.0f)),
            Mathf.Clamp(screenPosition.y - labelHeight - 8.0f, 6.0f,
                Mathf.Max(6.0f, Screen.height - labelHeight - 6.0f)),
            labelWidth,
            labelHeight
        );

        Color previousColor = GUI.color;
        GUI.color = new Color(0.05f, 0.02f, 0.08f, 0.88f);
        GUI.DrawTexture(labelRect, Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(labelRect, text, style);
    }
}
