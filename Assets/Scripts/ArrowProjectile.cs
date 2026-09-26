using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField]
    private float speed = 12.0f;

    [SerializeField]
    private float range = 8.0f;

    [SerializeField]
    [Tooltip("몬스터에게 주는 피해량.")]
    private int damage = 1;

    [SerializeField]
    [Tooltip("체크하면 한 번 맞춰도 사라지지 않고 계속 날아간다. 도적의 지면 융기처럼 관통형에 쓴다.")]
    private bool piercing = false;

    /// <summary>한 프레임에 한 칸을 건너뛰지 않도록, 이 간격마다 경로를 찍어본다.</summary>
    private const float PathStep = 0.2f;

    [SerializeField]
    [Tooltip("화살 굵기. 플레이어처럼 네 모서리를 검사해서, 소품 가장자리를 스치고 지나가지 않게 한다.")]
    private float hitRadius = 0.2f;

    private Vector2 direction;
    private Vector2 startPosition;
    private int runtimeDamage;
    private TileMap tileMap;

    // Destroy는 프레임 끝에 처리된다. 그 사이에 콜라이더가 살아 있어서
    // 막혀서 없어질 화살이 한 번 더 때리는 일이 생긴다.
    private bool blocked;

    // 관통형이 아닌 화살이 한 번 맞춘 뒤 더 훑지 않도록 한다.
    private bool spent;

    // 관통형은 같은 대상을 프레임마다 다시 때릴 수 있어서
    // 이미 때린 대상을 기억해 둔다. 몬스터와 보스를 함께 담는다.
    private readonly System.Collections.Generic.HashSet<IDamageable> hitTargets =
        new System.Collections.Generic.HashSet<IDamageable>();

    /// <summary>
    /// map을 넘기면 벽, 닫힌 문, 소품에 막힌다. 넘기지 않으면 사거리로만 사라진다.
    /// shooterPosition은 쏜 사람의 위치다. 화살은 몸에서 조금 앞에 생기기 때문에,
    /// 그 사이에 문이 한 칸 끼어 있으면 검사 없이 건너뛰게 된다. 그 구간까지 본다.
    /// </summary>
    public void Initialize(
        Vector2 fireDirection,
        float damageMultiplier = 1.0f,
        TileMap map = null,
        Vector2? shooterPosition = null)
    {
        direction = fireDirection.normalized;
        startPosition = transform.position;
        runtimeDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));
        tileMap = map;

        if (false == shooterPosition.HasValue)
        {
            return;
        }

        if (false == IsPathClear(shooterPosition.Value, transform.position))
        {
            Block();
            return;
        }

        // 화살은 몸에서 조금 앞에 생긴다. 그 사이에 있는 대상은
        // 화살이 지나간 적이 없어서 영영 안 맞는다. 생성 구간을 훑어서 때린다.
        SweepSpawnGap(shooterPosition.Value, transform.position);

        float angle = Mathf.Atan2(direction.y, direction.x)
            * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(
            0.0f,
            0.0f,
            angle
        );
    }

    private void Update()
    {
        Vector2 previous = transform.position;

        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);

        float traveledDistance =
            Vector2.Distance(startPosition, transform.position);

        // 끝점만 보면 빠른 화살이 한 칸짜리 문을 건너뛴다. 지나온 구간을 전부 본다.
        if (traveledDistance >= range
            || false == IsPathClear(previous, transform.position))
        {
            Block();
        }
    }

    /// <summary>더 이상 피해를 주지 않고 사라진다.</summary>
    private void Block()
    {
        blocked = true;

        Collider2D arrowCollider = GetComponent<Collider2D>();
        if (null != arrowCollider)
        {
            arrowCollider.enabled = false;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 쏜 위치에서 화살이 생긴 위치까지를 훑어 대상을 때린다.
    /// 몬스터에 딱 붙어서 쐈을 때 화살이 상대를 건너뛰는 것을 막는다.
    /// </summary>
    private void SweepSpawnGap(Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / PathStep));

        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, (float)i / steps);
            Collider2D[] hits = Physics2D.OverlapCircleAll(point, hitRadius);

            for (int h = 0; h < hits.Length; h++)
            {
                HandleHit(hits[h]);

                if (true == spent)
                {
                    return;
                }
            }
        }
    }

    /// <summary>두 점 사이를 일정 간격으로 찍어보며 막히는 칸이 있는지 확인한다.</summary>
    private bool IsPathClear(Vector2 from, Vector2 to)
    {
        if (null == tileMap)
        {
            return true;
        }

        float distance = Vector2.Distance(from, to);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / PathStep));

        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, (float)i / steps);
            if (false == IsFloor(point))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 플레이어와 같은 방식으로 네 모서리를 본다.
    /// 중심 한 점만 보면 한 칸짜리 소품이나 문을 비스듬히 스쳐 지나간다.
    /// </summary>
    private bool IsFloor(Vector2 position)
    {
        if (null == tileMap)
        {
            return true;
        }

        return IsOpenAt(position.x - hitRadius, position.y - hitRadius)
            && IsOpenAt(position.x + hitRadius, position.y - hitRadius)
            && IsOpenAt(position.x - hitRadius, position.y + hitRadius)
            && IsOpenAt(position.x + hitRadius, position.y + hitRadius);
    }

    /// <summary>한 점이 화살이 지나갈 수 있는 칸인지.</summary>
    private bool IsOpenAt(float worldX, float worldY)
    {
        Tile tile = tileMap.GetTile(Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldY));

        if (null == tile)
        {
            return false;
        }

        if (Tile.Type.Floor != tile.type)
        {
            return false;
        }

        if (null != tile.door && Door.State.Open != tile.door.state)
        {
            return false;
        }

        return false == PropBlock.IsBlocked(tile.index);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    // 몬스터가 Update에서 직접 좌표를 옮기기 때문에
    // 빠르게 스쳐 지나갈 때 Enter가 안 잡히는 경우가 있다.
    private void OnTriggerStay2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider2D other)
    {
        if (true == blocked || true == spent)
        {
            return;
        }

        IDamageable target = other.GetComponent<IDamageable>();
        if (null == target)
        {
            return;
        }

        if (true == hitTargets.Contains(target))
        {
            return;
        }

        hitTargets.Add(target);
        target.TakeDamage(runtimeDamage);

        if (false == piercing)
        {
            spent = true;
            Destroy(gameObject);
        }
    }
}
