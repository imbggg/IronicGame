using UnityEngine;

/// <summary>
/// 마법사의 X 스킬 타격 이펙트. 제자리에서 한 번 터지고 사라진다.
/// 데미지 판정은 Player가 처리하므로 여기서는 그리기만 한다.
/// 프리팹 없이 Player가 코드로 생성한다.
/// </summary>
public class MageSmashEffect : MonoBehaviour
{
    private const string ResourcePath = "Effects/MageSmash";
    private const int FrameSize = 256;
    private const int FrameCount = 8;

    private static Sprite[] cachedFrames;

    private SpriteRenderer spriteRenderer;
    private float animationFps;
    private float elapsed;

    public void Init(Vector2 facing, float fps, float pixelsPerUnit)
    {
        animationFps = fps;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (null == spriteRenderer)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 플레이어보다 앞에 그려야 타격감이 산다.
        spriteRenderer.sortingOrder = 25;

        // 이펙트가 오른쪽을 향해 그려져 있으므로 왼쪽 공격이면 뒤집는다.
        spriteRenderer.flipX = facing.x < 0.0f;

        LoadFrames(pixelsPerUnit);
        ApplyFrame();
    }

    private void LoadFrames(float pixelsPerUnit)
    {
        if (null != cachedFrames && 0 < cachedFrames.Length)
        {
            return;
        }

        Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
        if (null == texture)
        {
            Debug.LogError($"[MageSmashEffect] Cannot load texture: Resources/{ResourcePath}.png");
            cachedFrames = new Sprite[0];
            return;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int count = Mathf.Min(FrameCount, texture.width / FrameSize);
        cachedFrames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            cachedFrames[i] = Sprite.Create(
                texture,
                new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            cachedFrames[i].name = $"MageSmash_{i}";
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        ApplyFrame();

        if (elapsed >= GetDuration())
        {
            Destroy(gameObject);
        }
    }

    private void ApplyFrame()
    {
        if (null == cachedFrames || 0 == cachedFrames.Length || null == spriteRenderer)
        {
            return;
        }

        int index = Mathf.FloorToInt(elapsed * animationFps);
        index = Mathf.Clamp(index, 0, cachedFrames.Length - 1);
        spriteRenderer.sprite = cachedFrames[index];
    }

    private float GetDuration()
    {
        if (0.0f >= animationFps)
        {
            return 0.4f;
        }

        return FrameCount / animationFps;
    }
}
