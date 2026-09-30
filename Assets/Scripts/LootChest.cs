using UnityEngine;

public class LootChest : MonoBehaviour
{
    private const float OpenDistance = 1.25f;
    private const float MessageDuration = 1.1f;
    private const float HealthPotionChance = 0.30f;
    private const float BossFragmentChance = 0.03f;

    /// <summary>다른 UI와 같은 폰트를 쓴다.</summary>
    private const string FontPath = "Fonts/Galmuri11-Bold";
    private const float ReferenceHeight = 900.0f;

    private static readonly Color PromptColor = new Color32(226, 186, 206, 255);
    private static readonly Color PotionColor = new Color32(140, 216, 130, 255);
    private static readonly Color FragmentColor = new Color32(255, 214, 110, 255);
    private static readonly Color EmptyColor = new Color32(151, 132, 158, 255);
    private static readonly Color FullColor = new Color32(241, 115, 119, 255);

    private static Sprite closedSprite;
    private static Sprite openSprite;

    private Player player;
    private SpriteRenderer spriteRenderer;
    private bool opened;
    private float openedElapsed;
    private int guaranteedBossFragmentCount;
    private string message = string.Empty;
    private Color messageColor = PromptColor;
    private GUIStyle labelStyle;

    public static void Spawn(Vector3 position, int guaranteedFragments = 0)
    {
        GameObject chestObject = new GameObject("LootChest");
        chestObject.transform.position = new Vector3(position.x, position.y, 0.0f);

        LootChest chest = chestObject.AddComponent<LootChest>();
        chest.guaranteedBossFragmentCount = Mathf.Max(0, guaranteedFragments);
        chest.CreateVisual();
    }

    private void CreateVisual()
    {
        CreateSpritesIfNeeded();

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = closedSprite;
        spriteRenderer.sortingOrder = 18;
    }

    private void Update()
    {
        if (true == opened)
        {
            openedElapsed += Time.deltaTime;
            if (openedElapsed >= MessageDuration)
            {
                Destroy(gameObject);
            }
            return;
        }

        if (null == player)
        {
            player = FindAnyObjectByType<Player>();
        }

        if (null == player || player.IsDead || Time.timeScale <= 0.0f)
        {
            return;
        }

        if (Vector2.Distance(transform.position, player.transform.position) <= OpenDistance
            && true == Input.GetKeyDown(KeyCode.E))
        {
            TryOpen();
        }
    }

    private void TryOpen()
    {
        PlayerInventory inventory = player.Inventory;
        if (null == inventory)
        {
            return;
        }

        float rewardRoll = Random.value;
        bool guaranteedFragment = 0 < guaranteedBossFragmentCount;

        if (false == guaranteedFragment
            && rewardRoll >= BossFragmentChance + HealthPotionChance)
        {
            opened = true;
            openedElapsed = 0.0f;
            spriteRenderer.sprite = openSprite;
            message = "꽝!";
            messageColor = EmptyColor;
            RetroAudio.Play(RetroSound.ChestOpen);
            return;
        }

        InventoryItemType itemType = true == guaranteedFragment
            || rewardRoll < BossFragmentChance
            ? InventoryItemType.BossSummonStoneFragment
            : InventoryItemType.HealthPotion;

        int rewardAmount = true == guaranteedFragment
            ? guaranteedBossFragmentCount
            : 1;

        if (false == inventory.TryAddItem(itemType, rewardAmount))
        {
            message = "인벤토리가 가득 참";
            messageColor = FullColor;
            return;
        }

        opened = true;
        openedElapsed = 0.0f;
        spriteRenderer.sprite = openSprite;
        bool isPotion = InventoryItemType.HealthPotion == itemType;

        message = isPotion
            ? "체력 포션 획득!"
            : 1 < rewardAmount
                ? $"보스 소환석 조각 x{rewardAmount}"
                : "보스 소환석 조각 획득!";

        messageColor = isPotion ? PotionColor : FragmentColor;

        RetroAudio.Play(RetroSound.ChestOpen);
    }

