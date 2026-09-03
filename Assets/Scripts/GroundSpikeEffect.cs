using UnityEngine;

/// <summary>
/// 도적의 X 스킬. 바닥에서 돌기둥이 솟아오르며 앞으로 나아간다.
/// 프리팹 없이 Player가 코드로 생성한다.
/// </summary>
public class GroundSpikeEffect : MonoBehaviour
{
    private const string ResourcePath = "Effects/GroundSpike";
    private const int FrameSize = 256;
    private const int FrameCount = 9;

    // 스프라이트 안에서 바닥선이 셀 밑면으로부터 36px 위에 있다.
    // 피벗을 거기에 맞춰야 돌기둥이 바닥에 붙어 보인다.
    private const float PivotY = 36.0f / FrameSize;

    private static Sprite[] cachedFrames;

    private SpriteRenderer spriteRenderer;
    private Vector2 direction;
    private Vector2 startPosition;

    private float pixelsPerUnit;
    private float speed;
    private float range;
    private float animationFps;
    private int damage;

    private float elapsed;

    private readonly System.Collections.Generic.HashSet<Monster> hitMonsters =
        new System.Collections.Generic.HashSet<Monster>();

    public void Init(
        Vector2 fireDirection,
        int attackDamage,
        float moveSpeed,
        float maxRange,
        float fps,
        float unitScale)
    {
        direction = fireDirection.normalized;
        startPosition = transform.position;
        damage = attackDamage;
        speed = moveSpeed;
        range = maxRange;
        animationFps = fps;
        pixelsPerUnit = unitScale;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (null == spriteRenderer)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 플레이어보다 뒤에, 바닥보다는 앞에 그린다.
        spriteRenderer.sortingOrder = 18;

        // 진행 방향이 왼쪽이면 좌우를 뒤집는다.
        spriteRenderer.flipX = direction.x < 0.0f;

        LoadFrames();
        ApplyFrame();
    }

    private void LoadFrames()
    {
        if (null != cachedFrames && 0 < cachedFrames.Length)
        {
            return;
        }

        Texture2D texture = Resources.Load<Texture2D>(ResourcePath);
        if (null == texture)
        {
            Debug.LogError($"[GroundSpikeEffect] Cannot load texture: Resources/{ResourcePath}.png");
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
                new Vector2(0.5f, PivotY),
                pixelsPerUnit
            );
            cachedFrames[i].name = $"GroundSpike_{i}";
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (0.0f < speed)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        ApplyFrame();
        DamageMonsters();

        bool animationDone = elapsed >= GetDuration();
        bool outOfRange =
            Vector2.Distance(startPosition, transform.position) >= range;

        if (true == animationDone || true == outOfRange)
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

    private void DamageMonsters()
    {
        // 돌기둥이 다 솟기 전에는 때리지 않는다.
        if (elapsed < GetDuration() * 0.25f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.9f);

        for (int i = 0; i < hits.Length; i++)
        {
            Monster monster = hits[i].GetComponent<Monster>();
            if (null == monster || true == hitMonsters.Contains(monster))
            {
                continue;
            }

            hitMonsters.Add(monster);
            monster.TakeDamage(damage);
        }
    }

    private float GetDuration()
    {
        if (0.0f >= animationFps)
        {
            return 0.5f;
        }

        return FrameCount / animationFps;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.9f);
    }
}
