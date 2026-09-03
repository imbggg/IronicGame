using UnityEngine;

public class LootChest : MonoBehaviour
{
    private const float OpenDistance = 1.25f;
    private const float MessageDuration = 1.1f;
    private const float HealthPotionChance = 0.30f;
    private const float BossFragmentChance = 0.03f;

    private static Sprite closedSprite;
    private static Sprite openSprite;

    private Player player;
    private SpriteRenderer spriteRenderer;
    private bool opened;
    private float openedElapsed;
    private int guaranteedBossFragmentCount;
    private string message = string.Empty;

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
            return;
        }

        opened = true;
        openedElapsed = 0.0f;
        spriteRenderer.sprite = openSprite;
        message = InventoryItemType.HealthPotion == itemType
            ? "체력 포션 획득!"
            : 1 < rewardAmount
                ? $"보스 소환석 조각 x{rewardAmount}"
                : "보스 소환석 조각 획득!";

        RetroAudio.Play(RetroSound.ChestOpen);
    }

    private void OnGUI()
    {
        if (null == Camera.main || null == player)
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

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.020f), 10, 26);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = false;
        style.clipping = TextClipping.Overflow;
        style.normal.textColor = new Color(1.0f, 0.87f, 0.35f, 1.0f);

        string text = true == string.IsNullOrEmpty(message) ? "[E] 상자 열기" : message;
        GUIContent content = new GUIContent(text);
        float maximumWidth = Mathf.Max(40.0f, Screen.width - 12.0f);

        while (style.fontSize > 8 && style.CalcSize(content).x + 24.0f > maximumWidth)
        {
            style.fontSize--;
        }

        Vector2 textSize = style.CalcSize(content);
        float labelWidth = Mathf.Min(maximumWidth, Mathf.Max(220.0f, textSize.x + 40.0f));
        float labelHeight = Mathf.Max(60.0f, textSize.y + 30.0f);
        float labelX = Mathf.Clamp(
            screenPosition.x - labelWidth * 0.5f,
            6.0f,
            Mathf.Max(6.0f, Screen.width - labelWidth - 6.0f)
        );
        float labelY = Mathf.Clamp(
            screenPosition.y - labelHeight - 8.0f,
            6.0f,
            Mathf.Max(6.0f, Screen.height - labelHeight - 6.0f)
        );
        Rect labelRect = new Rect(labelX, labelY, labelWidth, labelHeight);

        Color previousColor = GUI.color;
        GUI.color = new Color(0.05f, 0.02f, 0.08f, 0.88f);
        GUI.DrawTexture(labelRect, Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(labelRect, text, style);
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
