using UnityEngine;

public class Monster : MonoBehaviour
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
    }

    private const float PixelsPerUnit = 32.0f;

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
        monsterType = Mathf.Clamp(typeNumber, 1, 4);
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
        SetState(AnimationState.Idle);
        ApplyFrame(true);
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
            TryDamagePlayer();
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
        if (0 >= health)
        {
            SetState(AnimationState.Death);
            RetroAudio.Play(RetroSound.MonsterDefeated);

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
    }

    private Sprite[] LoadFrames(string resourcePath, int expectedCount)
    {
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
                PixelsPerUnit
            );
            frames[i].name = $"{texture.name}_{i}";
        }

        return frames;
    }

    private MonsterConfig GetConfig(int typeNumber)
    {
        MonsterConfig result = new MonsterConfig();

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
