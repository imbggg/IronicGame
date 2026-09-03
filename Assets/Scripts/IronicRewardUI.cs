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
                currentOption = CreateRandomOption();

                float progress = Mathf.Clamp01(phaseElapsed / SpinDuration);
                tickRemaining = Mathf.Lerp(0.06f, 0.32f, progress * progress);
            }

            if (phaseElapsed >= SpinDuration)
            {
                currentOption = CreateRandomOption();
                phase = RoulettePhase.Result;
                phaseElapsed = 0.0f;

                if (null != player)
                {
                    player.ApplyIronicModifier(
                        currentOption.statType,
                        currentOption.increase
                    );
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

    private void OnGUI()
    {
        if (false == showing)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = new Color(0.02f, 0.01f, 0.04f, 0.90f);
        GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        float panelWidth = Mathf.Min(Screen.width * 0.94f, 920.0f);
        float panelHeight = Mathf.Min(Screen.height * 0.84f, 430.0f);
        float x = (Screen.width - panelWidth) * 0.5f;
        float y = (Screen.height - panelHeight) * 0.5f;

        DrawSolidRect(
            new Rect(x, y, panelWidth, panelHeight),
            new Color(0.08f, 0.035f, 0.12f, 0.98f)
        );
        bool showingResult = RoulettePhase.Result == phase;
        Color resultColor = true == currentOption.increase
            ? new Color(0.38f, 1.0f, 0.50f, 1.0f)
            : new Color(1.0f, 0.32f, 0.32f, 1.0f);

        DrawBorder(
            new Rect(x, y, panelWidth, panelHeight),
            4.0f,
            true == showingResult
                ? resultColor
                : new Color(0.68f, 0.44f, 0.78f, 1.0f)
        );

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(panelHeight * 0.105f), 10, 45);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.clipping = TextClipping.Overflow;
        titleStyle.normal.textColor = new Color(1.0f, 0.80f, 0.26f, 1.0f);

        GUIStyle resultStyle = new GUIStyle(GUI.skin.label);
        resultStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(panelHeight * 0.11f), 10, 47);
        resultStyle.fontStyle = FontStyle.Bold;
        resultStyle.alignment = TextAnchor.MiddleCenter;
        resultStyle.wordWrap = false;
        resultStyle.clipping = TextClipping.Overflow;
        resultStyle.normal.textColor = true == showingResult
            ? resultColor
            : new Color(0.92f, 0.88f, 0.96f, 1.0f);

        GUIStyle guideStyle = new GUIStyle(GUI.skin.label);
        guideStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(panelHeight * 0.06f), 9, 26);
        guideStyle.alignment = TextAnchor.MiddleCenter;
        guideStyle.wordWrap = true;
        guideStyle.clipping = TextClipping.Overflow;
        guideStyle.normal.textColor = Color.white;

        string title = RoulettePhase.Spinning == phase ? "아이러닉 룰렛" : "결과 확정";
        string resultText = false == showingResult
            ? currentOption.description
            : true == currentOption.increase
                ? $"강화  |  {currentOption.description}"
                : $"약화  |  {currentOption.description}";
        string guide = RoulettePhase.Spinning == phase
            ? "좋은 결과와 나쁜 결과가 무작위로 돌아갑니다"
            : true == currentOption.increase
                ? "능력치가 좋아졌습니다"
                : "능력치가 나빠졌습니다";

        Rect titleRect = new Rect(
            x + panelWidth * 0.04f,
            y + panelHeight * 0.04f,
            panelWidth * 0.92f,
            panelHeight * 0.22f
        );
        Rect resultRect = new Rect(
            x + panelWidth * 0.04f,
            y + panelHeight * 0.27f,
            panelWidth * 0.92f,
            panelHeight * 0.40f
        );
        Rect guideRect = new Rect(
            x + panelWidth * 0.04f,
            y + panelHeight * 0.69f,
            panelWidth * 0.92f,
            panelHeight * 0.24f
        );

        if (true == showingResult)
        {
            DrawSolidRect(
                resultRect,
                new Color(resultColor.r * 0.22f, resultColor.g * 0.22f, resultColor.b * 0.22f, 0.70f)
            );
            DrawBorder(resultRect, 2.0f, resultColor);
        }

        FitSingleLineToRect(titleStyle, title, titleRect, 6);
        FitSingleLineToRect(resultStyle, resultText, resultRect, 6);
        FitFontToRect(guideStyle, guide, guideRect, 6);

        GUI.Label(titleRect, title, titleStyle);
        GUI.Label(resultRect, resultText, resultStyle);
        GUI.Label(guideRect, guide, guideStyle);
    }

    private void FitFontToRect(GUIStyle style, string text, Rect rect, int minimumSize)
    {
        GUIContent content = new GUIContent(text);
        float usableHeight = Mathf.Max(1.0f, rect.height - 12.0f);

        while (style.fontSize > minimumSize && style.CalcHeight(content, rect.width) > usableHeight)
        {
            style.fontSize--;
        }
    }

    private void FitSingleLineToRect(GUIStyle style, string text, Rect rect, int minimumSize)
    {
        GUIContent content = new GUIContent(text);
        float usableWidth = Mathf.Max(1.0f, rect.width - 12.0f);
        float usableHeight = Mathf.Max(1.0f, rect.height - 12.0f);

        while (style.fontSize > minimumSize)
        {
            Vector2 textSize = style.CalcSize(content);
            if (textSize.x <= usableWidth && textSize.y <= usableHeight)
            {
                break;
            }

            style.fontSize--;
        }
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
