using UnityEngine;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private float screenMargin = 18.0f;
    [SerializeField] private float barWidthRatio = 0.22f;

    private static PlayerHUD instance;

    private DungeonGenerator generator;
    private TitleMenu titleMenu;
    private Player player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAutomatically()
    {
        if (null != FindAnyObjectByType<PlayerHUD>())
        {
            return;
        }

        GameObject hudObject = new GameObject("PlayerHUD");
        hudObject.AddComponent<PlayerHUD>();
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
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        titleMenu = FindAnyObjectByType<TitleMenu>();

        if (null != titleMenu && true == titleMenu.enabled)
        {
            player = null;
            return;
        }

        if (null == generator)
        {
            generator = FindAnyObjectByType<DungeonGenerator>();
        }

        if (null == generator)
        {
            player = null;
            return;
        }

        if (null == player)
        {
            player = FindAnyObjectByType<Player>();
        }

        // 일시정지와 결과 화면에서는 timeScale이 0이라 deltaTime도 0이 된다.
        // 그래서 시간이 저절로 멈춘다. 별도 처리가 필요 없다.
        if (null != player && false == player.IsDead)
        {
            GameData.elapsedTime += Time.deltaTime;
        }
    }

    private void OnGUI()
    {
        if (null == player || null == generator)
        {
            return;
        }

        if (null != titleMenu && true == titleMenu.enabled)
        {
            return;
        }

        if (true == GameEndUI.IsShowing || true == IronicRewardUI.IsShowing)
        {
            return;
        }

        float availableWidth = Mathf.Max(120.0f, Screen.width - screenMargin * 2.0f);
        float barWidth = Mathf.Min(
            availableWidth,
            Mathf.Max(360.0f, Screen.width * barWidthRatio)
        );
        float barHeight = Screen.height * 0.028f;
        float gap = Screen.height * 0.010f;
        float x = screenMargin;
        float y = screenMargin;

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 16, 30);
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.alignment = TextAnchor.MiddleLeft;
        textStyle.normal.textColor = Color.white;

        // --- 경과 시간 -------------------------------------------------
        GUI.Label(
            new Rect(x, y, barWidth, barHeight),
            $"시간  {GameData.FormatElapsedTime()}   난이도  {GameData.GetDifficultyName()}",
            textStyle
        );

        y += barHeight + gap;

        // --- 체력 ------------------------------------------------------
        float healthRatio = 0.0f;
        if (0 < player.MaxHealth)
        {
            healthRatio = Mathf.Clamp01((float)player.CurrentHealth / player.MaxHealth);
        }

        DrawBar(
            new Rect(x, y, barWidth, barHeight),
            healthRatio,
            new Color(0.85f, 0.16f, 0.20f, 1.0f)
        );

        GUIStyle centerStyle = new GUIStyle(textStyle);
        centerStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(
            new Rect(x, y, barWidth, barHeight),
            $"HP  {player.CurrentHealth} / {player.MaxHealth}",
            centerStyle
        );

        y += barHeight + gap;

        // --- 특수공격 쿨타임 -------------------------------------------
        // SkillCooldownRatio는 1이면 방금 썼다는 뜻이라 뒤집어서 채운다.
        float readyRatio = 1.0f - player.SkillCooldownRatio;
        bool ready = 0.999f <= readyRatio;

        Color skillColor = true == ready
            ? new Color(0.30f, 0.75f, 1.0f, 1.0f)
            : new Color(0.35f, 0.42f, 0.60f, 1.0f);

        DrawBar(new Rect(x, y, barWidth, barHeight), readyRatio, skillColor);

        string skillText = true == ready ? "특수공격 [X]  준비" : "특수공격 [X]";
        GUI.Label(new Rect(x, y, barWidth, barHeight), skillText, centerStyle);

        y += barHeight + gap;

        // --- 인벤토리 --------------------------------------------------
        PlayerInventory inventory = player.Inventory;
        if (null != inventory)
        {
            float rowHeight = Mathf.Max(28.0f, barHeight * 1.05f);
            float headerHeight = Mathf.Max(20.0f, barHeight * 0.65f);
            float padding = 8.0f;
            float inventoryHeight = padding * 2.0f + headerHeight * 2.0f + rowHeight * 4.0f;
            Rect inventoryRect = new Rect(x, y, barWidth, inventoryHeight);

            DrawSolidRect(inventoryRect, new Color(0.05f, 0.03f, 0.08f, 0.85f));
            DrawBorder(inventoryRect, 2.0f, new Color(0.58f, 0.43f, 0.68f, 1.0f));

            GUIStyle inventoryStyle = new GUIStyle(textStyle);
            inventoryStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.019f), 15, 26);
            inventoryStyle.clipping = TextClipping.Overflow;

            GUIStyle headerStyle = new GUIStyle(inventoryStyle);
            headerStyle.fontSize = Mathf.Max(12, inventoryStyle.fontSize - 3);
            headerStyle.normal.textColor = new Color(0.70f, 0.62f, 0.78f, 1.0f);

            GUIStyle valueStyle = new GUIStyle(inventoryStyle);
            valueStyle.alignment = TextAnchor.MiddleRight;
            valueStyle.normal.textColor = new Color(1.0f, 0.86f, 0.43f, 1.0f);

            float contentX = x + padding;
            float contentWidth = barWidth - padding * 2.0f;
            float currentY = y + padding;

            GUI.Label(new Rect(contentX + 4.0f, currentY, contentWidth, headerHeight), "아이템", headerStyle);
            currentY += headerHeight;

            Rect potionRect = new Rect(contentX, currentY, contentWidth, rowHeight);
            DrawHudRow(potionRect);
            DrawKeyBadge(potionRect, "1", inventoryStyle, new Color(0.78f, 0.22f, 0.30f, 1.0f));
            GUI.Label(
                new Rect(potionRect.x + 45.0f, potionRect.y, potionRect.width * 0.45f, potionRect.height),
                "체력 포션",
                inventoryStyle
            );
            GUI.Label(
                new Rect(potionRect.x + potionRect.width * 0.52f, potionRect.y, potionRect.width * 0.44f, potionRect.height),
                $"{inventory.HealthPotionCount}개   체력 +{inventory.HealthPotionHealAmount}",
                valueStyle
            );
            currentY += rowHeight;

            Rect fragmentRect = new Rect(contentX, currentY, contentWidth, rowHeight);
            DrawHudRow(fragmentRect);
            DrawKeyBadge(fragmentRect, "2", inventoryStyle, new Color(0.75f, 0.52f, 0.18f, 1.0f));
            GUI.Label(
                new Rect(fragmentRect.x + 45.0f, fragmentRect.y, fragmentRect.width * 0.52f, fragmentRect.height),
                "보스 소환석 조각",
                inventoryStyle
            );
            DrawFragmentSlots(
                fragmentRect,
                inventory.BossFragmentCount,
                PlayerInventory.BossFragmentsRequired,
                inventoryStyle
            );
            currentY += rowHeight;

            GUI.Label(new Rect(contentX + 4.0f, currentY, contentWidth, headerHeight), "현재 능력치", headerStyle);
            currentY += headerHeight;

            GUIStyle changeStyle = new GUIStyle(inventoryStyle);
            bool hasLatestChange = false == string.IsNullOrEmpty(player.LastIronicModifierDescription);
            changeStyle.normal.textColor = false == hasLatestChange
                ? Color.white
                : true == player.LastIronicModifierWasPositive
                    ? new Color(0.38f, 1.0f, 0.50f, 1.0f)
                    : new Color(1.0f, 0.38f, 0.38f, 1.0f);

            GUIStyle changeValueStyle = new GUIStyle(changeStyle);
            changeValueStyle.alignment = TextAnchor.MiddleRight;

            Rect changeRect = new Rect(contentX, currentY, contentWidth, rowHeight);
            DrawHudRow(changeRect);
            GUI.Label(
                new Rect(changeRect.x + 10.0f, changeRect.y, changeRect.width * 0.33f, changeRect.height),
                "최근 변화",
                inventoryStyle
            );
            string latestChange = false == hasLatestChange
                ? "없음"
                : player.LastIronicModifierDescription;
            Rect changeValueRect = new Rect(
                changeRect.x + changeRect.width * 0.30f,
                changeRect.y,
                changeRect.width * 0.66f,
                changeRect.height
            );
            FitFontToWidth(changeValueStyle, latestChange, changeValueRect, 10);
            GUI.Label(changeValueRect, latestChange, changeValueStyle);
            currentY += rowHeight;

            Rect statsRect = new Rect(contentX, currentY, contentWidth, rowHeight);
            DrawHudRow(statsRect);
            GUIStyle statsStyle = new GUIStyle(inventoryStyle);
            statsStyle.alignment = TextAnchor.MiddleCenter;
            string statsText = player.IronicStatsSummary
                .Replace("속도", "이동")
                .Replace("공격 ", "공격력 ")
                .Replace("X ", "특수 ");
            FitFontToWidth(statsStyle, statsText, statsRect, 10);
            GUI.Label(statsRect, statsText, statsStyle);
        }
    }

    private void DrawHudRow(Rect rect)
    {
        DrawSolidRect(rect, new Color(0.095f, 0.055f, 0.12f, 0.76f));
        DrawSolidRect(
            new Rect(rect.x, rect.yMax - 1.0f, rect.width, 1.0f),
            new Color(0.40f, 0.30f, 0.48f, 0.55f)
        );
    }

    private void DrawKeyBadge(Rect rowRect, string key, GUIStyle sourceStyle, Color color)
    {
        float badgeSize = Mathf.Min(26.0f, rowRect.height - 6.0f);
        Rect badgeRect = new Rect(
            rowRect.x + 7.0f,
            rowRect.y + (rowRect.height - badgeSize) * 0.5f,
            badgeSize,
            badgeSize
        );

        DrawSolidRect(badgeRect, new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1.0f));
        DrawBorder(badgeRect, 2.0f, color);

        GUIStyle keyStyle = new GUIStyle(sourceStyle);
        keyStyle.fontSize = Mathf.Max(11, sourceStyle.fontSize - 2);
        keyStyle.alignment = TextAnchor.MiddleCenter;
        keyStyle.normal.textColor = Color.white;
        GUI.Label(badgeRect, key, keyStyle);
    }

    private void DrawFragmentSlots(Rect rowRect, int currentCount, int requiredCount, GUIStyle sourceStyle)
    {
        int visibleCount = Mathf.Max(1, requiredCount);
        float slotSize = Mathf.Min(17.0f, rowRect.height - 10.0f);
        float slotGap = 5.0f;
        float countWidth = 45.0f;
        float slotsWidth = visibleCount * slotSize + (visibleCount - 1) * slotGap;
        float startX = rowRect.xMax - countWidth - slotsWidth - 10.0f;
        float slotY = rowRect.y + (rowRect.height - slotSize) * 0.5f;

        for (int i = 0; i < visibleCount; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotSize + slotGap), slotY, slotSize, slotSize);
            bool filled = i < currentCount;
            DrawSolidRect(
                slotRect,
                true == filled
                    ? new Color(1.0f, 0.68f, 0.20f, 1.0f)
                    : new Color(0.12f, 0.08f, 0.15f, 1.0f)
            );
            DrawBorder(slotRect, 1.0f, new Color(0.85f, 0.62f, 0.30f, 1.0f));
        }

        GUIStyle countStyle = new GUIStyle(sourceStyle);
        countStyle.fontSize = Mathf.Max(11, sourceStyle.fontSize - 2);
        countStyle.alignment = TextAnchor.MiddleRight;
        countStyle.normal.textColor = new Color(1.0f, 0.86f, 0.43f, 1.0f);
        GUI.Label(
            new Rect(rowRect.xMax - countWidth - 4.0f, rowRect.y, countWidth, rowRect.height),
            $"{currentCount}/{requiredCount}",
            countStyle
        );
    }

    private void FitFontToWidth(GUIStyle style, string text, Rect rect, int minimumSize)
    {
        GUIContent content = new GUIContent(text);
        float usableWidth = Mathf.Max(1.0f, rect.width - 8.0f);

        while (style.fontSize > minimumSize && style.CalcSize(content).x > usableWidth)
        {
            style.fontSize--;
        }
    }

    private void DrawBar(Rect rect, float ratio, Color fillColor)
    {
        DrawSolidRect(rect, new Color(0.05f, 0.03f, 0.08f, 0.85f));

        Rect fillRect = new Rect(
            rect.x,
            rect.y,
            rect.width * Mathf.Clamp01(ratio),
            rect.height
        );

        DrawSolidRect(fillRect, fillColor);
        DrawBorder(rect, 2.0f, new Color(0.58f, 0.43f, 0.68f, 1.0f));
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
