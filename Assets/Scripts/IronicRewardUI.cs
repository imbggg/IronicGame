using UnityEngine;

public enum IronicStatType
{
    MaxHealth,
    MoveSpeed,
    AttackPower,
    SkillCooldown
}

public class IronicRewardUI : MonoBehaviour
{
    private enum RoulettePhase
    {
        Spinning,
        Result
    }

    private struct RewardOption
    {
        public IronicStatType statType;
        public bool increase;
        public string description;
    }

    private const float SpinDuration = 2.8f;
    private const float ResultDuration = 2.2f;

    private static IronicRewardUI instance;

    private Player player;
    private RewardOption currentOption;
    private RewardOption previousOption;
    private float tickInterval = 0.06f;
    private float beforeValue;
    private float afterValue;
    private GUIStyle labelStyle;
    private RoulettePhase phase;
    private float phaseElapsed;
    private float tickRemaining;
    private bool showing;

    public static bool IsShowing
    {
        get { return null != instance && instance.showing; }
    }

    public static void Show(Player target)
    {
        if (null == target || true == target.IsDead)
        {
            return;
        }

        if (null == instance)
        {
            GameObject uiObject = new GameObject("IronicRewardUI");
            instance = uiObject.AddComponent<IronicRewardUI>();
        }

        instance.Open(target);
    }

