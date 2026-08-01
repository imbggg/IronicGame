using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string[] characterNames = { "궁수", "도적", "마법사" };

    private bool selectMode = false;

    private void OnGUI()
    {
        float width = 240.0f;
        float height = 60.0f;
        float gap = 12.0f;
        float centerX = Screen.width * 0.5f - width * 0.5f;

        GUI.skin.label.fontSize = 28;
        GUI.skin.button.fontSize = 20;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;

        if (false == selectMode)
        {
            GUI.Label(new Rect(centerX, Screen.height * 0.25f, width, 50.0f), "IRONIC");

            float startY = Screen.height * 0.45f;

            if (true == GUI.Button(new Rect(centerX, startY, width, height), "게임 시작"))
            {
                selectMode = true;
            }

            if (true == GUI.Button(new Rect(centerX, startY + (height + gap), width, height), "나가기"))
            {
                Application.Quit();
            }
        }
        else
        {
            GUI.Label(new Rect(centerX, Screen.height * 0.2f, width, 50.0f), "캐릭터 선택");

            float startY = Screen.height * 0.35f;

            for (int i = 0; i < characterNames.Length; i++)
            {
                Rect rect = new Rect(centerX, startY + i * (height + gap), width, height);

                if (true == GUI.Button(rect, characterNames[i]))
                {
                    GameData.selectedCharacter = i;
                    SceneManager.LoadScene(gameSceneName);
                }
            }

            float backY = startY + characterNames.Length * (height + gap) + gap;

            if (true == GUI.Button(new Rect(centerX, backY, width, height), "뒤로"))
            {
                selectMode = false;
            }
        }
    }
}