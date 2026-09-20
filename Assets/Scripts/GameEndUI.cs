using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndUI : MonoBehaviour
{
    private const string DefeatText = "패배";
    private const string ClearText = "던전 클리어!";

    private static GameEndUI instance;

    [SerializeField]
    [Tooltip("타이틀(게임 시작 메뉴) 씬 이름. Build Settings에 등록돼 있어야 한다.")]
    private string titleSceneName = "Title";

    [SerializeField]
    private string backgroundSpriteName = "Sprites/TitleBackground";

    private bool showing;
    private string resultText;
    private Texture2D background;
    private Texture2D defeatTitle;
    private readonly IronicMenuStyle menu = new IronicMenuStyle();

    public static bool IsShowing
    {
        get { return null != instance && instance.showing; }
    }

    private void Awake()
    {
        if (null != instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        background = LoadTexture(backgroundSpriteName);
        defeatTitle = LoadTexture("Sprites/defeat");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        menu.Dispose();
    }

    public static void ShowClear()
    {
        EnsureInstance().Show(ClearText);
    }

    public static void ShowGameOver()
    {
        EnsureInstance().Show(DefeatText);
    }

    private static GameEndUI EnsureInstance()
    {
        if (null != instance)
        {
            return instance;
        }

        GameObject uiObject = new GameObject("GameEndUI");
        instance = uiObject.AddComponent<GameEndUI>();
        return instance;
    }

    private void Show(string message)
    {
        if (true == showing)
        {
            return;
        }

        resultText = message;
        showing = true;
        Time.timeScale = 0.0f;
    }

    private void OnGUI()
    {
        if (false == showing)
        {
            return;
        }

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.color = Color.white;

        try
        {
            menu.Begin();
            DrawBackground();
            DrawResultMenu();
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }

    private void DrawBackground()
    {
        Rect screen = new Rect(0.0f, 0.0f, menu.Width, menu.Height);
        if (null != background)
        {
            GUI.DrawTexture(screen, background, ScaleMode.ScaleAndCrop);
        }
        else
        {
            IronicMenuStyle.Fill(screen, new Color32(20, 12, 24, 255));
        }

        // 시작 화면보다 어둡게 덮어 게임 결과에 시선이 모이게 한다.
        IronicMenuStyle.Fill(screen, new Color(0.025f, 0.008f, 0.035f, 0.58f));
    }

    private void DrawResultMenu()
    {
        const float panelWidth = 620.0f;
        const float panelHeight = 500.0f;
        float x = (menu.Width - panelWidth) * 0.5f;
        float y = (menu.Height - panelHeight) * 0.5f;

        Rect panel = new Rect(x, y, panelWidth, panelHeight);
        IronicMenuStyle.Fill(panel, new Color(0.055f, 0.025f, 0.070f, 0.76f));

        Texture2D resultTitle = DefeatText == resultText ? defeatTitle : null;
        Rect titleArea = new Rect(x + 90.0f, y + 48.0f, panelWidth - 180.0f, 118.0f);

        if (null != resultTitle)
        {
            GUI.DrawTexture(FitTexture(titleArea, resultTitle), resultTitle, ScaleMode.StretchToFill, true);
        }
        else
        {
            menu.Heading(titleArea, resultText, 58);
        }

        menu.Caption(
            new Rect(x + 60.0f, y + 178.0f, panelWidth - 120.0f, 42.0f),
            $"난이도 {GameData.GetDifficultyName()}  ·  기록 {GameData.FormatElapsedTime()}");

        Rect retryRect = new Rect(x + 80.0f, y + 258.0f, panelWidth - 160.0f, 72.0f);
        Rect quitRect = new Rect(x + 80.0f, y + 350.0f, panelWidth - 160.0f, 72.0f);

        if (menu.Button(retryRect, "다시 하기"))
        {
            Time.timeScale = 1.0f;
            showing = false;
            GameData.elapsedTime = 0.0f;
            SceneManager.LoadScene(titleSceneName);
        }

        if (menu.Button(quitRect, "게임 종료", true))
        {
            Time.timeScale = 1.0f;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private static Texture2D LoadTexture(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        return null != sprite ? sprite.texture : Resources.Load<Texture2D>(resourcePath);
    }

    private static Rect FitTexture(Rect area, Texture2D texture)
    {
        float textureRatio = (float)texture.width / texture.height;
        float areaRatio = area.width / area.height;

        if (textureRatio > areaRatio)
        {
            float height = area.width / textureRatio;
            return new Rect(area.x, area.center.y - height * 0.5f, area.width, height);
        }

        float width = area.height * textureRatio;
        return new Rect(area.center.x - width * 0.5f, area.y, width, area.height);
    }
}
