using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    private enum MenuState { Main, Character, Difficulty, Settings }

    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string backgroundSpriteName = "Sprites/TitleBackground";

    private static readonly string[] CharacterNames = { "궁수", "도적", "마법사" };
    private static readonly string[] DifficultyNames = { "쉬움", "보통", "어려움" };
    private Texture2D background;
    private MenuState menuState;
    private readonly IronicMenuStyle menu = new IronicMenuStyle();

    private void Awake()
    {
        Sprite sprite = Resources.Load<Sprite>(backgroundSpriteName);
        background = sprite != null ? sprite.texture : Resources.Load<Texture2D>(backgroundSpriteName);
    }

    private void OnDestroy() => menu.Dispose();

    private void OnGUI()
    {
        // Each menu owns its styles; changing GUI.skin.font also changed unrelated HUDs.
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.color = Color.white;
        try
        {
            menu.Begin();
            DrawBackground();
            HandleBackKey();

            switch (menuState)
            {
                case MenuState.Character: DrawCharacterMenu(); break;
                case MenuState.Difficulty: DrawDifficultyMenu(); break;
                case MenuState.Settings: DrawSettingsMenu(); break;
                default: DrawMainMenu(); break;
            }
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }

    private void HandleBackKey()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown || e.keyCode != KeyCode.Escape) return;
        if (menuState == MenuState.Difficulty) menuState = MenuState.Character;
        else menuState = MenuState.Main;
        GUIUtility.keyboardControl = 0;
        e.Use();
    }

    private void DrawMainMenu()
    {
        float y = menu.Height * 0.48f;
        if (menu.Button(Row(y), "게임 시작")) ChangeMenu(MenuState.Character);
        if (menu.Button(Row(y + 88), "환경설정")) ChangeMenu(MenuState.Settings);
        if (menu.Button(Row(y + 176), "나가기")) Quit();
    }

    private void DrawCharacterMenu()
    {
        float y = menu.Height * 0.36f;
        menu.Heading(Row(y, 60), "직업 선택");
        for (int i = 0; i < CharacterNames.Length; i++)
        {
            if (menu.Button(Row(y + 88 + i * 80), CharacterNames[i]))
            {
                GameData.selectedCharacter = i;
                ChangeMenu(MenuState.Difficulty);
            }
        }
        if (menu.Button(Row(y + 354, 58), "뒤로", true)) ChangeMenu(MenuState.Main);
    }

    private void DrawDifficultyMenu()
    {
        float y = menu.Height * 0.36f;
        menu.Heading(Row(y, 60), "난이도 선택");
        int hovered = -1;
        for (int i = 0; i < DifficultyNames.Length; i++)
        {
            Rect rect = Row(y + 84 + i * 78);
            if (rect.Contains(Event.current.mousePosition)) hovered = i;
            if (menu.Button(rect, DifficultyNames[i])) StartGame(i);
        }

        string description = hovered < 0
            ? "선택한 직업 · " + CharacterNames[Mathf.Clamp(GameData.selectedCharacter, 0, 2)]
            : hovered == 0 ? "몬스터 공격력 -1 · 공격 간격 +35%"
            : hovered == 2 ? "몬스터 공격력 +1 · 공격 간격 -25%"
            : "몬스터 기본 공격력 · 기본 공격 간격";
        menu.Caption(Row(y + 318, 38), description);
        if (menu.Button(Row(y + 378, 58), "뒤로", true)) ChangeMenu(MenuState.Character);
    }

    private void DrawSettingsMenu()
    {
        float y = menu.Height * 0.40f;
        menu.Heading(Row(y, 60), "환경설정");
        float volume = RetroAudio.MasterVolume;
        menu.Caption(Row(y + 98, 42), $"전체 음량  {Mathf.RoundToInt(volume * 100)}%");
        float changedVolume = menu.VolumeSlider(Row(y + 154, 32, 260), volume);
        if (Mathf.Abs(changedVolume - volume) > 0.001f) RetroAudio.SetMasterVolume(changedVolume);
        if (menu.Button(Row(y + 226, 64), "뒤로", true)) ChangeMenu(MenuState.Main);
    }

    private Rect Row(float y, float height = 72, float width = 460)
    {
        return new Rect((menu.Width - width) * 0.5f, y, width, height);
    }

    private void ChangeMenu(MenuState destination)
    {
        menuState = destination;
        GUIUtility.keyboardControl = 0;
    }

    private void StartGame(int difficulty)
    {
        GameData.difficulty = difficulty;
        GameData.elapsedTime = 0;
        Time.timeScale = 1;
        SceneManager.LoadScene(gameSceneName);
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DrawBackground()
    {
        Rect screen = new Rect(0, 0, menu.Width, menu.Height);
        if (background != null) GUI.DrawTexture(screen, background, ScaleMode.ScaleAndCrop);
        else IronicMenuStyle.Fill(screen, new Color32(20, 12, 24, 255));
        IronicMenuStyle.Fill(screen, new Color(0.035f, 0.015f, 0.045f, 0.08f));
    }
}

