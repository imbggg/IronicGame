using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    private enum MenuState
    {
        Main = 0,
        Character = 1,
        Difficulty = 2,
        Settings = 3
    }

    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string backgroundSpriteName = "Sprites/TitleBackground";

    private static readonly string[] CharacterNames = { "궁수", "도적", "마법사" };
    private static readonly string[] CharacterDescriptions =
    {
        "거리를 유지하며 안정적으로 공격합니다",
        "빠르게 파고들고 구르기로 위기를 피합니다",
        "강력한 마법과 특수공격을 사용합니다"
    };

    private static readonly string[] DifficultyNames = { "쉬움", "보통", "어려움" };
    private static readonly string[] DifficultyDescriptions =
    {
        "몬스터 공격력 -1\n공격 간격 35% 증가",
        "기본 공격력과 공격 간격",
        "몬스터 공격력 +1\n공격 간격 25% 감소"
    };

    private Texture2D background;
    private MenuState menuState = MenuState.Main;

    private readonly Color panelColor = new Color(0.08f, 0.035f, 0.10f, 0.78f);
    private readonly Color panelBorderColor = new Color(0.40f, 0.27f, 0.43f, 0.95f);
    private readonly Color goldColor = new Color(0.84f, 0.62f, 0.52f, 1.0f);
    private readonly Color textColor = new Color(0.90f, 0.82f, 0.88f, 1.0f);

    private void Awake()
    {
        background = LoadTexture(backgroundSpriteName);
    }

    private void OnGUI()
    {
        DrawBackground();
        DrawSolidRect(
            new Rect(0.0f, 0.0f, Screen.width, Screen.height),
            new Color(0.015f, 0.008f, 0.025f, 0.10f)
        );

        HandleBackKey();

        switch (menuState)
        {
            case MenuState.Main:
                DrawMainMenu();
                break;
            case MenuState.Character:
                DrawCharacterMenu();
                break;
            case MenuState.Difficulty:
                DrawDifficultyMenu();
                break;
            default:
                DrawSettingsMenu();
                break;
        }
    }

    private void HandleBackKey()
    {
        if (EventType.KeyDown != Event.current.type || KeyCode.Escape != Event.current.keyCode)
        {
            return;
        }

        if (MenuState.Difficulty == menuState)
        {
            menuState = MenuState.Character;
        }
        else if (MenuState.Character == menuState || MenuState.Settings == menuState)
        {
            menuState = MenuState.Main;
        }

        Event.current.Use();
    }

    private void DrawMainMenu()
    {
        float buttonWidth = Mathf.Min(Screen.width - 24.0f, 330.0f);
        float buttonHeight = Mathf.Clamp(Screen.height * 0.052f, 38.0f, 56.0f);
        float gap = Mathf.Clamp(Screen.height * 0.006f, 2.0f, 8.0f);
        float buttonX = (Screen.width - buttonWidth) * 0.5f;
        float buttonY = Mathf.Min(Screen.height * 0.57f, Screen.height - (buttonHeight + gap) * 3.0f - 18.0f);
        Color menuAccent = new Color(0.72f, 0.78f, 0.82f, 1.0f);

        if (DrawMinimalButton(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "게임 시작", menuAccent))
        {
            menuState = MenuState.Character;
        }

        buttonY += buttonHeight + gap;
        if (DrawMinimalButton(
            new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
            "환경설정",
            menuAccent))
        {
            menuState = MenuState.Settings;
        }

        buttonY += buttonHeight + gap;
        if (DrawMinimalButton(
            new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
            "나가기",
            menuAccent))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void DrawCharacterMenu()
    {
        float buttonWidth = Mathf.Min(Screen.width - 24.0f, 370.0f);
        float buttonHeight = Mathf.Clamp(Screen.height * 0.060f, 44.0f, 62.0f);
        float gap = Mathf.Clamp(Screen.height * 0.008f, 3.0f, 9.0f);
        float x = (Screen.width - buttonWidth) * 0.5f;
        float titleY = Screen.height * 0.40f;
        float y = titleY + Mathf.Clamp(Screen.height * 0.09f, 58.0f, 84.0f);
        Color menuAccent = new Color(0.72f, 0.78f, 0.82f, 1.0f);

        DrawMinimalHeading(new Rect(x, titleY, buttonWidth, 54.0f), "직업 선택");

        for (int i = 0; i < CharacterNames.Length; i++)
        {
            Rect buttonRect = new Rect(x, y + i * (buttonHeight + gap), buttonWidth, buttonHeight);
            if (DrawMinimalButton(buttonRect, CharacterNames[i], menuAccent))
            {
                GameData.selectedCharacter = i;
                menuState = MenuState.Difficulty;
            }
        }

        float backY = y + CharacterNames.Length * (buttonHeight + gap) + 8.0f;
        if (DrawMinimalButton(
            new Rect(x, backY, buttonWidth, buttonHeight),
            "뒤로",
            new Color(0.58f, 0.58f, 0.64f, 1.0f)))
        {
            menuState = MenuState.Main;
        }
    }

    private void DrawDifficultyMenu()
    {
        float buttonWidth = Mathf.Min(Screen.width - 24.0f, 370.0f);
        float buttonHeight = Mathf.Clamp(Screen.height * 0.052f, 38.0f, 56.0f);
        float gap = Mathf.Clamp(Screen.height * 0.006f, 2.0f, 8.0f);
        float x = (Screen.width - buttonWidth) * 0.5f;
        float titleY = Screen.height * 0.40f;
        float y = titleY + Mathf.Clamp(Screen.height * 0.075f, 54.0f, 78.0f);
        Color menuAccent = new Color(0.72f, 0.78f, 0.82f, 1.0f);

        DrawMinimalHeading(new Rect(x, titleY, buttonWidth, 48.0f), "난이도 선택");
        int hoveredDifficulty = -1;

        for (int i = 0; i < DifficultyNames.Length; i++)
        {
            Rect buttonRect = new Rect(x, y + i * (buttonHeight + gap), buttonWidth, buttonHeight);
            if (buttonRect.Contains(Event.current.mousePosition))
            {
                hoveredDifficulty = i;
            }

            if (DrawMinimalButton(buttonRect, DifficultyNames[i], menuAccent))
            {
                StartGame(i);
            }
        }

        float descriptionY = y + DifficultyNames.Length * (buttonHeight + gap) + 8.0f;
        string description = 0 <= hoveredDifficulty
            ? GetShortDifficultyDescription(hoveredDifficulty)
            : $"선택한 직업  ·  {GetSelectedCharacterName()}";
        DrawMinimalDescription(
            new Rect(x - 60.0f, descriptionY, buttonWidth + 120.0f, 38.0f),
            description
        );

        if (DrawMinimalButton(
            new Rect(x, descriptionY + 42.0f, buttonWidth, buttonHeight),
            "뒤로",
            new Color(0.58f, 0.58f, 0.64f, 1.0f)))
        {
            menuState = MenuState.Character;
        }
    }

    private void DrawSettingsMenu()
    {
        float contentWidth = Mathf.Min(Screen.width - 24.0f, 410.0f);
        float x = (Screen.width - contentWidth) * 0.5f;
        float y = Screen.height * 0.49f;
        DrawMinimalHeading(new Rect(x, y, contentWidth, 50.0f), "환경설정");

        GUIStyle volumeStyle = new GUIStyle(GUI.skin.label);
        volumeStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 14, 22);
        volumeStyle.fontStyle = FontStyle.Bold;
        volumeStyle.alignment = TextAnchor.MiddleCenter;
        volumeStyle.clipping = TextClipping.Overflow;
        volumeStyle.normal.textColor = textColor;

        float currentVolume = RetroAudio.MasterVolume;
        Rect volumeRect = new Rect(
            x,
            y + 72.0f,
            contentWidth,
            42.0f
        );
        string volumeText = $"전체 음량  {Mathf.RoundToInt(currentVolume * 100.0f)}%";
        FitSingleLine(volumeStyle, volumeText, volumeRect, 8);
        GUI.Label(volumeRect, volumeText, volumeStyle);

        float sliderWidth = contentWidth - 90.0f;
        float changedVolume = GUI.HorizontalSlider(
            new Rect(
                x + (contentWidth - sliderWidth) * 0.5f,
                y + 124.0f,
                sliderWidth,
                28.0f
            ),
            currentVolume,
            0.0f,
            1.0f
        );

        if (Mathf.Abs(changedVolume - currentVolume) > 0.001f)
        {
            RetroAudio.SetMasterVolume(changedVolume);
        }

        if (DrawMinimalButton(
            new Rect(x, y + 176.0f, contentWidth, 50.0f),
            "뒤로",
            new Color(0.66f, 0.69f, 0.72f, 1.0f)))
        {
            menuState = MenuState.Main;
        }
    }

    private Rect GetLargePanelRect()
    {
        float width = Mathf.Min(
            Mathf.Max(1.0f, Screen.width - 24.0f),
            Mathf.Clamp(Screen.width * 0.78f, 560.0f, 1040.0f)
        );
        float height = Mathf.Min(
            Mathf.Max(1.0f, Screen.height - 24.0f),
            Mathf.Clamp(Screen.height * 0.48f, 350.0f, 540.0f)
        );
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height * 0.46f;

        if (y + height > Screen.height - 12.0f)
        {
            y = Screen.height - height - 12.0f;
        }

        return new Rect(x, y, width, height);
    }

    private Rect GetMediumPanelRect(float maximumWidth, float maximumHeight)
    {
        float width = Mathf.Min(Mathf.Max(1.0f, Screen.width - 24.0f), maximumWidth);
        float height = Mathf.Min(Mathf.Max(1.0f, Screen.height - 24.0f), maximumHeight);
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height * 0.49f;

        if (y + height > Screen.height - 12.0f)
        {
            y = Screen.height - height - 12.0f;
        }

        return new Rect(x, y, width, height);
    }

    private Rect GetCardRect(Rect panelRect, int index)
    {
        float margin = Mathf.Clamp(panelRect.width * 0.045f, 14.0f, 44.0f);
        float gap = Mathf.Clamp(panelRect.width * 0.020f, 8.0f, 20.0f);
        float width = (panelRect.width - margin * 2.0f - gap * 2.0f) / 3.0f;

        return new Rect(
            panelRect.x + margin + index * (width + gap),
            panelRect.y + panelRect.height * 0.34f,
            width,
            panelRect.height * 0.43f
        );
    }

    private string GetShortDifficultyDescription(int difficultyIndex)
    {
        switch (difficultyIndex)
        {
            case 0:
                return "공격력 -1  ·  공격 간격 +35%";
            case 2:
                return "공격력 +1  ·  공격 간격 -25%";
            default:
                return "기본 수치";
        }
    }

    private void DrawHeading(Rect panelRect, string title, string subtitle)
    {
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(panelRect.height * 0.10f), 24, 44);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.clipping = TextClipping.Overflow;
        titleStyle.normal.textColor = goldColor;

        GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(panelRect.height * 0.044f), 14, 22);
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.wordWrap = true;
        subtitleStyle.clipping = TextClipping.Overflow;
        subtitleStyle.normal.textColor = new Color(0.78f, 0.72f, 0.84f, 1.0f);

        Rect titleRect = new Rect(
            panelRect.x + 12.0f,
            panelRect.y + panelRect.height * 0.06f,
            panelRect.width - 24.0f,
            panelRect.height * 0.16f
        );
        Rect subtitleRect = new Rect(
            panelRect.x + 12.0f,
            panelRect.y + panelRect.height * 0.21f,
            panelRect.width - 24.0f,
            panelRect.height * 0.11f
        );

        FitSingleLine(titleStyle, title, titleRect, 7);
        FitWrapped(subtitleStyle, subtitle, subtitleRect, 7);
        GUI.Label(titleRect, title, titleStyle);
        GUI.Label(subtitleRect, subtitle, subtitleStyle);
    }

    private bool DrawChoiceCard(Rect rect, string title, string description, Color accent)
    {
        bool hover = rect.Contains(Event.current.mousePosition);

        DrawSolidRect(new Rect(rect.x + 5.0f, rect.y + 7.0f, rect.width, rect.height), new Color(0.0f, 0.0f, 0.0f, 0.42f));
        DrawSolidRect(
            rect,
            true == hover
                ? new Color(0.18f, 0.075f, 0.23f, 0.99f)
                : new Color(0.075f, 0.035f, 0.10f, 0.98f)
        );
        DrawBorder(rect, true == hover ? 4.0f : 2.0f, accent);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 7.0f), accent);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.16f), 20, 38);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.clipping = TextClipping.Overflow;
        titleStyle.normal.textColor = accent;

        GUIStyle descriptionStyle = new GUIStyle(GUI.skin.label);
        descriptionStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.085f), 13, 20);
        descriptionStyle.alignment = TextAnchor.MiddleCenter;
        descriptionStyle.wordWrap = true;
        descriptionStyle.clipping = TextClipping.Overflow;
        descriptionStyle.normal.textColor = textColor;

        Rect titleRect = new Rect(
            rect.x + 8.0f,
            rect.y + rect.height * 0.13f,
            rect.width - 16.0f,
            rect.height * 0.30f
        );
        Rect descriptionRect = new Rect(
            rect.x + 10.0f,
            rect.y + rect.height * 0.43f,
            rect.width - 20.0f,
            rect.height * 0.48f
        );

        FitSingleLine(titleStyle, title, titleRect, 6);
        FitWrapped(descriptionStyle, description, descriptionRect, 6);
        GUI.Label(titleRect, title, titleStyle);
        GUI.Label(descriptionRect, description, descriptionStyle);

        return ConsumeClick(rect);
    }

    private bool DrawMinimalButton(Rect rect, string text, Color accent)
    {
        bool hover = rect.Contains(Event.current.mousePosition);

        if (true == hover)
        {
            DrawSolidRect(
                new Rect(rect.x + rect.width * 0.18f, rect.yMax - 2.0f, rect.width * 0.64f, 1.0f),
                new Color(accent.r, accent.g, accent.b, 0.72f)
            );
        }

        GUIStyle glowStyle = new GUIStyle(GUI.skin.label);
        glowStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.64f), 20, 35);
        glowStyle.fontStyle = FontStyle.Bold;
        glowStyle.alignment = TextAnchor.MiddleCenter;
        glowStyle.clipping = TextClipping.Overflow;
        glowStyle.normal.textColor = true == hover
            ? new Color(0.52f, 0.80f, 0.84f, 0.40f)
            : new Color(0.0f, 0.0f, 0.0f, 0.52f);
        FitSingleLine(glowStyle, text, rect, 7);

        GUIStyle textStyle = new GUIStyle(glowStyle);
        textStyle.normal.textColor = true == hover
            ? new Color(0.90f, 1.0f, 1.0f, 1.0f)
            : accent;

        GUI.Label(new Rect(rect.x + 1.0f, rect.y + 2.0f, rect.width, rect.height), text, glowStyle);
        if (true == hover)
        {
            GUI.Label(new Rect(rect.x - 1.0f, rect.y, rect.width, rect.height), text, glowStyle);
        }
        GUI.Label(rect, text, textStyle);

        return ConsumeClick(rect);
    }

    private void DrawMinimalHeading(Rect rect, string text)
    {
        GUIStyle shadowStyle = new GUIStyle(GUI.skin.label);
        shadowStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.74f), 22, 38);
        shadowStyle.fontStyle = FontStyle.Bold;
        shadowStyle.alignment = TextAnchor.MiddleCenter;
        shadowStyle.clipping = TextClipping.Overflow;
        shadowStyle.normal.textColor = new Color(0.0f, 0.0f, 0.0f, 0.72f);
        FitSingleLine(shadowStyle, text, rect, 8);

        GUIStyle textStyle = new GUIStyle(shadowStyle);
        textStyle.normal.textColor = new Color(0.82f, 0.86f, 0.88f, 1.0f);

        GUI.Label(new Rect(rect.x + 2.0f, rect.y + 3.0f, rect.width, rect.height), text, shadowStyle);
        GUI.Label(rect, text, textStyle);
    }

    private void DrawMinimalDescription(Rect rect, string text)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.48f), 13, 19);
        style.alignment = TextAnchor.MiddleCenter;
        style.clipping = TextClipping.Overflow;
        style.normal.textColor = new Color(0.58f, 0.62f, 0.66f, 1.0f);
        FitSingleLine(style, text, rect, 7);
        GUI.Label(rect, text, style);
    }

    private bool DrawDifficultyRow(Rect rect, string title, string description, Color accent)
    {
        bool hover = rect.Contains(Event.current.mousePosition);

        DrawSolidRect(
            rect,
            true == hover
                ? new Color(0.20f, 0.09f, 0.20f, 0.90f)
                : new Color(0.075f, 0.035f, 0.09f, 0.78f)
        );
        DrawBorder(rect, true == hover ? 3.0f : 1.0f, accent);
        DrawSolidRect(new Rect(rect.x, rect.y, 5.0f, rect.height), accent);

        float titleWidth = rect.width * 0.31f;
        Rect titleRect = new Rect(rect.x + 12.0f, rect.y, titleWidth - 12.0f, rect.height);
        Rect descriptionRect = new Rect(
            rect.x + titleWidth,
            rect.y,
            rect.width - titleWidth - 12.0f,
            rect.height
        );

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.40f), 15, 25);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.clipping = TextClipping.Overflow;
        titleStyle.normal.textColor = accent;

        GUIStyle descriptionStyle = new GUIStyle(GUI.skin.label);
        descriptionStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.27f), 11, 17);
        descriptionStyle.alignment = TextAnchor.MiddleCenter;
        descriptionStyle.clipping = TextClipping.Overflow;
        descriptionStyle.normal.textColor = textColor;

        FitSingleLine(titleStyle, title, titleRect, 7);
        FitSingleLine(descriptionStyle, description, descriptionRect, 7);
        GUI.Label(titleRect, title, titleStyle);
        GUI.Label(descriptionRect, description, descriptionStyle);

        return ConsumeClick(rect);
    }

    private bool DrawImageButton(Rect rect, Texture2D texture, string fallbackText, Color accent)
    {
        bool hover = rect.Contains(Event.current.mousePosition);

        DrawSolidRect(
            new Rect(rect.x + 3.0f, rect.y + 4.0f, rect.width, rect.height),
            new Color(0.0f, 0.0f, 0.0f, 0.28f)
        );
        DrawSolidRect(
            rect,
            true == hover
                ? new Color(0.20f, 0.09f, 0.20f, 0.82f)
                : new Color(0.07f, 0.03f, 0.08f, 0.64f)
        );
        DrawBorder(rect, true == hover ? 3.0f : 1.0f, accent);

        if (null != texture)
        {
            float textureRatio = (float)texture.width / texture.height;
            float width = Mathf.Min(rect.width * 0.86f, rect.height * 0.72f * textureRatio);
            float height = width / textureRatio;
            Rect imageRect = new Rect(
                rect.x + (rect.width - width) * 0.5f,
                rect.y + (rect.height - height) * 0.5f,
                width,
                height
            );
            GUI.DrawTexture(imageRect, texture, ScaleMode.ScaleToFit, true);
        }
        else
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.40f), 14, 27);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.clipping = TextClipping.Overflow;
            style.normal.textColor = textColor;
            FitSingleLine(style, fallbackText, rect, 7);
            GUI.Label(rect, fallbackText, style);
        }

        return ConsumeClick(rect);
    }

    private bool DrawButton(Rect rect, string text, Color accent)
    {
        bool hover = rect.Contains(Event.current.mousePosition);

        DrawSolidRect(new Rect(rect.x + 4.0f, rect.y + 5.0f, rect.width, rect.height), new Color(0.0f, 0.0f, 0.0f, 0.38f));
        DrawSolidRect(
            rect,
            true == hover
                ? new Color(0.22f, 0.09f, 0.27f, 0.99f)
                : new Color(0.08f, 0.035f, 0.105f, 0.98f)
        );
        DrawBorder(rect, true == hover ? 3.0f : 2.0f, accent);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.43f), 17, 29);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.clipping = TextClipping.Overflow;
        style.normal.textColor = true == hover ? Color.white : textColor;
        FitSingleLine(style, text, rect, 7);
        GUI.Label(rect, text, style);

        return ConsumeClick(rect);
    }

    private void DrawBackButton(Rect panelRect, MenuState destination)
    {
        float width = Mathf.Clamp(panelRect.width * 0.18f, 120.0f, 175.0f);
        float height = Mathf.Clamp(panelRect.height * 0.10f, 42.0f, 58.0f);
        Rect rect = new Rect(
            panelRect.x + 26.0f,
            panelRect.yMax - height - 22.0f,
            width,
            height
        );

        if (DrawButton(rect, "← 뒤로", new Color(0.56f, 0.46f, 0.65f, 1.0f)))
        {
            menuState = destination;
        }
    }

    private void DrawPanel(Rect rect)
    {
        DrawSolidRect(new Rect(rect.x + 6.0f, rect.y + 8.0f, rect.width, rect.height), new Color(0.0f, 0.0f, 0.0f, 0.32f));
        DrawSolidRect(rect, panelColor);
        DrawBorder(rect, 2.0f, panelBorderColor);
        DrawBorder(
            new Rect(rect.x + 5.0f, rect.y + 5.0f, rect.width - 10.0f, rect.height - 10.0f),
            1.0f,
            new Color(0.56f, 0.38f, 0.48f, 0.42f)
        );
    }

    private bool ConsumeClick(Rect rect)
    {
        if (EventType.MouseDown != Event.current.type
            || 0 != Event.current.button
            || false == rect.Contains(Event.current.mousePosition))
        {
            return false;
        }

        Event.current.Use();
        return true;
    }

    private string GetSelectedCharacterName()
    {
        int index = Mathf.Clamp(GameData.selectedCharacter, 0, CharacterNames.Length - 1);
        return CharacterNames[index];
    }

    private Texture2D LoadTexture(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        return null == sprite ? null : sprite.texture;
    }

    private void FitSingleLine(GUIStyle style, string text, Rect rect, int minimumSize)
    {
        GUIContent content = new GUIContent(text);
        float width = Mathf.Max(1.0f, rect.width - 12.0f);
        float height = Mathf.Max(1.0f, rect.height - 10.0f);

        while (style.fontSize > minimumSize)
        {
            Vector2 size = style.CalcSize(content);
            if (size.x <= width && size.y <= height)
            {
                break;
            }

            style.fontSize--;
        }
    }

    private void FitWrapped(GUIStyle style, string text, Rect rect, int minimumSize)
    {
        GUIContent content = new GUIContent(text);
        float height = Mathf.Max(1.0f, rect.height - 10.0f);

        while (style.fontSize > minimumSize
            && style.CalcHeight(content, Mathf.Max(1.0f, rect.width - 8.0f)) > height)
        {
            style.fontSize--;
        }
    }

    private void StartGame(int difficultyIndex)
    {
        GameData.difficulty = difficultyIndex;
        GameData.elapsedTime = 0.0f;
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(gameSceneName);
    }

    private void DrawBackground()
    {
        if (null == background)
        {
            DrawSolidRect(
                new Rect(0.0f, 0.0f, Screen.width, Screen.height),
                new Color(0.035f, 0.018f, 0.05f, 1.0f)
            );
            return;
        }

        float screenRatio = (float)Screen.width / Screen.height;
        float imageRatio = (float)background.width / background.height;
        float drawWidth = Screen.width;
        float drawHeight = Screen.height;

        if (screenRatio > imageRatio)
        {
            drawHeight = Screen.width / imageRatio;
        }
        else
        {
            drawWidth = Screen.height * imageRatio;
        }

        float x = (Screen.width - drawWidth) * 0.5f;
        float y = (Screen.height - drawHeight) * 0.5f;
        GUI.DrawTexture(new Rect(x, y, drawWidth, drawHeight), background, ScaleMode.StretchToFill);
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
