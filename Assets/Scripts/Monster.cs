using UnityEngine;

public class Monster : MonoBehaviour, IDamageable
{
    private enum AnimationState
    {
        Idle,
        Walk,
        Attack,
        Special,
        Hit,
        Death
    }

    private struct MonsterConfig
    {
        public int frameSize;
        public int idleCount;
        public int walkCount;
        public int attackCount;
        public int specialCount;
        public int hitCount;
        public int deathCount;
        public int health;
        public int attackDamage;
        public int specialAttackDamage;
        public float speed;
        public float detectionRange;
        public float attackRange;
        public float attackCooldown;
        public float animationFps;

        // 몬스터마다 셀 크기가 달라서 화면상 크기를 맞추려면 따로 잡아야 한다.
        public float pixelsPerUnit;

        // 원거리 몬스터용
        public bool isRanged;
        public int projectileCount;     // 탄 스프라이트 프레임 수
        public int launchFrame;         // 공격 애니메이션 중 탄이 나가는 프레임
        public float projectileSpeed;
        public float keepDistance;      // 이보다 가까우면 물러선다
    }

    /// <summary>탄이 몬스터 몸 밖에서 시작하도록 앞으로 밀어내는 거리.</summary>
    private const float ProjectileSpawnOffset = 0.45f;

    [Header("체력바")]
    [SerializeField]
    [Tooltip("몬스터 머리 위 어느 높이에 띄울지. 셀 크기에 비례해서 정해진다.")]
    private float healthBarHeightRatio = 0.32f;

    [SerializeField] private float healthBarWidth = 0.75f;
    [SerializeField] private float healthBarThickness = 0.10f;

    [SerializeField] private float radius = 0.28f;
    [SerializeField, Range(0.0f, 1.0f)] private float specialAttackChance = 0.3f;

    private TileMap tileMap;
    private Player player;
    private SpriteRenderer spriteRenderer;
    private MonsterConfig config;
    private int monsterType;

    private Sprite[] idleFrames;
    private Sprite[] walkFrames;
    private Sprite[] attackFrames;
    private Sprite[] specialFrames;
    private Sprite[] hitFrames;
    private Sprite[] deathFrames;

    private AnimationState state;
    private float stateElapsed;
    private float cooldownRemaining;
    private int health;
    private bool attackDamageApplied;
    private bool lootChestSpawned;
    private int guaranteedBossFragmentCount;
    private Sprite[] projectileFrames;
    private bool projectileLaunched;

    private static Sprite barSprite;
    private Transform healthBarRoot;
    private Transform healthBarFill;

    public int RoomIndex { get; private set; } = -1;
    public bool IsDefeated { get { return AnimationState.Death == state; } }

    public void Init(
        TileMap map,
        Vector2 startPosition,
        Player target,
        int typeNumber,
        int spawnRoomIndex = -1)
    {
        tileMap = map;
        player = target;
        RoomIndex = spawnRoomIndex;
        monsterType = Mathf.Clamp(typeNumber, 1, 5);
        config = GetConfig(monsterType);
        config.attackCooldown *= GameData.GetMonsterAttackCooldownMultiplier();
        health = config.health;
        transform.position = new Vector3(startPosition.x, startPosition.y, 0.0f);
        gameObject.name = $"Monster{monsterType}";

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (null == spriteRenderer)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sortingOrder = 19;
        LoadAnimations();
        CreateHealthBar();
        SetState(AnimationState.Idle);
        ApplyFrame(true);
    }

    // --- 체력바 -------------------------------------------------------