// Shared borderless typography for title / character / difficulty / pause.
// The font is bundled with the project so every computer gets the same Hangul.
internal sealed class IronicMenuStyle
{
    private Font font;
    private GUIStyle label;
    private GUIStyle hitTarget;
    private GUIStyle slider;
    private GUIStyle thumb;
    private Texture2D trackTexture;
    private Texture2D thumbTexture;
    private Texture2D activeThumbTexture;

    private static readonly Color Face = new Color32(184, 145, 167, 255);
    private static readonly Color Highlight = new Color32(222, 184, 199, 255);
    private static readonly Color Shade = new Color32(89, 53, 78, 255);
    private static readonly Color Edge = new Color32(29, 16, 31, 255);

    public float Width { get; private set; }
    public float Height { get; private set; }

    public void Begin()
    {
        // Scaling the whole layout avoids 34px labels becoming tiny in a QHD game.
        float scale = Mathf.Max(0.01f, Mathf.Min(Screen.width / 1600f, Screen.height / 900f));
        Width = Screen.width / scale;
        Height = Screen.height / scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        EnsureStyles();
    }

    private void EnsureStyles()
    {
        if (label != null) return;
        font = Resources.Load<Font>("Fonts/Galmuri11-Bold");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        label = new GUIStyle
        {
            font = font,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Overflow,
            wordWrap = false,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0)
        };
        // A real IMGUI control preserves release-to-activate and keyboard focus.
        hitTarget = new GUIStyle { padding = new RectOffset(), margin = new RectOffset() };
        trackTexture = SolidTexture(new Color32(84, 57, 79, 255));
        thumbTexture = SolidTexture(Face);
        activeThumbTexture = SolidTexture(Highlight);
        slider = new GUIStyle { fixedHeight = 4, margin = new RectOffset(0, 0, 14, 0) };
        slider.normal.background = trackTexture;
        thumb = new GUIStyle { fixedWidth = 8, fixedHeight = 18, margin = new RectOffset(0, 0, -7, 0) };
        thumb.normal.background = thumbTexture;
        thumb.hover.background = activeThumbTexture;
        thumb.active.background = activeThumbTexture;
    }

    public bool Button(Rect rect, string text, bool secondary = false)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        string controlName = "IronicMenu." + text;
        GUI.SetNextControlName(controlName);
        bool clicked = GUI.Button(rect, GUIContent.none, hitTarget);
        bool focused = GUI.GetNameOfFocusedControl() == controlName;
        float strength = secondary ? 0.74f : 1;
        DrawStoneText(rect, text, secondary ? 30 : 42, hovered || focused, strength);
        return clicked;
    }

    public void Heading(Rect rect, string text)
    {
        DrawStoneText(rect, text, 36, false, 0.88f);
    }

    public void Caption(Rect rect, string text)
    {
        label.fontSize = 20;
        label.normal.textColor = new Color32(176, 159, 176, 255);
        GUI.Label(rect, text, label);
    }

    private void DrawStoneText(Rect rect, string text, int size, bool hovered, float strength)
    {
        label.fontSize = size;
        // Pixel font + restrained relief echoes the stone logo without a button box.
        label.normal.textColor = new Color(0.02f, 0.01f, 0.025f, 0.85f);
        GUI.Label(Offset(rect, 3, 5), text, label);
        label.normal.textColor = Edge;
        GUI.Label(Offset(rect, -1, 0), text, label);
        GUI.Label(Offset(rect, 1, 0), text, label);
        GUI.Label(Offset(rect, 0, -1), text, label);
        GUI.Label(Offset(rect, 0, 2), text, label);
        label.normal.textColor = Shade;
        GUI.Label(Offset(rect, 0, 3), text, label);
        label.normal.textColor = WithAlpha(hovered ? Highlight : Color.Lerp(Shade, Highlight, 0.48f), strength);
        GUI.Label(Offset(rect, 0, -1), text, label);
        label.normal.textColor = WithAlpha(hovered ? Color.Lerp(Face, Highlight, 0.65f) : Face, strength);
        GUI.Label(rect, text, label);
    }

    public float VolumeSlider(Rect rect, float value)
    {
        float result = GUI.HorizontalSlider(rect, value, 0, 1, slider, thumb);
        if (Event.current.type == EventType.Repaint)
            Fill(new Rect(rect.x, rect.y + 14, (rect.width - 8) * result, 2), new Color32(154, 111, 142, 255));
        return result;
    }

    public void Dispose()
    {
        if (trackTexture != null) Object.Destroy(trackTexture);
        if (thumbTexture != null) Object.Destroy(thumbTexture);
        if (activeThumbTexture != null) Object.Destroy(activeThumbTexture);
        label = null;
    }

    private static Texture2D SolidTexture(Color color)
    {
        var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static Rect Offset(Rect rect, float x, float y) => new Rect(rect.x + x, rect.y + y, rect.width, rect.height);
    private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }

    public static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
