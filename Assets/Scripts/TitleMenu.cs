using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string backgroundSpriteName = "Sprites/TitleBackground";

    [SerializeField] private float menuScale = 0.5f;
    [SerializeField] private float menuGap = 20.0f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float menuGapHorizontal = 60.0f;

    private Texture2D background;

    private Texture2D texStart;
    private Texture2D texQuit;
    private Texture2D texArcher;
    private Texture2D texRogue;
    private Texture2D texMage;
    private Texture2D texBack;

    private bool selectMode = false;

    private void Awake()
    {
        background = LoadTexture(backgroundSpriteName);

        texStart = LoadTexture("Sprites/Menu.Start");
        texQuit = LoadTexture("Sprites/Menu.Quit");
        texArcher = LoadTexture("Sprites/Menu.Archer");
        texRogue = LoadTexture("Sprites/Menu.Rogue");
        texMage = LoadTexture("Sprites/Menu.Mage");
        texBack = LoadTexture("Sprites/Menu.Back");
    }

    private Texture2D LoadTexture(string path)
    {
        Sprite sprite = Resources.Load<Sprite>(path);
        if (null == sprite)
        {
            return null;
        }

        return sprite.texture;
    }

    private void OnGUI()
    {
        DrawBackground();

        if (false == selectMode)
        {
            float totalHeight = GetHeight(texStart) + GetHeight(texQuit) + menuGap;
            float y = Screen.height * 0.62f - totalHeight * 0.5f;

            if (true == DrawMenuItem(texStart, ref y))
            {
                selectMode = true;
            }

            if (true == DrawMenuItem(texQuit, ref y))
            {
                Application.Quit();
            }
        }
        else
        {
            Texture2D[] characters = { texArcher, texRogue, texMage };

            float rowWidth = 0.0f;
            float rowHeight = 0.0f;

            for (int i = 0; i < characters.Length; i++)
            {
                rowWidth += GetWidth(characters[i]);
                rowHeight = Mathf.Max(rowHeight, GetHeight(characters[i]));
            }

            rowWidth += menuGapHorizontal * (characters.Length - 1);

            float rowY = Screen.height * 0.68f - rowHeight * 0.5f;
            float x = Screen.width * 0.5f - rowWidth * 0.5f;

            for (int i = 0; i < characters.Length; i++)
            {
                float itemHeight = GetHeight(characters[i]);
                float itemY = rowY + (rowHeight - itemHeight) * 0.5f;

                if (true == DrawMenuItemAt(characters[i], x, itemY))
                {
                    StartGame(i);
                }

                x += GetWidth(characters[i]) + menuGapHorizontal;
            }

            float backY = rowY + rowHeight + menuGap;

            if (true == DrawMenuItem(texBack, ref backY))
            {
                selectMode = false;
            }
        }
    }

    private void StartGame(int characterIndex)
    {
        GameData.selectedCharacter = characterIndex;
        SceneManager.LoadScene(gameSceneName);
    }

    private float GetHeight(Texture2D texture)
    {
        if (null == texture)
        {
            return 0.0f;
        }

        return texture.height * menuScale;
    }

    private float GetWidth(Texture2D texture)
    {
        if (null == texture)
        {
            return 0.0f;
        }

        return texture.width * menuScale;
    }

    private bool DrawMenuItemAt(Texture2D texture, float x, float y)
    {
        if (null == texture)
        {
            return false;
        }

        float width = texture.width * menuScale;
        float height = texture.height * menuScale;

        Rect rect = new Rect(x, y, width, height);
        bool hover = rect.Contains(Event.current.mousePosition);

        Rect drawRect = rect;
        if (true == hover)
        {
            float grownWidth = width * hoverScale;
            float grownHeight = height * hoverScale;

            drawRect = new Rect(
                x - (grownWidth - width) * 0.5f,
                y - (grownHeight - height) * 0.5f,
                grownWidth,
                grownHeight
            );
        }

        GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill);

        if (Event.current.type == EventType.MouseDown && true == hover)
        {
            Event.current.Use();
            return true;
        }

        return false;
    }

    private bool DrawMenuItem(Texture2D texture, ref float y)
    {
        if (null == texture)
        {
            return false;
        }

        float width = texture.width * menuScale;
        float height = texture.height * menuScale;
        float x = Screen.width * 0.5f - width * 0.5f;

        Rect rect = new Rect(x, y, width, height);
        bool hover = rect.Contains(Event.current.mousePosition);

        Rect drawRect = rect;
        if (true == hover)
        {
            float grownWidth = width * hoverScale;
            float grownHeight = height * hoverScale;

            drawRect = new Rect(
                Screen.width * 0.5f - grownWidth * 0.5f,
                y - (grownHeight - height) * 0.5f,
                grownWidth,
                grownHeight
            );
        }

        GUI.DrawTexture(drawRect, texture, ScaleMode.StretchToFill);

        bool clicked = false;
        if (Event.current.type == EventType.MouseDown && true == hover)
        {
            clicked = true;
            Event.current.Use();
        }

        y += height + menuGap;

        return clicked;
    }

    private void DrawBackground()
    {
        if (null == background)
        {
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
}