    /// <summary>
    /// 프리팹 없이 코드로 만든다. 1픽셀 흰색 스프라이트를 늘려서 막대로 쓴다.
    /// 자식으로 달기 때문에 몬스터의 flipX 영향을 받지 않는다.
    /// </summary>
    private void CreateHealthBar()
    {
        if (null == barSprite)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.filterMode = FilterMode.Point;
            texture.Apply();

            barSprite = Sprite.Create(
                texture, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f), 1.0f);
        }

        // 셀 크기가 몬스터마다 달라서, 높이도 거기에 맞춰 띄운다.
        float cellHeight = config.frameSize / config.pixelsPerUnit;
        float barY = cellHeight * healthBarHeightRatio;

        GameObject root = new GameObject("HealthBar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0.0f, barY, 0.0f);
        healthBarRoot = root.transform;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(root.transform, false);
        background.transform.localScale =
            new Vector3(healthBarWidth, healthBarThickness, 1.0f);

        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = barSprite;
        backgroundRenderer.color = new Color(0.10f, 0.08f, 0.12f, 0.85f);
        backgroundRenderer.sortingOrder = 20;

        // 채움은 왼쪽 끝을 기준으로 줄어들어야 해서, 피벗 대신 위치를 왼쪽으로 옮긴다.
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(root.transform, false);
        healthBarFill = fill.transform;

        SpriteRenderer fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = barSprite;
        fillRenderer.sortingOrder = 21;

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (null == healthBarFill)
        {
            return;
        }

        float ratio = Mathf.Clamp01((float)health / Mathf.Max(1, config.health));

        float filledWidth = healthBarWidth * ratio;
        healthBarFill.localScale =
            new Vector3(filledWidth, healthBarThickness, 1.0f);

        // 줄어든 만큼 왼쪽으로 붙인다.
        healthBarFill.localPosition =
            new Vector3(-(healthBarWidth - filledWidth) * 0.5f, 0.0f, 0.0f);

        SpriteRenderer fillRenderer = healthBarFill.GetComponent<SpriteRenderer>();
        if (null != fillRenderer)
        {
            fillRenderer.color = Color.Lerp(
                new Color(0.85f, 0.15f, 0.15f, 1.0f),
                new Color(0.35f, 0.80f, 0.25f, 1.0f),
                ratio);
        }
    }

    private void Update()
    {
        if (AnimationState.Death == state)
        {
            UpdateLockedAnimation(false);
            if (stateElapsed >= GetDuration(deathFrames) + 0.15f)
            {
                if (false == lootChestSpawned)
                {
                    lootChestSpawned = true;
                    LootChest.Spawn(transform.position, guaranteedBossFragmentCount);
                }

                Destroy(gameObject);
            }
            return;
        }

        if (AnimationState.Hit == state)
        {
            if (UpdateLockedAnimation(false))
            {
                SetState(AnimationState.Idle);
            }
            return;
        }

        if (AnimationState.Attack == state || AnimationState.Special == state)
        {
            if (true == config.isRanged)
            {
                TryLaunchProjectile();
            }
            else
            {
                TryDamagePlayer();
            }

            if (UpdateLockedAnimation(false))
            {
                SetState(AnimationState.Idle);
            }
            return;
        }

        if (null == player)
        {
            player = FindAnyObjectByType<Player>();
        }

        cooldownRemaining = Mathf.Max(0.0f, cooldownRemaining - Time.deltaTime);

        if (null == player)
        {
            SetState(AnimationState.Idle);
            UpdateLoopAnimation();
            return;
        }

        Vector2 direction = (Vector2)player.transform.position - (Vector2)transform.position;
        float distance = direction.magnitude;

        if (true == config.isRanged)
        {
            UpdateRanged(direction, distance);
            return;
        }

        if (distance <= config.attackRange && 0.0f >= cooldownRemaining)
        {
            cooldownRemaining = config.attackCooldown;
            bool useSpecial = 0 < specialFrames.Length && Random.value < specialAttackChance;
            SetState(useSpecial ? AnimationState.Special : AnimationState.Attack);
            ApplyFrame(false);
            return;
        }

        if (distance <= config.detectionRange && 0.001f < distance)
        {
            direction.Normalize();
            Move(direction);
            SetState(AnimationState.Walk);

            if (0.0f != direction.x)
            {
                spriteRenderer.flipX = direction.x < 0.0f;
            }
        }
        else
        {
            SetState(AnimationState.Idle);
        }

        UpdateLoopAnimation();
    }

    public void TakeDamage(int damage)
    {
        if (AnimationState.Death == state || 0 >= damage)
        {
            return;
        }

        health -= damage;
        UpdateHealthBar();

        if (0 >= health)
        {
            SetState(AnimationState.Death);
            RetroAudio.Play(RetroSound.MonsterDefeated);

            if (null != healthBarRoot)
            {
                healthBarRoot.gameObject.SetActive(false);
            }

            Collider2D monsterCollider = GetComponent<Collider2D>();
            if (null != monsterCollider)
            {
                monsterCollider.enabled = false;
            }
            return;
        }

        RetroAudio.Play(RetroSound.MonsterHit);
        SetState(AnimationState.Hit);
    }

    /// <summary>
    /// 원거리 몬스터는 사거리 안에서 탄을 쏘고, 너무 붙으면 물러선다.
    /// </summary>
    private void UpdateRanged(Vector2 toPlayer, float distance)
    {
        if (distance > config.detectionRange)
        {
            SetState(AnimationState.Idle);
            UpdateLoopAnimation();
            return;
        }

        if (0.001f < Mathf.Abs(toPlayer.x))
        {
            spriteRenderer.flipX = toPlayer.x < 0.0f;
        }

        if (distance <= config.attackRange && 0.0f >= cooldownRemaining)
        {
            cooldownRemaining = config.attackCooldown;
            SetState(AnimationState.Attack);
            ApplyFrame(false);
            return;
        }

        Vector2 move = Vector2.zero;

        if (distance > config.attackRange)
        {
            move = toPlayer.normalized;
        }
        else if (distance < config.keepDistance)
        {
            // 너무 붙었으면 거리를 벌린다.
            move = -toPlayer.normalized;
        }

        if (Vector2.zero != move)
        {
            Move(move);
            SetState(AnimationState.Walk);
        }
        else
        {
            SetState(AnimationState.Idle);
        }

        UpdateLoopAnimation();
    }

    /// <summary>공격 애니메이션이 발사 프레임에 닿으면 탄을 하나 만든다.</summary>
    private void TryLaunchProjectile()
    {
        if (true == projectileLaunched || null == player)
        {
            return;
        }

        int frame = Mathf.FloorToInt(stateElapsed * config.animationFps);
        if (frame < config.launchFrame)
        {
            return;
        }

        projectileLaunched = true;

        Vector2 direction = (Vector2)player.transform.position - (Vector2)transform.position;
        if (0.001f > direction.magnitude)
        {
            direction = Vector2.right;
        }

        direction.Normalize();

        // 몬스터 중심에서 그대로 내보내면 탄이 몸 안에서 튀어나온다.
        Vector3 origin = transform.position + (Vector3)(direction * ProjectileSpawnOffset);

        MonsterProjectile.Spawn(
            tileMap,
            origin,
            direction,
            GameData.GetMonsterAttackDamage(config.attackDamage),
            config.projectileSpeed,
            player,
            projectileFrames,
            config.animationFps);
    }

    public void AddGuaranteedBossFragment()
    {
        guaranteedBossFragmentCount++;
    }

    private void Move(Vector2 direction)
    {
        float distance = config.speed * Time.deltaTime;
        Vector2 position = transform.position;

        Vector2 movedX = new Vector2(position.x + direction.x * distance, position.y);
        if (CanMove(movedX))
        {
            position = movedX;
        }

        Vector2 movedY = new Vector2(position.x, position.y + direction.y * distance);
        if (CanMove(movedY))
        {
            position = movedY;
        }

        transform.position = new Vector3(position.x, position.y, 0.0f);
    }

    private void TryDamagePlayer()
    {
        if (true == attackDamageApplied || null == player)
        {
            return;
        }

        Sprite[] frames = GetCurrentFrames();
        if (stateElapsed < GetDuration(frames) * 0.45f)
        {
            return;
        }

        float hitRange = config.attackRange;
        if (AnimationState.Special == state)
        {
            hitRange += 0.35f;
        }

        attackDamageApplied = true;
        if (Vector2.Distance(transform.position, player.transform.position) <= hitRange)
        {
            int damage = AnimationState.Special == state
                ? config.specialAttackDamage
                : config.attackDamage;
            player.TakeDamage(GameData.GetMonsterAttackDamage(damage));
        }
    }

    private bool CanMove(Vector2 position)
    {
        return
            IsFloor(position.x - radius, position.y - radius)
            && IsFloor(position.x + radius, position.y - radius)
            && IsFloor(position.x - radius, position.y + radius)
            && IsFloor(position.x + radius, position.y + radius);
    }

    private bool IsFloor(float worldX, float worldY)
    {
        if (null == tileMap)
        {
            return false;
        }

        Tile tile = tileMap.GetTile(Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldY));
        if (null == tile || Tile.Type.Floor != tile.type)
        {
            return false;
        }

        if (null != tile.door && Door.State.Open != tile.door.state)
        {
            return false;
        }

        return false == PropBlock.IsBlocked(tile.index);
    }

    private void LoadAnimations()
    {
        string root = $"Monsters/Monster{monsterType}/";
        idleFrames = LoadFrames(root + "Idle", config.idleCount);
        walkFrames = LoadFrames(root + "Walk", config.walkCount);
        attackFrames = LoadFrames(root + "Attack", config.attackCount);
        specialFrames = LoadFrames(root + "Special", config.specialCount);
        hitFrames = LoadFrames(root + "Hit", config.hitCount);
        deathFrames = LoadFrames(root + "Death", config.deathCount);
        projectileFrames = LoadFrames(root + "Projectile", config.projectileCount);
    }

    private Sprite[] LoadFrames(string resourcePath, int expectedCount)
    {
        // 해당 클립이 없는 몬스터도 있다. 그때는 에러를 내지 않는다.
        if (0 >= expectedCount)
        {
            return new Sprite[0];
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (null == texture)
        {
            Debug.LogError($"[Monster] Cannot load texture: Resources/{resourcePath}.png");
            return new Sprite[0];
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int count = Mathf.Min(expectedCount, texture.width / config.frameSize);
        Sprite[] frames = new Sprite[count];

        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                texture,
                new Rect(i * config.frameSize, 0, config.frameSize, config.frameSize),
                new Vector2(0.5f, 0.5f),
                config.pixelsPerUnit
            );
            frames[i].name = $"{texture.name}_{i}";
        }

        return frames;
    }

    private MonsterConfig GetConfig(int typeNumber)
    {
        MonsterConfig result = new MonsterConfig();
        result.pixelsPerUnit = 32.0f;

        switch (typeNumber)
        {
            case 2:
                result.frameSize = 100;
                result.idleCount = 6;
                result.walkCount = 8;
                result.attackCount = 7;
                result.specialCount = 7;
                result.hitCount = 4;
                result.deathCount = 4;
                result.health = 4;
                result.attackDamage = 1;
                result.specialAttackDamage = 2;
                result.speed = 2.1f;
                result.detectionRange = 7.5f;
                result.attackRange = 1.0f;
                result.attackCooldown = 1.0f;
                result.animationFps = 10.0f;
                break;
            case 3:
                result.frameSize = 100;
                result.idleCount = 6;
                result.walkCount = 8;
                result.attackCount = 6;
                result.specialCount = 6;
                result.hitCount = 4;
                result.deathCount = 4;
                result.health = 5;
                result.attackDamage = 2;
                result.specialAttackDamage = 2;
                result.speed = 1.55f;
                result.detectionRange = 7.0f;
                result.attackRange = 1.05f;
                result.attackCooldown = 1.2f;
                result.animationFps = 10.0f;
                break;
            case 5:
                result.frameSize = 64;
                result.pixelsPerUnit = 32.0f; //화면에 보이는 크기
                result.idleCount = 4;   //애니메이션 프레임 수
                result.walkCount = 8;   //애니메이션 프레임 수
                result.attackCount = 11;//애니메이션 프레임 수
                result.hitCount = 4;    //애니메이션 프레임 수
                result.deathCount = 12;//애니메이션 프레임 수
                result.health = 4;      //체력
                result.attackDamage = 2;//공격력
                result.speed = 1.2f; //이동속도
                result.detectionRange = 9.5f; //플레이어 감지 범위
                result.attackRange = 6.0f;    //공격 범위
                result.attackCooldown = 2.4f;//공격 쿨타임
                result.animationFps = 12.0f; //애니메이션 재생 속도
                result.isRanged = true; //원거리 공격 몬스터
                result.projectileCount = 3; //탄 스프라이트 프레임 수
                result.launchFrame = 6;  //공격 애니메이션 중 탄이 나가는 프레임
                result.projectileSpeed = 5.5f; //탄 이동속도
                result.keepDistance = 3.0f; //이보다 가까우면 물러선다
                break;
            case 4:
                result.frameSize = 96;
                result.idleCount = 6;
                result.walkCount = 8;
                result.attackCount = 8;
                result.specialCount = 8;
                result.hitCount = 4;
                result.deathCount = 10;
                result.health = 6;
                result.attackDamage = 1;
                result.specialAttackDamage = 2;
                result.speed = 1.35f;
                result.detectionRange = 6.5f;
                result.attackRange = 1.1f;
                result.attackCooldown = 1.35f;
                result.animationFps = 9.0f;
                break;
            default:
                result.frameSize = 100;
                result.idleCount = 6;
                result.walkCount = 8;
                result.attackCount = 8;
                result.specialCount = 8;
                result.hitCount = 4;
                result.deathCount = 4;
                result.health = 3;
                result.attackDamage = 1;
                result.specialAttackDamage = 2;
                result.speed = 1.8f;
                result.detectionRange = 7.0f;
                result.attackRange = 0.85f;
                result.attackCooldown = 1.1f;
                result.animationFps = 10.0f;
                break;
        }

        return result;
    }

    private void SetState(AnimationState nextState)
    {
        if (state == nextState && 0.0f < stateElapsed)
        {
            return;
        }

        state = nextState;
        stateElapsed = 0.0f;
        if (AnimationState.Attack == nextState || AnimationState.Special == nextState)
        {
            attackDamageApplied = false;
            projectileLaunched = false;
        }
    }

    private void UpdateLoopAnimation()
    {
        stateElapsed += Time.deltaTime;
        ApplyFrame(true);
    }

    private bool UpdateLockedAnimation(bool loop)
    {
        stateElapsed += Time.deltaTime;
        ApplyFrame(loop);
        return stateElapsed >= GetDuration(GetCurrentFrames());
    }

    private void ApplyFrame(bool loop)
    {
        Sprite[] frames = GetCurrentFrames();
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        int index = Mathf.FloorToInt(stateElapsed * config.animationFps);
        if (loop)
        {
            index %= frames.Length;
        }
        else
        {
            index = Mathf.Min(index, frames.Length - 1);
        }

        spriteRenderer.sprite = frames[index];
    }

    private float GetDuration(Sprite[] frames)
    {
        if (null == frames || 0 == frames.Length)
        {
            return 0.1f;
        }

        return frames.Length / config.animationFps;
    }

    private Sprite[] GetCurrentFrames()
    {
        switch (state)
        {
            case AnimationState.Walk:
                return walkFrames;
            case AnimationState.Attack:
                return attackFrames;
            case AnimationState.Special:
                return specialFrames;
            case AnimationState.Hit:
                return hitFrames;
            case AnimationState.Death:
                return deathFrames;
            default:
                return idleFrames;
        }
    }
}

