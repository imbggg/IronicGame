using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private enum CharacterClass
    {
        Archer = 0,
        Rogue = 1,
        Mage = 2
    }

    public float moveSpeed = 6.0f;
    public float radius = 0.3f;

    [Header("체력")]
    [SerializeField]
    private int maxHealth = 10;

    [SerializeField]
    [Tooltip("피격 후 무적 시간(초). 몬스터에 겹쳐 있을 때 연속으로 맞는 것을 막는다.")]
    private float invincibleDuration = 0.6f;

    [Header("직업")]
    [SerializeField]
    [Tooltip("-1이면 타이틀에서 고른 직업을 쓴다. 0 궁수 / 1 도적 / 2 마법사.")]
    private int classIndexOverride = -1;

    [Header("원거리 프리팹")]
    [SerializeField]
    [Tooltip("궁수 화살. 궁수 프리팹에만 넣는다.")]
    private GameObject arrowPrefab;

    [SerializeField]
    private Transform arrowSpawnPoint;

    [SerializeField]
    [Tooltip("화살이 생기는 거리. 플레이어 몸에서 이만큼 앞에 나온다. 너무 작으면 붙어 있는 몬스터에게 즉시 맞아 화살이 보이지 않는다.")]
    private float arrowSpawnDistance = 1.0f;

    [Header("도적 X - 지면 융기")]
    [SerializeField] private int groundSpikeDamage = 2;

    [SerializeField]
    [Tooltip("돌기둥이 앞으로 나아가는 속도. 0이면 제자리에서 솟는다.")]
    private float groundSpikeSpeed = 0.0f;

    [SerializeField]
    [Tooltip("도적 앞쪽 어느 거리에서 솟을지.")]
    private float groundSpikeForward = 1.2f;

    [SerializeField]
    [Tooltip("캐릭터 원점에서 발밑까지의 거리. 피벗이 몸통 중앙이라 이만큼 내려야 바닥에 붙는다.")]
    private float groundSpikeFootDrop = 0.5f;

    [SerializeField] private float groundSpikeRange = 5.0f;
    [SerializeField] private float groundSpikeFps = 14.0f;

    [SerializeField]
    [Tooltip("이펙트 스프라이트의 Pixels Per Unit. 캐릭터와 같은 값으로 맞춘다.")]
    private float groundSpikePixelsPerUnit = 108.0f;

    [Header("근접 공격")]
    [SerializeField]
    [Tooltip("근접 판정 원의 중심 거리. 캐릭터에서 바라보는 방향으로 이만큼 떨어진다.")]
    private float meleeOffset = 0.6f;

    [SerializeField]
    [Tooltip("근접 판정 원의 반지름.")]
    private float meleeRadius = 0.7f;

    [SerializeField] private int rogueAttackDamage = 1;
    [SerializeField] private int mageAttackDamage = 1;
    [SerializeField] private int mageSmashDamage = 3;

    [SerializeField]
    [Tooltip("마법사 강타는 판정 범위가 더 넓다.")]
    private float mageSmashRadiusBonus = 0.9f;

    [Header("마법사 X - 타격 이펙트")]
    [SerializeField]
    [Tooltip("이펙트가 터지는 위치. 캐릭터 앞쪽 거리.")]
    private float mageSmashEffectForward = 1.0f;

    [SerializeField] private float mageSmashEffectFps = 18.0f;

    [SerializeField]
    [Tooltip("이펙트 스프라이트의 Pixels Per Unit. 값이 클수록 작아진다.")]
    private float mageSmashPixelsPerUnit = 108.0f;

    // --- 공격 애니메이션 타이밍 ---------------------------------------
    // 클립은 12 FPS 9프레임 = 0.75초. 팔이 완전히 뻗는 6번째 프레임이
    // 클립 시간으로 0.4167초 지점이라, 거기서 공격 판정을 낸다.
    // 속도를 인스펙터에서 조절하면 판정 타이밍도 같이 따라간다.
    private const float ClipLength = 0.75f;
    private const float ReleasePoint = 0.4167f;

    [Header("공격 애니메이션")]
    [SerializeField]
    [Range(0.2f, 2.0f)]
    [Tooltip("작을수록 느리게 재생된다. 판정 타이밍은 자동으로 맞춰진다.")]
    private float attackAnimSpeed = 0.6f;

    [SerializeField]
    [Tooltip("X 특수공격 재사용 대기시간(초).")]
    private float skillCooldown = 3.0f;

    // Animator 창에 보이는 상태 이름을 그대로 적는다.
    // 트리거 대신 이 이름으로 직접 재생하기 때문에,
    // 캐릭터마다 프리팹에서 이름만 바꿔주면 된다.
    [Header("애니메이터 상태 이름")]
    [SerializeField] private string idleStateName = "archer_walk";
    [SerializeField] private string attackStateName = "archer_attack";
    [SerializeField] private string skillStateName = "archer_scatter";

    public int MaxHealth { get { return maxHealth; } }
    public int CurrentHealth { get { return currentHealth; } }
    public bool IsDead { get { return 0 >= currentHealth; } }
    public PlayerInventory Inventory { get; private set; }
    public string LastIronicModifierDescription { get; private set; } = string.Empty;
    public bool LastIronicModifierWasPositive { get; private set; }
    public string IronicStatsSummary
    {
        get
        {
            return $"속도 {moveSpeed:0.0}  |  공격 x{attackPowerMultiplier:0.00}  |  X {skillCooldown:0.0}초";
        }
    }

    /// <summary>
    /// HP 아래에 쿨타임 게이지를 그릴 때 쓴다. 0이면 사용 가능, 1이면 방금 썼다.
    /// </summary>
    public float SkillCooldownRatio
    {
        get
        {
            if (0.0f >= skillCooldown)
            {
                return 0.0f;
            }

            return Mathf.Clamp01(skillCooldownRemaining / skillCooldown);
        }
    }

    private TileMap tileMap;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CharacterClass characterClass;

    private bool isAttacking;
    private bool isUsingSkill;
    private bool isMoving;
    private Vector2 lastMoveDirection = Vector2.right;

    private int currentHealth;
    private float invincibleRemaining;
    private float skillCooldownRemaining;
    private float attackPowerMultiplier = 1.0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Inventory = GetComponent<PlayerInventory>();
        if (null == Inventory)
        {
            Inventory = gameObject.AddComponent<PlayerInventory>();
        }

        currentHealth = maxHealth;

        int index = (0 <= classIndexOverride)
            ? classIndexOverride
            : GameData.selectedCharacter;

        characterClass = (CharacterClass)Mathf.Clamp(index, 0, 2);
    }

    public void Init(TileMap tileMap, Vector2 startPosition)
    {
        this.tileMap = tileMap;
        transform.position = new Vector3(startPosition.x, startPosition.y, 0.0f);
    }

    /// <summary>
    /// 상태를 처음부터 강제로 재생한다.
    /// SetTrigger는 전환에 소비될 때까지 켜진 채 남아 있어서,
    /// 연타하면 엉뚱한 시점에 소비되어 모션이 건너뛰어진다.
    /// Play는 전환 설정과 무관하게 항상 0프레임부터 시작한다.
    /// </summary>
    private void PlayState(string stateName)
    {
        if (null == animator || true == string.IsNullOrEmpty(stateName))
        {
            return;
        }

        animator.Play(stateName, 0, 0.0f);
    }

    private void SetMoving(bool moving)
    {
        if (null == animator)
        {
            return;
        }

        // 공격/스킬 중에는 애니메이터를 건드리지 않는다.
        // 이동 중에 공격하면 여기서 speed를 1로 덮어써서
        // 공격 애니메이션이 갑자기 빨라진다.
        if (true == isAttacking || true == isUsingSkill)
        {
            return;
        }

        // 상태가 바뀔 때만 처리한다.
        // 매 프레임 되감으면 애니메이션이 계속 리셋된다.
        if (moving == isMoving)
        {
            return;
        }

        isMoving = moving;

        if (true == moving)
        {
            animator.speed = 1.0f;
            return;
        }

        // 멈출 때는 속도만 0으로 두지 않고 첫 프레임으로 되감는다.
        // 그러지 않으면 걷던 중간 포즈 그대로 굳어버린다.
        animator.speed = 0.0f;
        animator.Play(0, 0, 0.0f);
    }

    private void Update()
    {
        invincibleRemaining = Mathf.Max(0.0f, invincibleRemaining - Time.deltaTime);
        skillCooldownRemaining = Mathf.Max(0.0f, skillCooldownRemaining - Time.deltaTime);

        if (true == IsDead)
        {
            return;
        }

        if (null == tileMap)
        {
            return;
        }

        bool busy = (true == isAttacking || true == isUsingSkill);

        if (true == Input.GetKeyDown(KeyCode.Z) && false == busy)
        {
            StartCoroutine(Attack());
        }

        if (true == Input.GetKeyDown(KeyCode.X)
            && false == busy
            && 0.0f >= skillCooldownRemaining)
        {
            skillCooldownRemaining = skillCooldown;
            StartCoroutine(Skill());
        }

        float horizontal = 0.0f;
        float vertical = 0.0f;

        if (true == Input.GetKey(KeyCode.LeftArrow) || true == Input.GetKey(KeyCode.A))
        {
            horizontal -= 1.0f;
        }

        if (true == Input.GetKey(KeyCode.RightArrow) || true == Input.GetKey(KeyCode.D))
        {
            horizontal += 1.0f;
        }

        if (true == Input.GetKey(KeyCode.DownArrow) || true == Input.GetKey(KeyCode.S))
        {
            vertical -= 1.0f;
        }

        if (true == Input.GetKey(KeyCode.UpArrow) || true == Input.GetKey(KeyCode.W))
        {
            vertical += 1.0f;
        }

        Vector2 direction = new Vector2(horizontal, vertical);
        if (0.0f == direction.sqrMagnitude)
        {
            // 공격/스킬 중에는 애니메이터를 건드리지 않는다.
            // 여기서 되감으면 해당 애니메이션이 매 프레임 리셋되어 깜빡인다.
            if (false == busy)
            {
                SetMoving(false);
            }

            return;
        }

        lastMoveDirection = direction.normalized;
        SetMoving(true);

        if (null != spriteRenderer && 0.0f != horizontal)
        {
            spriteRenderer.flipX = (horizontal < 0.0f);
        }

        direction = direction.normalized;

        float distance = moveSpeed * Time.deltaTime;
        Vector2 position = transform.position;

        Vector2 movedX = new Vector2(position.x + direction.x * distance, position.y);
        if (true == CanMove(movedX))
        {
            position = movedX;
        }

        Vector2 movedY = new Vector2(position.x, position.y + direction.y * distance);
        if (true == CanMove(movedY))
        {
            position = movedY;
        }

        transform.position = new Vector3(position.x, position.y, 0.0f);

        Camera camera = Camera.main;
        if (null != camera)
        {
            camera.transform.position = new Vector3(transform.position.x, transform.position.y, -10.0f);
        }
    }

    // --- 공격 ---------------------------------------------------------

    /// <summary>
    /// Z. 궁수는 화살, 도적과 마법사는 근접.
    /// </summary>
    private IEnumerator Attack()
    {
        isAttacking = true;

        isMoving = true;
        animator.speed = attackAnimSpeed;
        PlayState(attackStateName);

        yield return new WaitForSeconds(ReleasePoint / attackAnimSpeed);

        switch (characterClass)
        {
            case CharacterClass.Archer:
                ThrowArrow();
                break;
            case CharacterClass.Rogue:
                MeleeHit(rogueAttackDamage, meleeRadius);
                break;
            case CharacterClass.Mage:
                MeleeHit(mageAttackDamage, meleeRadius);
                break;
        }

        RetroAudio.Play(RetroSound.PlayerAttack);

        yield return new WaitForSeconds(
            (ClipLength - ReleasePoint) / attackAnimSpeed);

        animator.speed = 1.0f;
        PlayState(idleStateName);
        isAttacking = false;
    }

    /// <summary>
    /// X. 궁수는 산탄, 도적은 지면 융기, 마법사는 강타.
    /// </summary>
    private IEnumerator Skill()
    {
        isUsingSkill = true;

        // 스킬 중에는 애니메이터가 멈춰 있으면 안 된다.
        isMoving = true;
        animator.speed = attackAnimSpeed;
        PlayState(skillStateName);

        yield return new WaitForSeconds(ReleasePoint / attackAnimSpeed);

        switch (characterClass)
        {
            case CharacterClass.Archer:
                FireArrowSpread();
                break;
            case CharacterClass.Rogue:
                FireGroundSpike();
                break;
            case CharacterClass.Mage:
                MeleeHit(mageSmashDamage, meleeRadius + mageSmashRadiusBonus);
                SpawnMageSmashEffect();
                break;
        }

        RetroAudio.Play(RetroSound.PlayerAttack);

        yield return new WaitForSeconds(
            (ClipLength - ReleasePoint) / attackAnimSpeed);

        animator.speed = 1.0f;
        PlayState(idleStateName);
        isUsingSkill = false;
    }

    /// <summary>
    /// 바라보는 방향 앞쪽에 원을 만들어 그 안의 몬스터를 전부 때린다.
    /// 몬스터 콜라이더는 트리거라서 OverlapCircle로 잡힌다.
    /// </summary>
    private void MeleeHit(int damage, float hitRadius)
    {
        Vector2 facing = lastMoveDirection.normalized;
        if (0.0f == facing.sqrMagnitude)
        {
            facing = Vector2.right;
        }

        Vector2 center = (Vector2)transform.position + facing * meleeOffset;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius);

        for (int i = 0; i < hits.Length; ++i)
        {
            Monster monster = hits[i].GetComponent<Monster>();
            if (null == monster)
            {
                continue;
            }

            monster.TakeDamage(GetModifiedDamage(damage));
        }
    }

    private void SpawnMageSmashEffect()
    {
        Vector2 facing = lastMoveDirection.normalized;
        if (0.0f == facing.sqrMagnitude)
        {
            facing = Vector2.right;
        }

        Vector3 spawnPosition =
            transform.position
            + (Vector3)(facing * mageSmashEffectForward);

        GameObject effectObject = new GameObject("MageSmashEffect");
        effectObject.transform.position = spawnPosition;

        MageSmashEffect effect = effectObject.AddComponent<MageSmashEffect>();
        effect.Init(facing, mageSmashEffectFps, mageSmashPixelsPerUnit);
    }

    private void FireGroundSpike()
    {
        Vector2 direction = lastMoveDirection.normalized;
        if (0.0f == direction.sqrMagnitude)
        {
            direction = Vector2.right;
        }

        // 피벗이 몸통 중앙이라 그대로 쓰면 돌기둥이 공중에 뜬다.
        // 앞으로 밀고 아래로 내려서 발밑 바닥에 맞춘다.
        Vector3 spawnPosition =
            transform.position
            + (Vector3)(direction * groundSpikeForward)
            + Vector3.down * groundSpikeFootDrop;

        // 프리팹 없이 코드로 만든다. 스프라이트는 Resources에서 불러온다.
        GameObject spike = new GameObject("GroundSpike");
        spike.transform.position = spawnPosition;

        GroundSpikeEffect effect = spike.AddComponent<GroundSpikeEffect>();
        effect.Init(
            direction,
            GetModifiedDamage(groundSpikeDamage),
            groundSpikeSpeed,
            groundSpikeRange,
            groundSpikeFps,
            groundSpikePixelsPerUnit
        );
    }

    private void FireArrowSpread()
    {
        if (null == arrowPrefab || null == arrowSpawnPoint)
        {
            return;
        }

        Vector2 baseDirection = lastMoveDirection.normalized;

        float spawnDistance = arrowSpawnDistance;

        Vector3 spawnPosition =
            transform.position
            + (Vector3)(baseDirection * spawnDistance);

        for (int i = -2; i <= 2; ++i)
        {
            float angle = i * 10.0f;

            Vector2 direction =
                Quaternion.Euler(0.0f, 0.0f, angle)
                * baseDirection;

            GameObject arrow = Instantiate(
                arrowPrefab,
                spawnPosition,
                Quaternion.identity
            );

            ArrowProjectile projectile =
                arrow.GetComponent<ArrowProjectile>();

            if (null != projectile)
            {
                projectile.Initialize(direction, attackPowerMultiplier);
            }
        }
    }

    public void ThrowArrow()
    {
        if (null == arrowPrefab || null == arrowSpawnPoint)
        {
            return;
        }

        Vector2 direction = lastMoveDirection.normalized;

        float spawnDistance = arrowSpawnDistance;

        Vector3 spawnPosition =
            transform.position
            + (Vector3)(direction * spawnDistance);

        GameObject arrow = Instantiate(
            arrowPrefab,
            spawnPosition,
            Quaternion.identity
        );

        ArrowProjectile projectile =
            arrow.GetComponent<ArrowProjectile>();

        if (null != projectile)
        {
            projectile.Initialize(direction, attackPowerMultiplier);
        }
    }

    // --- 피격과 사망 ---------------------------------------------------

    /// <summary>
    /// 몬스터가 호출한다. 무적 시간 중이거나 이미 죽었으면 무시한다.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (0 >= damage || true == IsDead || 0.0f < invincibleRemaining)
        {
            return;
        }

        currentHealth -= damage;
        invincibleRemaining = invincibleDuration;
        RetroAudio.Play(RetroSound.PlayerHit);

        if (0 >= currentHealth)
        {
            currentHealth = 0;
            Die();
            return;
        }

        StartCoroutine(FlashOnHit());
    }

    public void Heal(int amount)
    {
        if (0 >= amount || true == IsDead)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public void ApplyIronicModifier(IronicStatType statType, bool increase)
    {
        LastIronicModifierDescription = IronicRewardUI.GetDescription(statType, increase);
        LastIronicModifierWasPositive = increase;

        switch (statType)
        {
            case IronicStatType.MaxHealth:
            {
                int previousMaxHealth = maxHealth;
                maxHealth = Mathf.Clamp(maxHealth + (true == increase ? 2 : -2), 3, 30);

                if (maxHealth > previousMaxHealth)
                {
                    currentHealth += maxHealth - previousMaxHealth;
                }
                else
                {
                    currentHealth = Mathf.Min(currentHealth, maxHealth);
                }
                break;
            }
            case IronicStatType.MoveSpeed:
                moveSpeed *= true == increase ? 1.20f : 0.85f;
                moveSpeed = Mathf.Clamp(moveSpeed, 2.5f, 12.0f);
                break;
            case IronicStatType.AttackPower:
                attackPowerMultiplier *= true == increase ? 1.50f : 0.75f;
                attackPowerMultiplier = Mathf.Clamp(attackPowerMultiplier, 0.50f, 4.0f);
                break;
            case IronicStatType.SkillCooldown:
                skillCooldown *= true == increase ? 0.75f : 1.30f;
                skillCooldown = Mathf.Clamp(skillCooldown, 0.75f, 8.0f);
                skillCooldownRemaining = Mathf.Min(skillCooldownRemaining, skillCooldown);
                break;
        }
    }

    private int GetModifiedDamage(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * attackPowerMultiplier));
    }

    private void Die()
    {
        StopAllCoroutines();

        isAttacking = false;
        isUsingSkill = false;

        if (null != animator)
        {
            animator.speed = 0.0f;
        }

        if (null != spriteRenderer)
        {
            spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }

        // 패배 화면은 GameEndUI가 담당한다. 여기서는 호출만 한다.
        GameEndUI.ShowGameOver();
    }

    private IEnumerator FlashOnHit()
    {
        if (null == spriteRenderer)
        {
            yield break;
        }

        Color original = spriteRenderer.color;

        for (int i = 0; i < 3; ++i)
        {
            spriteRenderer.color = new Color(1.0f, 0.4f, 0.4f, 1.0f);
            yield return new WaitForSeconds(0.06f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(0.06f);
        }

        spriteRenderer.color = original;
    }

    // --- 이동 판정 -----------------------------------------------------

    private bool CanMove(Vector2 position)
    {
        if (false == IsFloor(position.x - radius, position.y - radius))
        {
            return false;
        }

        if (false == IsFloor(position.x + radius, position.y - radius))
        {
            return false;
        }

        if (false == IsFloor(position.x - radius, position.y + radius))
        {
            return false;
        }

        if (false == IsFloor(position.x + radius, position.y + radius))
        {
            return false;
        }

        return true;
    }

    private bool IsFloor(float worldX, float worldY)
    {
        int x = Mathf.FloorToInt(worldX);
        int y = Mathf.FloorToInt(worldY);

        Tile tile = tileMap.GetTile(x, y);
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

        if (true == PropBlock.IsBlocked(tile.index))
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 facing = lastMoveDirection.normalized;
        if (0.0f == facing.sqrMagnitude)
        {
            facing = Vector2.right;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            (Vector2)transform.position + facing * meleeOffset,
            meleeRadius
        );

        // 이동 충돌 판정 사각형.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(radius * 2.0f, radius * 2.0f, 0.0f)
        );
    }
}
