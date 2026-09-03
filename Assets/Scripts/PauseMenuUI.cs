using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    private static PauseMenuUI instance;

    [SerializeField] private string titleSceneName = "Title";

    private DungeonGenerator generator;
    private TitleMenu titleMenu;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAutomatically()
    {
        if (null != FindAnyObjectByType<PauseMenuUI>())
        {
            return;
        }

        GameObject menuObject = new GameObject("PauseMenuUI");
        menuObject.AddComponent<PauseMenuUI>();
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

        // 타이틀 화면에서는 ESC로 일시정지 메뉴가 뜨면 안 된다.
        if (null != titleMenu && true == titleMenu.enabled)
        {
            paused = false;
            return;
        }

        if (null == generator)
        {
            generator = FindAnyObjectByType<DungeonGenerator>();
        }

        if (null == generator)
        {
            paused = false;
            return;
        }

        if (true == GameEndUI.IsShowing)
        {
            return;
        }

        if (true == IronicRewardUI.IsShowing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPaused(false == paused);
        }
    }

    private void SetPaused(bool shouldPause)
    {
        paused = shouldPause;
        Time.timeScale = true == paused ? 0.0f : 1.0f;
    }

    private void OnGUI()
    {
        if (false == paused)
        {
            return;
        }

        if (null != titleMenu && true == titleMenu.enabled)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = new Color(0.02f, 0.01f, 0.04f, 0.86f);
        GUI.DrawTexture(new Rect(0.0f, 0.0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        float panelWidth = 420.0f;
        float panelHeight = 500.0f;
        float x = Screen.width * 0.5f - panelWidth * 0.5f;
        float y = Screen.height * 0.5f - panelHeight * 0.5f;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 42;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.95f, 0.85f, 1.0f, 1.0f);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;
        buttonStyle.fontStyle = FontStyle.Bold;

        GUIStyle soundStyle = new GUIStyle(GUI.skin.label);
        soundStyle.fontSize = 20;
        soundStyle.fontStyle = FontStyle.Bold;
        soundStyle.alignment = TextAnchor.MiddleCenter;
        soundStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(x, y, panelWidth, 85.0f), "일시정지", titleStyle);

        float currentVolume = RetroAudio.MasterVolume;
        GUI.Label(
            new Rect(x + 60.0f, y + 88.0f, panelWidth - 120.0f, 36.0f),
            $"전체 사운드  {Mathf.RoundToInt(currentVolume * 100.0f)}%",
            soundStyle
        );

        float changedVolume = GUI.HorizontalSlider(
            new Rect(x + 70.0f, y + 128.0f, panelWidth - 140.0f, 28.0f),
            currentVolume,
            0.0f,
            1.0f
        );

        if (0.001f < Mathf.Abs(changedVolume - currentVolume))
        {
            RetroAudio.SetMasterVolume(changedVolume);
        }

        if (GUI.Button(new Rect(x + 60.0f, y + 175.0f, panelWidth - 120.0f, 60.0f), "계속하기", buttonStyle))
        {
            SetPaused(false);
        }

        if (GUI.Button(new Rect(x + 60.0f, y + 255.0f, panelWidth - 120.0f, 60.0f), "직업 선택", buttonStyle))
        {
            SetPaused(false);
            generator = null;
            SceneManager.LoadScene(titleSceneName);
        }

        if (GUI.Button(new Rect(x + 60.0f, y + 335.0f, panelWidth - 120.0f, 60.0f), "게임 종료", buttonStyle))
        {
            SetPaused(false);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
