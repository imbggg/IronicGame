using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    private static PauseMenuUI instance;
    [SerializeField] private string titleSceneName = "Title";
    private DungeonGenerator generator;
    private TitleMenu titleMenu;
    private bool paused;
    private readonly IronicMenuStyle menu = new IronicMenuStyle();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAutomatically()
    {
        if (FindAnyObjectByType<PauseMenuUI>() != null) return;
        new GameObject("PauseMenuUI").AddComponent<PauseMenuUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        menu.Dispose();
        if (instance == this) instance = null;
    }

    private void Update()
    {
        titleMenu = FindAnyObjectByType<TitleMenu>();
        if (titleMenu != null && titleMenu.enabled) { paused = false; return; }
        if (generator == null) generator = FindAnyObjectByType<DungeonGenerator>();
        if (generator == null) { paused = false; return; }
        if (GameEndUI.IsShowing || IronicRewardUI.IsShowing) return;
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!paused);
    }

    private void SetPaused(bool shouldPause)
    {
        paused = shouldPause;
        Time.timeScale = paused ? 0 : 1;
    }

    private void OnGUI()
    {
        if (!paused || (titleMenu != null && titleMenu.enabled)) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        GUI.color = Color.white;
        try
        {
            menu.Begin();
            IronicMenuStyle.Fill(new Rect(0, 0, menu.Width, menu.Height), new Color(0.04f, 0.02f, 0.055f, 0.78f));
            float y = (menu.Height - 480) * 0.5f;
            menu.Heading(Row(y, 64), "일시정지");

            float volume = RetroAudio.MasterVolume;
            menu.Caption(Row(y + 96, 40), $"전체 음량  {Mathf.RoundToInt(volume * 100)}%");
            float changedVolume = menu.VolumeSlider(Row(y + 146, 32, 260), volume);
            if (Mathf.Abs(changedVolume - volume) > 0.001f) RetroAudio.SetMasterVolume(changedVolume);

            if (menu.Button(Row(y + 218), "계속하기")) SetPaused(false);
            if (menu.Button(Row(y + 306), "직업 선택"))
            {
                SetPaused(false);
                generator = null;
                SceneManager.LoadScene(titleSceneName);
            }
            if (menu.Button(Row(y + 394), "게임 종료"))
            {
                SetPaused(false);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }
    }

    private Rect Row(float y, float height = 72, float width = 460)
    {
        return new Rect((menu.Width - width) * 0.5f, y, width, height);
    }
}