/// <summary>
/// 원거리 몬스터가 쏘는 탄. 벽에 닿거나 사거리를 넘으면 사라진다.
/// 스프라이트는 몬스터가 이미 잘라둔 것을 넘겨받아 쓴다.
/// </summary>
public class MonsterProjectile : MonoBehaviour
{
    private const float Lifetime = 3.0f;
    private const float HitRadius = 0.45f;

    private TileMap tileMap;
    private SpriteRenderer spriteRenderer;
    private Player player;
    private Sprite[] frames;

    private Vector2 direction;
    private float speed;
    private float animationFps;
    private int damage;
    private float elapsed;

    public static void Spawn(
        TileMap map,
        Vector3 origin,
        Vector2 moveDirection,
        int damageAmount,
        float moveSpeed,
        Player target,
        Sprite[] sprites,
        float fps)
    {
        if (null == sprites || 0 == sprites.Length)
        {
            return;
        }

        GameObject projectileObject = new GameObject("MonsterProjectile");
        projectileObject.transform.position = new Vector3(origin.x, origin.y, 0.0f);

        MonsterProjectile projectile = projectileObject.AddComponent<MonsterProjectile>();
        projectile.tileMap = map;
        projectile.direction = moveDirection.normalized;
        projectile.damage = damageAmount;
        projectile.speed = moveSpeed;
        projectile.player = target;
        projectile.frames = sprites;
        projectile.animationFps = fps;
        projectile.Setup();
    }