    private void EnsureLabelStyle()
    {
        if (null != labelStyle)
        {
            return;
        }

        Font font = Resources.Load<Font>(FontPath);
        if (null == font)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        labelStyle = new GUIStyle();
        labelStyle.font = font;
        labelStyle.fontStyle = FontStyle.Normal;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.wordWrap = false;
        labelStyle.clipping = TextClipping.Overflow;
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
        if (null == Camera.main || null == player)
        {
            return;
        }

        if (true == GameEndUI.IsShowing)
        {
            return;
        }

        bool nearby = Vector2.Distance(transform.position, player.transform.position) <= OpenDistance;
        if (false == nearby && false == opened)
        {
            return;
        }

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
        if (0.0f >= screenPosition.z)
        {
            return;
        }

        screenPosition.y = Screen.height - screenPosition.y;

        EnsureLabelStyle();

        bool hasMessage = false == string.IsNullOrEmpty(message);
        string text = hasMessage ? message : "[E]  상자 열기";
        Color color = hasMessage ? messageColor : PromptColor;

        float scale = Mathf.Max(0.6f, Screen.height / ReferenceHeight);
        labelStyle.fontSize = Mathf.RoundToInt(20.0f * scale);

        GUIContent content = new GUIContent(text);
        float maximumWidth = Mathf.Max(40.0f, Screen.width - 12.0f);

        while (labelStyle.fontSize > 10
            && labelStyle.CalcSize(content).x + 44.0f * scale > maximumWidth)
        {
            labelStyle.fontSize--;
        }

        Vector2 textSize = labelStyle.CalcSize(content);
        float boxWidth = textSize.x + 44.0f * scale;
        float boxHeight = textSize.y + 22.0f * scale;

        // 획득 메시지는 끝날 때쯤 서서히 사라진다.
        float alpha = 1.0f;
        if (true == opened)
        {
            float remaining = MessageDuration - openedElapsed;
            alpha = Mathf.Clamp01(remaining / 0.35f);
        }

        Rect box = new Rect(
            Mathf.Clamp(screenPosition.x - boxWidth * 0.5f, 6.0f,
                Mathf.Max(6.0f, Screen.width - boxWidth - 6.0f)),
            Mathf.Clamp(screenPosition.y - boxHeight - 8.0f, 6.0f,
                Mathf.Max(6.0f, Screen.height - boxHeight - 6.0f)),
            boxWidth,
            boxHeight
        );

        Fill(new Rect(box.x, box.y + 3.0f * scale, box.width, box.height),
            new Color(0.0f, 0.0f, 0.0f, 0.35f * alpha));
        Fill(box, new Color(0.07f, 0.043f, 0.094f, 0.95f * alpha));

        float thickness = Mathf.Max(1.0f, 2.0f * scale);
        Color edge = new Color(color.r, color.g, color.b, 0.85f * alpha);
        Fill(new Rect(box.x, box.y, box.width, thickness), edge);
        Fill(new Rect(box.x, box.yMax - thickness, box.width, thickness), edge);
        Fill(new Rect(box.x, box.y, thickness, box.height), edge);
        Fill(new Rect(box.xMax - thickness, box.y, thickness, box.height), edge);

        labelStyle.normal.textColor = new Color(0.03f, 0.01f, 0.04f, 0.9f * alpha);
        GUI.Label(new Rect(box.x + 1.0f, box.y + 2.0f, box.width, box.height), content, labelStyle);

        labelStyle.normal.textColor = new Color(color.r, color.g, color.b, alpha);
        GUI.Label(box, content, labelStyle);
    }

    private static void CreateSpritesIfNeeded()
    {
        if (null != closedSprite && null != openSprite)
        {
            return;
        }

        closedSprite = CreateChestSprite(false);
        openSprite = CreateChestSprite(true);
    }

    private static Sprite CreateChestSprite(bool isOpen)
    {
        const int width = 20;
        const int height = 16;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = isOpen ? "ChestOpen" : "ChestClosed";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        Color outline = new Color(0.10f, 0.04f, 0.13f, 1.0f);
        Color darkWood = new Color(0.27f, 0.10f, 0.20f, 1.0f);
        Color wood = new Color(0.50f, 0.20f, 0.28f, 1.0f);
        Color highlight = new Color(0.70f, 0.34f, 0.34f, 1.0f);
        Color metal = new Color(0.95f, 0.63f, 0.18f, 1.0f);

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        PaintRect(pixels, width, 1, 2, 18, 9, outline);
        PaintRect(pixels, width, 2, 3, 16, 7, darkWood);
        PaintRect(pixels, width, 3, 4, 14, 5, wood);
        PaintRect(pixels, width, 3, 8, 14, 1, highlight);

        int lidY = true == isOpen ? 12 : 10;
        PaintRect(pixels, width, 1, lidY, 18, 4, outline);
        PaintRect(pixels, width, 2, lidY + 1, 16, 2, wood);
        PaintRect(pixels, width, 3, lidY + 2, 14, 1, highlight);

        PaintRect(pixels, width, 4, 2, 2, 11, metal);
        PaintRect(pixels, width, 14, 2, 2, 11, metal);
        PaintRect(pixels, width, 8, 6, 4, 4, outline);
        PaintRect(pixels, width, 9, 7, 2, 2, metal);

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0.0f, 0.0f, width, height),
            new Vector2(0.5f, 0.18f),
            20.0f
        );
    }

    private static void PaintRect(
        Color[] pixels,
        int textureWidth,
        int x,
        int y,
        int width,
        int height,
        Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                pixels[py * textureWidth + px] = color;
            }
        }
    }
}
