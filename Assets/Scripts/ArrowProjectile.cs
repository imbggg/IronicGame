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

    private Vector2 direction;
    private Vector2 startPosition;
    private int runtimeDamage;

    // 관통형은 같은 몬스터를 프레임마다 다시 때릴 수 있어서
    // 이미 때린 대상을 기억해 둔다.
    private readonly System.Collections.Generic.HashSet<Monster> hitMonsters =
        new System.Collections.Generic.HashSet<Monster>();

    public void Initialize(Vector2 fireDirection, float damageMultiplier = 1.0f)
    {
        direction = fireDirection.normalized;
        startPosition = transform.position;
        runtimeDamage = Mathf.Max(1, Mathf.RoundToInt(damage * damageMultiplier));

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
        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);

        float traveledDistance =
            Vector2.Distance(startPosition, transform.position);

        if (traveledDistance >= range)
        {
            Destroy(gameObject);
        }
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
        Monster monster = other.GetComponent<Monster>();
        if (null == monster)
        {
            return;
        }

        if (true == hitMonsters.Contains(monster))
        {
            return;
        }

        hitMonsters.Add(monster);
        monster.TakeDamage(runtimeDamage);

        if (false == piercing)
        {
            Destroy(gameObject);
        }
    }
}