    private void Setup()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 20;

        // 탄 스프라이트는 오른쪽을 향해 그려져 있다.
        // 돌려주지 않으면 아래로 날아가면서도 옆을 보고 있어 어색하다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);

        // 왼쪽으로 쏘면 180도 돌아가면서 위아래가 뒤집힌다. 그것만 되돌린다.
        spriteRenderer.flipY = direction.x < 0.0f;

        ApplyFrame();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        ApplyFrame();

        if (elapsed >= Lifetime || false == IsFloor(transform.position))
        {
            Destroy(gameObject);
            return;
        }

        if (null == player || true == player.IsDead)
        {
            return;
        }

        if (Vector2.Distance(transform.position, player.transform.position) <= HitRadius)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }

    private bool IsFloor(Vector2 position)
    {
        if (null == tileMap)
        {
            return true;
        }

        Tile tile = tileMap.GetTile(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        if (null == tile || Tile.Type.Floor != tile.type)
        {
            return false;
        }

        if (null != tile.door && Door.State.Open != tile.door.state)
        {
            return false;
        }

        // 돌 같은 소품도 막는다. 몬스터 이동 판정은 이미 이걸 보고 있었다.
        return false == PropBlock.IsBlocked(tile.index);
    }

    private void ApplyFrame()
    {
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        int index = Mathf.FloorToInt(elapsed * animationFps) % frames.Length;
        spriteRenderer.sprite = frames[index];
    }
}