    private void Awake()
    {
        if (null != instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Open(Player target)
    {
        if (true == showing)
        {
            return;
        }

        player = target;
        currentOption = CreateRandomOption();
        previousOption = currentOption;
        tickInterval = 0.06f;
        phase = RoulettePhase.Spinning;
        phaseElapsed = 0.0f;
        tickRemaining = 0.0f;
        showing = true;
        Time.timeScale = 0.0f;
    }

    private void Update()
    {
        if (false == showing)
        {
            return;
        }

        phaseElapsed += Time.unscaledDeltaTime;

        if (RoulettePhase.Spinning == phase)
        {
            tickRemaining -= Time.unscaledDeltaTime;

            if (0.0f >= tickRemaining)
            {
                previousOption = currentOption;
                currentOption = CreateRandomOption();

                float progress = Mathf.Clamp01(phaseElapsed / SpinDuration);
                tickInterval = Mathf.Lerp(0.06f, 0.32f, progress * progress);
                tickRemaining = tickInterval;
            }

            if (phaseElapsed >= SpinDuration)
            {
                currentOption = CreateRandomOption();
                phase = RoulettePhase.Result;
                phaseElapsed = 0.0f;

                if (null != player)
                {
                    beforeValue = ReadStat(player, currentOption.statType);
                    player.ApplyIronicModifier(
                        currentOption.statType,
                        currentOption.increase
                    );
                    afterValue = ReadStat(player, currentOption.statType);
                }

                RetroAudio.Play(
                    true == currentOption.increase
                        ? RetroSound.RouletteGood
                        : RetroSound.RouletteBad
                );
            }
            return;
        }

        if (phaseElapsed >= ResultDuration)
        {
            showing = false;
            Time.timeScale = 1.0f;
        }
    }

    private RewardOption CreateRandomOption()
    {
        IronicStatType statType = (IronicStatType)Random.Range(0, 4);
        bool increase = 0 == Random.Range(0, 2);

        return new RewardOption
        {
            statType = statType,
            increase = increase,
            description = GetDescription(statType, increase)
        };
    }

    public static string GetDescription(IronicStatType statType, bool increase)
    {
        switch (statType)
        {
            case IronicStatType.MaxHealth:
                return true == increase ? "최대 체력 +2" : "최대 체력 -2";
            case IronicStatType.MoveSpeed:
                return true == increase ? "이동 속도 +20%" : "이동 속도 -15%";
            case IronicStatType.AttackPower:
                return true == increase ? "공격력 +50%" : "공격력 -25%";
            default:
                return true == increase ? "특수공격 대기시간 -25%" : "특수공격 대기시간 +30%";
        }
    }

    private static readonly Color Face = new Color32(199, 174, 192, 255);
    private static readonly Color Muted = new Color32(151, 132, 158, 255);
    private static readonly Color Good = new Color32(171, 218, 137, 255);
    private static readonly Color Bad = new Color32(241, 115, 119, 255);
    private static readonly string[] StatLabels = { "체력", "이동", "공격", "대기시간" };

    private void OnGUI()
    {
        if (!showing) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        try
        {
            EnsureStyle();
            float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            GUI.depth = -30;
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            Fill(new Rect(0, 0, width, height), new Color(0.025f, 0.012f, 0.035f, 0.78f));

            const float panelWidth = 560;
            const float panelHeight = 408;
            float x = (width - panelWidth) * 0.5f;
            float y = (height - panelHeight) * 0.5f;
            bool result = phase == RoulettePhase.Result;
            bool changed = !Mathf.Approximately(beforeValue, afterValue);
            Color accent = !result || !changed ? Face : currentOption.increase ? Good : Bad;

            // Quiet stone-colored surfaces, without the old bright rectangular frame.
            Fill(new Rect(x + 5, y + 7, panelWidth, panelHeight), new Color(0, 0, 0, 0.25f));
            Fill(new Rect(x, y, panelWidth, panelHeight), new Color32(23, 16, 30, 245));
            Fill(new Rect(x + 40, y, panelWidth - 80, 1), new Color32(112, 78, 109, 180));
            Text(new Rect(x + 32, y + 21, panelWidth - 64, 46),
                result ? (changed ? (currentOption.increase ? "강화" : "약화") : "변화 없음") : "아이러닉 룰렛",
                30, accent);
            Text(new Rect(x + 32, y + 67, panelWidth - 64, 28),
                result ? "결과가 자동으로 적용되었습니다" : "방 클리어 · 멈추는 순간 결정됩니다", 17, Muted);

            DrawStatTabs(new Rect(x + 52, y + 111, panelWidth - 104, 30), accent);
            if (result)
            {
                Text(new Rect(x + 40, y + 159, panelWidth - 80, 40), StatName(currentOption.statType), 26, Face);
                Text(new Rect(x + 40, y + 199, panelWidth - 80, 66), ResultDelta(), 48, accent);
                Text(new Rect(x + 55, y + 278, 185, 38), FormatValue(beforeValue), 26, Muted);
                Text(new Rect(x + 240, y + 278, 80, 38), "→", 25, Muted);
                Text(new Rect(x + 320, y + 278, 185, 38), FormatValue(afterValue), 26, accent);
            }
            else
            {
                DrawReel(new Rect(x + 42, y + 154, panelWidth - 84, 120));
                Text(new Rect(x + 32, y + 284, panelWidth - 64, 30), "좋든 나쁘든, 결과는 하나", 18, Muted);
            }

            float progress = result
                ? 1 - Mathf.Clamp01(phaseElapsed / ResultDuration)
                : Mathf.Clamp01(phaseElapsed / SpinDuration);
            Fill(new Rect(x + 70, y + 338, panelWidth - 140, 3), new Color32(58, 40, 63, 255));
            Fill(new Rect(x + 70, y + 338, (panelWidth - 140) * progress, 3), accent);
            Text(new Rect(x + 32, y + 356, panelWidth - 64, 30),
                result ? "잠시 후 탐험을 계속합니다" : "운명이 돌아가는 중...", 17, Muted);
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }

    private void DrawStatTabs(Rect rect, Color accent)
    {
        float cellWidth = rect.width / StatLabels.Length;
        for (int i = 0; i < StatLabels.Length; i++)
        {
            bool active = i == (int)currentOption.statType;
            Rect cell = new Rect(rect.x + i * cellWidth, rect.y, cellWidth, rect.height);
            Text(cell, StatLabels[i], 18, active ? accent : Muted);
            if (active) Fill(new Rect(cell.center.x - 15, cell.yMax, 30, 2), accent);
        }
    }

    private void DrawReel(Rect rect)
    {
        // Each tick moves the previous result up and the next one into the center.
        // Visual motion uses no extra random draws, so reward odds remain unchanged.
        float progress = 1 - Mathf.Clamp01(tickRemaining / Mathf.Max(0.01f, tickInterval));
        float motion = Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress * 2.5f));
        const float stride = 59;
        GUI.BeginGroup(rect);
        try
        {
            float centerY = (rect.height - 46) * 0.5f;
            ReelRow(previousOption, centerY - motion * stride, rect.width, 1 - motion);
            ReelRow(currentOption, centerY + (1 - motion) * stride, rect.width, motion);
            RewardOption preview = new RewardOption
            {
                statType = (IronicStatType)(((int)currentOption.statType + 1) % 4),
                increase = !currentOption.increase
            };
            ReelRow(preview, centerY + (2 - motion) * stride, rect.width, 0);
        }
        finally { GUI.EndGroup(); }
        Fill(new Rect(rect.x + 24, rect.y + 32, rect.width - 48, 1), new Color32(87, 60, 88, 150));
        Fill(new Rect(rect.x + 24, rect.y + 88, rect.width - 48, 1), new Color32(87, 60, 88, 150));
        // Fixed selection marks make the stopping position obvious.
        Text(new Rect(rect.x - 18, rect.y + 36, 28, 46), "›", 28, Face);
        Text(new Rect(rect.xMax - 10, rect.y + 36, 28, 46), "‹", 28, Face);
    }

    private void ReelRow(RewardOption option, float y, float width, float emphasis)
    {
        Color color = Color.Lerp(new Color32(95, 77, 106, 110), Face, emphasis);
        string text = StatName(option.statType) + "  " + OptionDelta(option);
        Text(new Rect(12, y, width - 24, 46), text, 28, color);
    }

    private static string StatName(IronicStatType stat)
    {
        switch (stat)
        {
            case IronicStatType.MaxHealth: return "최대 체력";
            case IronicStatType.MoveSpeed: return "이동 속도";
            case IronicStatType.AttackPower: return "공격력";
            default: return "특수공격 대기시간";
        }
    }

    private static string OptionDelta(RewardOption option)
    {
        switch (option.statType)
        {
            case IronicStatType.MaxHealth: return option.increase ? "+2" : "-2";
            case IronicStatType.MoveSpeed: return option.increase ? "+20%" : "-15%";
            case IronicStatType.AttackPower: return option.increase ? "+50%" : "-25%";
            default: return option.increase ? "-25%" : "+30%";
        }
    }

    private static float ReadStat(Player target, IronicStatType stat)
    {
        switch (stat)
        {
            case IronicStatType.MaxHealth: return target.MaxHealth;
            case IronicStatType.MoveSpeed: return target.moveSpeed;
            case IronicStatType.AttackPower: return target.AttackPowerMultiplier;
            default: return target.SkillCooldownDuration;
        }
    }

    private string FormatValue(float value)
    {
        switch (currentOption.statType)
        {
            case IronicStatType.MaxHealth: return value.ToString("0");
            case IronicStatType.MoveSpeed: return value.ToString("0.0");
            case IronicStatType.AttackPower: return "x" + value.ToString("0.00");
            default: return value.ToString("0.0") + "초";
        }
    }

    private string ResultDelta()
    {
        // Show the actual change, including when the stat has reached its clamp.
        float difference = afterValue - beforeValue;
        if (Mathf.Approximately(difference, 0)) return "유지";
        if (currentOption.statType == IronicStatType.MaxHealth)
            return difference.ToString("+0;-0;0");
        float percent = Mathf.Abs(beforeValue) > 0.0001f ? difference / beforeValue * 100 : 0;
        return percent.ToString("+0.#;-0.#;0") + "%";
    }

    private void EnsureStyle()
    {
        if (labelStyle != null) return;
        Font font = Resources.Load<Font>("Fonts/Galmuri11-Bold");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelStyle = new GUIStyle
        {
            font = font, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter,
            wordWrap = false, clipping = TextClipping.Clip,
            padding = new RectOffset(), margin = new RectOffset()
        };
    }

    private void Text(Rect rect, string text, int size, Color color)
    {
        labelStyle.fontSize = size;
        GUIContent content = new GUIContent(text);
        while (labelStyle.fontSize > 15 && labelStyle.CalcSize(content).x > rect.width) labelStyle.fontSize--;
        labelStyle.normal.textColor = new Color(0.02f, 0.01f, 0.03f, color.a);
        GUI.Label(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), content, labelStyle);
        labelStyle.normal.textColor = color;
        GUI.Label(rect, content, labelStyle);
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
