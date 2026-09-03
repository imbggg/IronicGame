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

    private bool showing;
    private string resultText;

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
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
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

        Color previousColor = GUI.color;
        GUI.color = new Color(0.02f, 0.01f, 0.04f, 0.88f);
        GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        // 해상도가 4K까지 올라가므로 화면 높이에 비례시킨다.
        float panelWidth = Screen.height * 0.34f;
        float buttonHeight = Screen.height * 0.075f;
        float gap = Screen.height * 0.022f;
        float titleHeight = Screen.height * 0.09f;
        float recordHeight = Screen.height * 0.05f;

        float panelHeight =
            titleHeight + recordHeight + gap
            + buttonHeight * 2.0f + gap;

        float x = Screen.width * 0.5f - panelWidth * 0.5f;
        float y = Screen.height * 0.5f - panelHeight * 0.5f;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.055f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = DefeatText == resultText
            ? new Color(1.0f, 0.25f, 0.25f)
            : new Color(1.0f, 0.82f, 0.25f);

        GUIStyle recordStyle = new GUIStyle(GUI.skin.label);
        recordStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.026f);
        recordStyle.fontStyle = FontStyle.Bold;
        recordStyle.alignment = TextAnchor.MiddleCenter;
        recordStyle.normal.textColor = Color.white;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.030f);
        buttonStyle.fontStyle = FontStyle.Bold;

        GUI.Label(new Rect(x, y, panelWidth, titleHeight), resultText, titleStyle);

        GUI.Label(
            new Rect(x, y + titleHeight, panelWidth, recordHeight),
            $"난이도 {GameData.GetDifficultyName()}  |  기록 {GameData.FormatElapsedTime()}",
            recordStyle
        );

        float buttonY = y + titleHeight + recordHeight + gap;
        float buttonX = x + panelWidth * 0.12f;
        float buttonWidth = panelWidth * 0.76f;

        if (GUI.Button(
            new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
            "다시 하기",
            buttonStyle))
        {
            Time.timeScale = 1.0f;
            showing = false;

            // 시작 메뉴로 돌아가 직업과 난이도를 다시 고르게 한다.
            GameData.elapsedTime = 0.0f;
            SceneManager.LoadScene(titleSceneName);
        }

        buttonY += buttonHeight + gap;

        if (GUI.Button(
            new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
            "게임 종료",
            buttonStyle))
        {
            Time.timeScale = 1.0f;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
