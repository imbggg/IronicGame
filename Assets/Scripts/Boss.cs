using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 낱장 PNG 폴더나 시트에서 프레임을 읽어오는 공용 로더.
/// 낱장을 Sprite로 직접 읽지 않고 Texture2D로 읽어서 Sprite.Create 하는 이유는,
/// Import 설정의 Pixels Per Unit(기본 100)에 끌려가지 않게 하기 위해서다.
/// </summary>
public static class BossFrameLoader
{
    /// <summary>
    /// 폴더 안의 낱장을 파일명 끝 숫자 순서로 정렬해 돌려준다.
    /// 파일이 없으면 빈 배열.
    /// </summary>
    public static Sprite[] LoadFolder(string folder, float pixelsPerUnit, Vector2 pivot)
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(folder);
        if (null == textures || 0 == textures.Length)
        {
            return new Sprite[0];
        }

        List<Texture2D> ordered = new List<Texture2D>(textures);
        SortByTrailingNumber(ordered);

        Sprite[] frames = new Sprite[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            frames[i] = Create(ordered[i], pixelsPerUnit, pivot);
        }

        return frames;
    }

    /// <summary>
    /// 폴더 안의 낱장을 클립별로 나눠 돌려준다.
    /// Boss_Attack3_0 / Boss.Attack3_0 어느 쪽이어도 "Attack3"로 묶인다.
    /// </summary>
    public static Dictionary<string, Sprite[]> LoadClips(
        string folder, float pixelsPerUnit, Vector2 pivot)
    {
        Dictionary<string, Sprite[]> result = new Dictionary<string, Sprite[]>();

        Texture2D[] textures = Resources.LoadAll<Texture2D>(folder);
        if (null == textures || 0 == textures.Length)
        {
            return result;
        }

        Dictionary<string, List<Texture2D>> buckets = new Dictionary<string, List<Texture2D>>();

        for (int i = 0; i < textures.Length; i++)
        {
            string clipName = ExtractClipName(textures[i].name);
            if (null == clipName)
            {
                continue;
            }

            if (false == buckets.ContainsKey(clipName))
            {
                buckets[clipName] = new List<Texture2D>();
            }

            buckets[clipName].Add(textures[i]);
        }

        foreach (KeyValuePair<string, List<Texture2D>> pair in buckets)
        {
            List<Texture2D> ordered = pair.Value;
            SortByTrailingNumber(ordered);

            Sprite[] frames = new Sprite[ordered.Count];
            for (int i = 0; i < ordered.Count; i++)
            {
                frames[i] = Create(ordered[i], pixelsPerUnit, pivot);
            }

            result[pair.Key] = frames;
        }

        return result;
    }

    /// <summary>가로로 이어붙인 시트를 자른다.</summary>
    public static Sprite[] SliceRow(
        string resourcePath, int frameWidth, int frameHeight, int frameCount,
        float pixelsPerUnit, Vector2 pivot)
    {
        Texture2D texture = LoadTexture(resourcePath);
        if (null == texture)
        {
            return new Sprite[0];
        }

        int count = Mathf.Min(frameCount, texture.width / frameWidth);
        Sprite[] frames = new Sprite[Mathf.Max(0, count)];

        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                texture,
                new Rect(i * frameWidth, texture.height - frameHeight, frameWidth, frameHeight),
                pivot,
                pixelsPerUnit
            );
        }

        return frames;
    }

    /// <summary>
    /// 격자 시트를 자른다. rowCounts는 각 행에서 실제로 채워진 칸 수다.
    /// unholy_ground는 행마다 끝에 빈 칸이 있어서 이 값이 필요하다.
    /// </summary>
    public static Sprite[] SliceGrid(
        string resourcePath, int frameWidth, int frameHeight, int[] rowCounts,
        float pixelsPerUnit, Vector2 pivot)
    {
        Texture2D texture = LoadTexture(resourcePath);
        if (null == texture)
        {
            return new Sprite[0];
        }

        List<Sprite> frames = new List<Sprite>();

        for (int row = 0; row < rowCounts.Length; row++)
        {
            // Unity 텍스처는 왼쪽 아래가 원점이라 행 순서를 뒤집는다.
            int y = texture.height - (row + 1) * frameHeight;
            if (0 > y)
            {
                break;
            }

            for (int column = 0; column < rowCounts[row]; column++)
            {
                frames.Add(Sprite.Create(
                    texture,
                    new Rect(column * frameWidth, y, frameWidth, frameHeight),
                    pivot,
                    pixelsPerUnit
                ));
            }
        }

        return frames.ToArray();
    }

    /// <summary>격자 시트에서 지정한 행 하나만 자른다. 색깔별로 행이 나뉜 시트용.</summary>
    public static Sprite[] SliceGridRow(
        string resourcePath, int frameWidth, int frameHeight, int row, int columnCount,
        float pixelsPerUnit, Vector2 pivot)
    {
        Texture2D texture = LoadTexture(resourcePath);
        if (null == texture)
        {
            return new Sprite[0];
        }

        int y = texture.height - (row + 1) * frameHeight;
        if (0 > y)
        {
            Debug.LogError($"[BossFrameLoader] 시트 높이가 예상과 다릅니다: {resourcePath}");
            return new Sprite[0];
        }

        int count = Mathf.Min(columnCount, texture.width / frameWidth);
        Sprite[] frames = new Sprite[Mathf.Max(0, count)];

        for (int i = 0; i < count; i++)
        {
            frames[i] = Sprite.Create(
                texture,
                new Rect(i * frameWidth, y, frameWidth, frameHeight),
                pivot,
                pixelsPerUnit
            );
        }

        return frames;
    }

    private static Texture2D LoadTexture(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (null == texture)
        {
            return null;
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Sprite Create(Texture2D texture, float pixelsPerUnit, Vector2 pivot)
    {
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            pivot,
            pixelsPerUnit
        );
    }

    /// <summary>"Boss_Attack3_0" / "Boss.Attack3_0" → "Attack3"</summary>
    private static string ExtractClipName(string assetName)
    {
        int underscore = assetName.LastIndexOf('_');
        if (0 > underscore || underscore == assetName.Length - 1)
        {
            return null;
        }

        int dummy;
        if (false == int.TryParse(assetName.Substring(underscore + 1), out dummy))
        {
            return null;
        }

        string clipName = assetName.Substring(0, underscore);

        if (clipName.StartsWith("Boss.") || clipName.StartsWith("Boss_"))
        {
            clipName = clipName.Substring(5);
        }

        return clipName;
    }

    /// <summary>파일명 끝 숫자로 정렬한다. 문자열 정렬이면 10이 2보다 앞에 온다.</summary>
    private static void SortByTrailingNumber(List<Texture2D> items)
    {
        items.Sort(delegate (Texture2D a, Texture2D b)
        {
            return TrailingNumber(a.name).CompareTo(TrailingNumber(b.name));
        });
    }

    private static int TrailingNumber(string assetName)
    {
        int underscore = assetName.LastIndexOf('_');
        if (0 > underscore || underscore == assetName.Length - 1)
        {
            return 0;
        }

        int value;
        if (false == int.TryParse(assetName.Substring(underscore + 1), out value))
        {
            return 0;
        }

        return value;
    }
}

/// <summary>
/// 네크로맨서 보스. Animator 없이 낱장 스프라이트를 코드로 재생한다.
/// 스프라이트는 Assets/Resources/Boss/ 아래에 있어야 한다.
/// 체력 100 / 2페이즈. 공격 3종.
/// </summary>
public class Boss : MonoBehaviour, IDamageable
{
    private enum BossState
    {
        Spawn,
        Idle,
        Walk,
        Attack1,
        Attack2,
        Attack3,
        Hurt,
        Death
    }

    private const string BossFolder = "Boss";   // Resources/Boss/

    private const float PixelsPerUnit = 32.0f;
    private const float AnimationFps = 12.0f;

    // 각 공격에서 실제로 기술이 나가는 프레임 (0-based)
    private const int Attack1CastFrame = 8;     // 시트에 폭발이 보이는 프레임
    private const int Attack2CastFrame = 6;
    private const int Attack3CastFrame = 11;    // 구체가 가장 커지는 시점

    // ─────────────────────────────────────────────
    // 밸런스 (플레이어 최대 체력 10 기준)
    // ─────────────────────────────────────────────
    public const int MaxHealth = 100;

    private const float PhaseTwoRatio = 0.5f;

    private const int GroundDamagePhase1 = 2;
    private const int GroundDamagePhase2 = 3;
    private const int LightningDamagePhase1 = 1;
    private const int LightningDamagePhase2 = 2;
    private const int OrbDamagePhase1 = 2;
    private const int OrbDamagePhase2 = 3;

    /// <summary>2페이즈 번개 3발의 좌우 오프셋과 모양(0=Lightning, 1=Lightning2, 2=Lightning3).</summary>
    private static readonly float[] PhaseTwoBoltOffsets = { -2.5f, 0.0f, 2.5f };
    private static readonly int[] PhaseTwoBoltVariants = { 0, 2, 1 };

    private const float AttackCooldownPhase1 = 2.2f;
    private const float AttackCooldownPhase2 = 1.5f;

    private const float SpeedPhase1 = 1.6f;
    private const float SpeedPhase2 = 2.2f;

    /// <summary>2페이즈 Attack3은 사방으로 퍼지는 구체 몇 발인지.</summary>
    private const int PhaseTwoOrbCount = 8;

    private const float KeepDistance = 3.0f;    // 이 거리보다 가까우면 더 안 붙는다
    private const float OrbMinDistance = 2.5f;  // 너무 붙으면 구체를 안 쏜다
    private const float DetectionRange = 40.0f;

    private const float SpawnInvulnerableTime = 1.2f;
    private const float HurtCooldown = 0.6f;    // 매 타격마다 경직되면 아무것도 못 한다

    // ─────────────────────────────────────────────
    [SerializeField, Tooltip("벽/타일 충돌용 반경. 너무 키우면 보스가 좁은 길을 못 지나간다.")]
    private float radius = 0.6f;

    // 프리팹에 콜라이더가 없을 때만 쓰는 기본값. 피격 판정용이라 몸통 크기에 맞춘다.
    private const float defaultColliderRadius = 1.3f;
    [SerializeField] private int lootFragmentCount = 0;

    private TileMap tileMap;
    private Player player;
    private SpriteRenderer spriteRenderer;

    private Dictionary<string, Sprite[]> clips;

    private BossState state;
    private float stateElapsed;
    private float cooldownRemaining;
    private float hurtCooldownRemaining;
    private int health;
    private BossState lastAttack = BossState.Idle;
    private readonly List<BossState> attackCandidates = new List<BossState>();

    private int phase = 1;
    private bool castTriggered;
    private bool defeatHandled;
    private bool initialized;

    public static Boss Instance { get; private set; }

    /// <summary>보스가 쓰러졌을 때 한 번 호출된다. 던전 클리어 처리를 여기에 연결한다.</summary>
    public static event System.Action OnDefeated;

    public int Health { get { return health; } }
    public int Phase { get { return phase; } }
    public bool IsDefeated { get { return BossState.Death == state; } }
    public int RoomIndex { get; private set; } = -1;

    // ─────────────────────────────────────────────
    // 생성 / 초기화
    // ─────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        SetupComponents();

        clips = BossFrameLoader.LoadClips(BossFolder, PixelsPerUnit, new Vector2(0.5f, 0.5f));

        if (0 == clips.Count)
        {
            Debug.LogError(
                $"[Boss] Resources/{BossFolder}/ 에서 스프라이트를 찾지 못했습니다. " +
                "폴더가 Assets/Resources/ 아래에 있는지 확인하세요.");
        }

        health = MaxHealth;
        SetState(BossState.Spawn);
        ApplyFrame(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>프리팹으로 Instantiate한 경우. 기존 호출부와 호환된다.</summary>
    public void Init(TileMap map)
    {
        Init(map, transform.position, null, -1);
    }

    public void Init(TileMap map, Vector2 startPosition, Player target, int spawnRoomIndex = -1)
    {
        tileMap = map;
        player = target;
        RoomIndex = spawnRoomIndex;
        initialized = true;

        transform.position = new Vector3(startPosition.x, startPosition.y, 0.0f);

        SetState(BossState.Spawn);
        ApplyFrame(true);
    }

    /// <summary>프리팹 없이 코드로 생성한다.</summary>
    public static Boss Spawn(TileMap map, Vector3 position, Player target, int roomIndex = -1)
    {
        GameObject bossObject = new GameObject("Boss");
        bossObject.transform.position = new Vector3(position.x, position.y, 0.0f);

        Boss boss = bossObject.AddComponent<Boss>();
        boss.Init(map, position, target, roomIndex);
        return boss;
    }

    private void SetupComponents()
    {
        // Animator가 살아 있으면 매 프레임 sprite를 덮어써서 코드 재생이 보이지 않는다.
        Animator animator = GetComponent<Animator>();
        if (null != animator)
        {
            animator.enabled = false;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (null == spriteRenderer)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sortingOrder = 20;
        spriteRenderer.color = Color.white;

        CircleCollider2D bodyCollider = GetComponent<CircleCollider2D>();
        if (null == bodyCollider)
        {
            // 프리팹에 콜라이더가 없을 때만 새로 붙인다.
            // 이미 있으면 Inspector에서 맞춘 Radius/Offset을 그대로 존중한다.
            bodyCollider = gameObject.AddComponent<CircleCollider2D>();
            bodyCollider.radius = defaultColliderRadius;
        }

        bodyCollider.isTrigger = true;

        // 화살의 OnTriggerEnter2D가 발생하려면 한쪽에 Rigidbody2D가 있어야 한다.
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (null == body)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;
        body.gravityScale = 0.0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private Sprite[] GetClip(string clipName)
    {
        Sprite[] frames;
        if (null != clips && clips.TryGetValue(clipName, out frames) && 0 < frames.Length)
        {
            return frames;
        }

        return null;
    }

    // ─────────────────────────────────────────────
    // 루프
    // ─────────────────────────────────────────────

    private void Update()
    {
        stateElapsed += Time.deltaTime;
        hurtCooldownRemaining = Mathf.Max(0.0f, hurtCooldownRemaining - Time.deltaTime);

        if (BossState.Death == state)
        {
            UpdateDeath();
            return;
        }

        if (false == initialized)
        {
            ApplyFrame(true);
            return;
        }

        if (BossState.Spawn == state)
        {
            ApplyFrame(true);
            if (stateElapsed >= SpawnInvulnerableTime)
            {
                SetState(BossState.Idle);
            }
            return;
        }

        if (BossState.Hurt == state)
        {
            ApplyFrame(false);
            if (stateElapsed >= GetDuration(GetCurrentFrames()))
            {
                SetState(BossState.Idle);
            }
            return;
        }

        if (IsAttacking())
        {
            UpdateAttack();
            return;
        }

        if (null == player)
        {
            player = FindAnyObjectByType<Player>();
        }

        cooldownRemaining = Mathf.Max(0.0f, cooldownRemaining - Time.deltaTime);

        if (null == player || true == player.IsDead)
        {
            SetState(BossState.Idle);
            ApplyFrame(true);
            return;
        }

        Vector2 direction = (Vector2)player.transform.position - (Vector2)transform.position;
        float distance = direction.magnitude;

        if (0.001f < distance)
        {
            spriteRenderer.flipX = direction.x < 0.0f;
        }

        if (0.0f >= cooldownRemaining && distance <= DetectionRange)
        {
            BeginAttack(ChooseAttack(distance));
            return;
        }

        if (distance > KeepDistance && 0.001f < distance)
        {
            direction.Normalize();
            Move(direction);
            SetState(BossState.Walk);
        }
        else
        {
            SetState(BossState.Idle);
        }

        ApplyFrame(true);
    }

    private bool IsAttacking()
    {
        return BossState.Attack1 == state
            || BossState.Attack2 == state
            || BossState.Attack3 == state;
    }

    /// <summary>
    /// 거리로 쓸 수 있는 기술을 추리고, 직전에 쓴 기술은 후보에서 뺀다.
    /// 순수 랜덤이면 같은 기술이 연속으로 나와 한 가지만 쓰는 것처럼 보인다.
    /// </summary>
    private BossState ChooseAttack(float distance)
    {
        attackCandidates.Clear();
        attackCandidates.Add(BossState.Attack1);
        attackCandidates.Add(BossState.Attack2);

        if (distance >= OrbMinDistance)
        {
            attackCandidates.Add(BossState.Attack3);
        }

        if (1 < attackCandidates.Count && true == attackCandidates.Contains(lastAttack))
        {
            attackCandidates.Remove(lastAttack);
        }

        BossState chosen = attackCandidates[Random.Range(0, attackCandidates.Count)];
        lastAttack = chosen;

        return chosen;
    }

    private void UpdateAttack()
    {
        ApplyFrame(false);

        int castFrame;
        switch (state)
        {
            case BossState.Attack1:
                castFrame = Attack1CastFrame;
                break;
            case BossState.Attack2:
                castFrame = Attack2CastFrame;
                break;
            default:
                castFrame = Attack3CastFrame;
                break;
        }

        if (false == castTriggered && GetFrameIndex() >= castFrame)
        {
            castTriggered = true;
            Cast();
        }

        if (stateElapsed >= GetDuration(GetCurrentFrames()))
        {
            SetState(BossState.Idle);
        }
    }

    private void Cast()
    {
        if (null == player)
        {
            return;
        }

        switch (state)
        {
            case BossState.Attack1:
                RetroAudio.Play(RetroSound.BossGroundCast);
                CastGroundZone();
                break;
            case BossState.Attack2:
                RetroAudio.Play(RetroSound.BossLightningCast);
                CastLightning();
                break;
            default:
                RetroAudio.Play(RetroSound.BossOrbCast);
                CastOrb();
                break;
        }
    }

    /// <summary>시전 시점 플레이어 발밑에 장판. 피해는 장판이 낸다.</summary>
    private void CastGroundZone()
    {
        int damage = 2 <= phase ? GroundDamagePhase2 : GroundDamagePhase1;
        BossGroundZone.Spawn(player.transform.position, damage, 2 <= phase, player);
    }

    /// <summary>
    /// 시전 시점의 플레이어 위치를 노린다. 예고가 끝날 때까지 그 자리에 있으면 맞는다.
    /// 피해 판정은 BossLightning이 낙하 시점에 낸다.
    /// </summary>
    private void CastLightning()
    {
        int damage = 2 <= phase ? LightningDamagePhase2 : LightningDamagePhase1;
        Vector3 origin = player.transform.position;

        if (2 > phase)
        {
            BossLightning.Spawn(origin, damage, player, 0);
            return;
        }

        // 2페이즈: 좌 / 중앙 / 우로 세 발이 동시에 떨어진다. 중앙만 시전 시점 플레이어 위치다.
        for (int i = 0; i < PhaseTwoBoltOffsets.Length; i++)
        {
            Vector3 position = origin + new Vector3(PhaseTwoBoltOffsets[i], 0.0f, 0.0f);
            BossLightning.Spawn(position, damage, player, PhaseTwoBoltVariants[i]);
        }
    }

    /// <summary>플레이어 방향으로 구체 1발. 벽에 닿으면 터지고 사라진다.</summary>
    private void CastOrb()
    {
        Vector2 direction = (Vector2)player.transform.position - (Vector2)transform.position;
        if (0.001f > direction.magnitude)
        {
            direction = Vector2.right;
        }

        direction.Normalize();
        int damage = 2 <= phase ? OrbDamagePhase2 : OrbDamagePhase1;

        if (2 > phase)
        {
            BossOrb.Spawn(tileMap, transform.position, direction, damage, player, 0);
            return;
        }

        // 2페이즈: 플레이어 방향을 기준으로 사방에 고르게 뿌린다. 색은 순서대로 돌아간다.
        float baseAngle = Mathf.Atan2(direction.y, direction.x);
        float step = Mathf.PI * 2.0f / PhaseTwoOrbCount;

        for (int i = 0; i < PhaseTwoOrbCount; i++)
        {
            float angle = baseAngle + step * i;
            Vector2 spread = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            BossOrb.Spawn(
                tileMap, transform.position, spread, damage, player, i % BossOrb.ColorCount);
        }
    }

    private void UpdateDeath()
    {
        ApplyFrame(false);

        if (stateElapsed >= GetDuration(GetCurrentFrames()) + 0.2f)
        {
            if (false == defeatHandled)
            {
                defeatHandled = true;

                if (0 < lootFragmentCount)
                {
                    LootChest.Spawn(transform.position, lootFragmentCount);
                }

                if (null != OnDefeated)
                {
                    OnDefeated();
                }

                // 보스를 잡으면 던전 클리어. 승리 UI에 다시하기와 종료 버튼이 들어 있다.
                GameEndUI.ShowClear();
            }

            Destroy(gameObject);
        }
    }

    // ─────────────────────────────────────────────
    // 피격
    // ─────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        if (BossState.Death == state || BossState.Spawn == state || 0 >= damage)
        {
            return;
        }

        health -= damage;

        if (1 == phase && health <= Mathf.RoundToInt(MaxHealth * PhaseTwoRatio))
        {
            EnterPhaseTwo();
        }

        if (0 >= health)
        {
            health = 0;
            SetState(BossState.Death);
            RetroAudio.Play(RetroSound.BossDeath);

            Collider2D bodyCollider = GetComponent<Collider2D>();
            if (null != bodyCollider)
            {
                bodyCollider.enabled = false;
            }
            return;
        }

        RetroAudio.Play(RetroSound.BossHit);

        // 공격 중에는 경직시키지 않는다. 그러지 않으면 보스가 아무것도 못 한다.
        if (false == IsAttacking() && 0.0f >= hurtCooldownRemaining)
        {
            hurtCooldownRemaining = HurtCooldown;
            SetState(BossState.Hurt);
        }
    }

    private void EnterPhaseTwo()
    {
        phase = 2;
        cooldownRemaining = 0.5f;
        RetroAudio.Play(RetroSound.BossPhaseTwo);
    }

    private void BeginAttack(BossState attackState)
    {
        SetState(attackState);
        castTriggered = false;

        float cooldown = 2 <= phase ? AttackCooldownPhase2 : AttackCooldownPhase1;
        cooldownRemaining = cooldown * GameData.GetMonsterAttackCooldownMultiplier();

        ApplyFrame(false);
    }

    // ─────────────────────────────────────────────
    // 이동 / 충돌 (Monster.cs와 동일한 방식)
    // ─────────────────────────────────────────────

    private void Move(Vector2 direction)
    {
        float speed = 2 <= phase ? SpeedPhase2 : SpeedPhase1;
        float distance = speed * Time.deltaTime;
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

    // ─────────────────────────────────────────────
    // 애니메이션
    // ─────────────────────────────────────────────

    private void SetState(BossState nextState)
    {
        if (state == nextState && 0.0f < stateElapsed)
        {
            return;
        }

        state = nextState;
        stateElapsed = 0.0f;
    }

    private int GetFrameIndex()
    {
        return Mathf.FloorToInt(stateElapsed * AnimationFps);
    }

    private void ApplyFrame(bool loop)
    {
        Sprite[] frames = GetCurrentFrames();
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        int index = GetFrameIndex();
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

        return frames.Length / AnimationFps;
    }

    private Sprite[] GetCurrentFrames()
    {
        Sprite[] frames = null;

        switch (state)
        {
            case BossState.Walk:
                // 파일명이 Run인 경우와 Walk인 경우를 둘 다 받는다.
                frames = GetClip("Run");
                if (null == frames)
                {
                    frames = GetClip("Walk");
                }
                break;
            case BossState.Attack1:
                frames = GetClip("Attack1");
                break;
            case BossState.Attack2:
                frames = GetClip("Attack2");
                break;
            case BossState.Attack3:
                frames = GetClip("Attack3");
                break;
            case BossState.Hurt:
                frames = GetClip("Hurt");
                break;
            case BossState.Death:
                frames = GetClip("Death");
                break;
            default:
                frames = GetClip("Idle");
                break;
        }

        if (null == frames)
        {
            frames = GetClip("Idle");
        }

        return frames;
    }

    // ─────────────────────────────────────────────
    // 체력바
    // ─────────────────────────────────────────────

    private void OnGUI()
    {
        if (BossState.Death == state || 0 >= health || false == initialized)
        {
            return;
        }

        // 결과 화면이 떠 있으면 그리지 않는다.
        // Time.timeScale이 0이어도 OnGUI는 계속 돌기 때문에 직접 막아야 한다.
        if (true == GameEndUI.IsShowing)
        {
            return;
        }

        float barWidth = Screen.width * 0.5f;
        float barHeight = Mathf.Max(18.0f, Screen.height * 0.022f);
        float left = (Screen.width - barWidth) * 0.5f;
        float top = Screen.height * 0.045f;

        Rect background = new Rect(left, top, barWidth, barHeight);
        Rect fill = new Rect(left, top, barWidth * ((float)health / MaxHealth), barHeight);

        Color previousColor = GUI.color;

        GUI.color = new Color(0.05f, 0.02f, 0.08f, 0.88f);
        GUI.DrawTexture(background, Texture2D.whiteTexture);

        GUI.color = 2 <= phase
            ? new Color(0.98f, 0.80f, 0.20f, 1.0f)
            : new Color(0.80f, 0.20f, 0.30f, 1.0f);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);

        GUI.color = previousColor;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.020f), 10, 26);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        GUI.Label(background, $"BOSS   {health} / {MaxHealth}", style);
    }
}

// ═════════════════════════════════════════════════
// Attack1 — unholy_ground 장판
// ═════════════════════════════════════════════════

/// <summary>
/// 플레이어 발밑에 깔리는 저주받은 땅.
/// 앞 17프레임은 십자가가 솟는 예고(연출만), 17프레임부터 번개가 떨어지며 피해를 준다.
/// </summary>
public class BossGroundZone : MonoBehaviour
{
    private const string RedFolder = "Effects/UnholyGroundRed";
    private const string YellowFolder = "Effects/UnholyGroundYellow";

    private const string RedSheet = "Effects/unholy_ground_size_reduction-Sheet";
    private const string YellowSheet = "Effects/unholy_ground_size_reduction_yellow-Sheet";

    private const int FrameWidth = 300;
    private const int FrameHeight = 256;

    // 행마다 채워진 칸 수가 다르다. 이걸 무시하면 빈 칸이 프레임으로 잡힌다.
    private static readonly int[] RowCounts = { 9, 8, 8, 7, 6 };

    private const int StrikeFrame = 17;         // 번개가 내리꽂히기 시작하는 프레임

    private const float WarningFps = 34.0f;     // 예고 구간은 빠르게 돌린다
    private const float StrikeFps = 20.0f;

    private const float PixelsPerUnit = 48.0f;
    private const float PivotY = 0.40f;
    private const float DamageRadius = 2.2f;

    private static Sprite[] redFrames;
    private static Sprite[] yellowFrames;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private Player player;

    private int damage;
    private float elapsed;
    private bool damageApplied;

    public static void Spawn(Vector3 position, int damageAmount, bool yellow, Player target)
    {
        GameObject zoneObject = new GameObject("BossGroundZone");
        zoneObject.transform.position = new Vector3(position.x, position.y, 0.0f);

        BossGroundZone zone = zoneObject.AddComponent<BossGroundZone>();
        zone.damage = damageAmount;
        zone.player = target;
        zone.Setup(yellow);
    }

    private void Setup(bool yellow)
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 18;   // 보스(20)보다 아래, 바닥 위

        if (yellow)
        {
            if (null == yellowFrames || 0 == yellowFrames.Length)
            {
                yellowFrames = Load(YellowFolder, YellowSheet);
            }

            frames = yellowFrames;
        }
        else
        {
            if (null == redFrames || 0 == redFrames.Length)
            {
                redFrames = Load(RedFolder, RedSheet);
            }

            frames = redFrames;
        }

        ApplyFrame();
    }

    /// <summary>낱장 폴더가 있으면 그걸 쓰고, 없으면 시트를 자른다.</summary>
    private static Sprite[] Load(string folder, string sheetPath)
    {
        Vector2 pivot = new Vector2(0.5f, PivotY);

        Sprite[] loose = BossFrameLoader.LoadFolder(folder, PixelsPerUnit, pivot);
        if (0 < loose.Length)
        {
            return loose;
        }

        Sprite[] sliced = BossFrameLoader.SliceGrid(
            sheetPath, FrameWidth, FrameHeight, RowCounts, PixelsPerUnit, pivot);

        if (0 == sliced.Length)
        {
            Debug.LogError(
                $"[BossGroundZone] 프레임을 찾지 못했습니다: " +
                $"Resources/{folder}/ 또는 Resources/{sheetPath}.png");
        }

        return sliced;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        ApplyFrame();

        if (false == damageApplied && GetFrameIndex() >= StrikeFrame)
        {
            damageApplied = true;
            TryDamage();
        }

        if (null != frames && GetFrameIndex() >= frames.Length)
        {
            Destroy(gameObject);
        }
    }

    private void TryDamage()
    {
        if (null == player || true == player.IsDead)
        {
            return;
        }

        if (Vector2.Distance(transform.position, player.transform.position) > DamageRadius)
        {
            return;
        }

        player.TakeDamage(GameData.GetMonsterAttackDamage(damage));
    }

    /// <summary>예고 구간과 타격 구간의 재생 속도가 달라서 직접 계산한다.</summary>
    private int GetFrameIndex()
    {
        float warningDuration = StrikeFrame / WarningFps;

        if (elapsed < warningDuration)
        {
            return Mathf.FloorToInt(elapsed * WarningFps);
        }

        return StrikeFrame + Mathf.FloorToInt((elapsed - warningDuration) * StrikeFps);
    }

    private void ApplyFrame()
    {
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        int index = Mathf.Clamp(GetFrameIndex(), 0, frames.Length - 1);
        spriteRenderer.sprite = frames[index];
    }
}

// ═════════════════════════════════════════════════
// Attack2 — 수직 번개 (연출 전용, 피해는 Boss가 즉시 준다)
// ═════════════════════════════════════════════════

public class BossLightning : MonoBehaviour
{
    // 0 = lightning_1(대각), 1 = lightning_2(약간 기울어짐), 2 = lightning_3(수직)
    private static readonly string[] Folders =
    {
        "Effects/Lightning", "Effects/Lightning2", "Effects/Lightning3"
    };

    private static readonly string[] SheetPaths =
    {
        "Effects/lightning_1", "Effects/lightning_2", "Effects/lightning_3"
    };

    private const int FrameWidth = 100;
    private const int FrameHeight = 208;
    private const int FrameCount = 9;

    private const float PixelsPerUnit = 32.0f;
    private const float PivotY = 0.04f;      // 번개가 꽂히는 지점이 셀 맨 아래다
    private const float Fps = 20.0f;

    /// <summary>낙하까지의 예고 시간. 이 안에 자리를 벗어나면 피할 수 있다.</summary>
    private const float WarningDuration = 0.6f;

    /// <summary>낙하 순간 이 반경 안에 있으면 맞는다.</summary>
    private const float HitRadius = 0.8f;

    private const float WarningAlpha = 0.35f;
    private const float WarningBlinkFps = 6.0f;

    private static readonly Sprite[][] variantFrames = new Sprite[Folders.Length][];

    private const float SoundMergeWindow = 0.08f;
    private static float lastStrikeSoundTime = -1.0f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private Player player;
    private int damage;
    private float elapsed;
    private bool struck;

    public static void Spawn(Vector3 position, int damageAmount, Player target, int variant)
    {
        GameObject boltObject = new GameObject("BossLightning");
        boltObject.transform.position = new Vector3(position.x, position.y, 0.0f);

        BossLightning bolt = boltObject.AddComponent<BossLightning>();
        bolt.damage = damageAmount;
        bolt.player = target;
        bolt.Setup(variant);
    }

    private void Setup(int variant)
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 22;

        frames = LoadVariant(Mathf.Clamp(variant, 0, Folders.Length - 1));

        ApplyFrame();
    }

    /// <summary>모양별로 한 번만 로드해서 캐시한다.</summary>
    private static Sprite[] LoadVariant(int variant)
    {
        if (null != variantFrames[variant] && 0 < variantFrames[variant].Length)
        {
            return variantFrames[variant];
        }

        Vector2 pivot = new Vector2(0.5f, PivotY);

        Sprite[] loaded = BossFrameLoader.LoadFolder(Folders[variant], PixelsPerUnit, pivot);
        if (0 == loaded.Length)
        {
            loaded = BossFrameLoader.SliceRow(
                SheetPaths[variant], FrameWidth, FrameHeight, FrameCount, PixelsPerUnit, pivot);
        }

        if (0 == loaded.Length)
        {
            Debug.LogError(
                $"[BossLightning] 프레임 없음: Resources/{Folders[variant]}/ 또는 Resources/{SheetPaths[variant]}.png");
        }

        variantFrames[variant] = loaded;
        return loaded;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (elapsed < WarningDuration)
        {
            ApplyFrame();
            return;
        }

        if (false == struck)
        {
            struck = true;
            PlayStrikeSound();
            TryDamage();
        }

        ApplyFrame();

        if (null != frames && 0 < frames.Length
            && elapsed >= WarningDuration + frames.Length / Fps)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>2페이즈는 세 발이 같은 순간에 떨어진다. 그대로 두면 소리가 세 번 겹쳐 터진다.</summary>
    private static void PlayStrikeSound()
    {
        if (Time.time - lastStrikeSoundTime < SoundMergeWindow)
        {
            return;
        }

        lastStrikeSoundTime = Time.time;
        RetroAudio.Play(RetroSound.BossLightningStrike);
    }

    private void TryDamage()
    {
        if (null == player || true == player.IsDead)
        {
            return;
        }

        if (Vector2.Distance(transform.position, player.transform.position) > HitRadius)
        {
            return;
        }

        player.TakeDamage(GameData.GetMonsterAttackDamage(damage));
    }

    private void ApplyFrame()
    {
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        // 예고 구간: 첫 프레임을 흐리게 깜빡여서 낙하 지점을 알린다.
        if (elapsed < WarningDuration)
        {
            spriteRenderer.sprite = frames[0];

            bool visible = 0 == Mathf.FloorToInt(elapsed * WarningBlinkFps) % 2;
            spriteRenderer.color = new Color(
                1.0f, 1.0f, 1.0f, visible ? WarningAlpha : WarningAlpha * 0.4f);
            return;
        }

        spriteRenderer.color = Color.white;

        int index = Mathf.Min(
            Mathf.FloorToInt((elapsed - WarningDuration) * Fps), frames.Length - 1);
        spriteRenderer.sprite = frames[index];
    }
}

// ═════════════════════════════════════════════════
// Attack3 — 해골 구체 (직선 1발)
// ═════════════════════════════════════════════════

public class BossOrb : MonoBehaviour
{
    /// <summary>0=빨강, 1=파랑, 2=초록, 3=자주</summary>
    public const int ColorCount = 4;

    private static readonly string[] Folders =
    {
        "Effects/SkullFireball", "Effects/SkullFireballBlue",
        "Effects/SkullFireballGreen", "Effects/SkullFireballMagenta"
    };

    private static readonly string[] SheetPaths =
    {
        "Effects/skull_fireball_red-Sheet", "Effects/skull_fireball_blue-Sheet",
        "Effects/skull_fireball_green-Sheet", "Effects/skull_fireball_magenta-Sheet"
    };

    private const int FrameSize = 64;
    private const int FrameCount = 8;
    private const float PixelsPerUnit = 32.0f;
    private const float Fps = 14.0f;

    private const float Speed = 6.0f;
    private const float Lifetime = 5.0f;
    private const float HitRadius = 0.6f;

    private static readonly Sprite[][] colorFrames = new Sprite[ColorCount][];

    private TileMap tileMap;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private Player player;
    private Vector2 direction;
    private int color;
    private int damage;
    private float elapsed;

    public static void Spawn(
        TileMap map, Vector3 position, Vector2 moveDirection, int damageAmount,
        Player target, int colorIndex)
    {
        GameObject orbObject = new GameObject("BossOrb");
        orbObject.transform.position = new Vector3(position.x, position.y, 0.0f);

        BossOrb orb = orbObject.AddComponent<BossOrb>();
        orb.tileMap = map;
        orb.direction = moveDirection.normalized;
        orb.damage = damageAmount;
        orb.player = target;
        orb.color = Mathf.Clamp(colorIndex, 0, ColorCount - 1);
        orb.Setup();
    }

    private void Setup()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 21;

        frames = LoadColor(color);

        // 해골이 오른쪽을 보고 있어서 진행 방향으로 돌려준다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);

        ApplyFrame();
    }

    /// <summary>색마다 한 번만 로드해서 캐시한다.</summary>
    private static Sprite[] LoadColor(int colorIndex)
    {
        if (null != colorFrames[colorIndex] && 0 < colorFrames[colorIndex].Length)
        {
            return colorFrames[colorIndex];
        }

        Vector2 pivot = new Vector2(0.5f, 0.5f);

        Sprite[] loaded = BossFrameLoader.LoadFolder(Folders[colorIndex], PixelsPerUnit, pivot);
        if (0 == loaded.Length)
        {
            loaded = BossFrameLoader.SliceRow(
                SheetPaths[colorIndex], FrameSize, FrameSize, FrameCount, PixelsPerUnit, pivot);
        }

        if (0 == loaded.Length)
        {
            Debug.LogError(
                $"[BossOrb] 프레임 없음: Resources/{Folders[colorIndex]}/ 또는 Resources/{SheetPaths[colorIndex]}.png");
        }

        colorFrames[colorIndex] = loaded;
        return loaded;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += (Vector3)(direction * Speed * Time.deltaTime);
        ApplyFrame();

        // 벽에 닿으면 터지고 사라진다. 피해는 없다.
        if (false == IsFloor(transform.position))
        {
            Explode();
            return;
        }

        if (elapsed >= Lifetime)
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
            player.TakeDamage(GameData.GetMonsterAttackDamage(damage));
            Explode();
        }
    }

    private void Explode()
    {
        BossExplosion.Spawn(transform.position, color);
        Destroy(gameObject);
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

        // 돌 같은 소품도 막는다. 보스 본체 이동은 이미 이걸 보고 있었다.
        return false == PropBlock.IsBlocked(tile.index);
    }

    private void ApplyFrame()
    {
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        int index = Mathf.FloorToInt(elapsed * Fps) % frames.Length;
        spriteRenderer.sprite = frames[index];
    }
}

/// <summary>
/// 구체가 터질 때 나는 폭발. 시트를 쓰는 경우 빨간 행(위에서 3번째)만 잘라 쓴다.
/// </summary>
public class BossExplosion : MonoBehaviour
{
    private static readonly string[] Folders =
    {
        "Effects/Explosion", "Effects/ExplosionBlue",
        "Effects/ExplosionGreen", "Effects/ExplosionMagenta"
    };

    private const string SheetPath = "Effects/explosion_simple-Sheet";

    private const int FrameSize = 64;
    private const int Columns = 6;

    /// <summary>시트 행 순서가 초록 / 파랑 / 빨강 / 자주라서, 구체 색(빨강0 파랑1 초록2 자주3)과 어긋난다.</summary>
    private static readonly int[] RowByColor = { 2, 1, 0, 3 };

    private const float PixelsPerUnit = 32.0f;
    private const float Fps = 16.0f;

    private static readonly Sprite[][] colorFrames = new Sprite[RowByColor.Length][];

    private const float SoundMergeWindow = 0.08f;
    private static float lastSoundTime = -1.0f;

    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private float elapsed;

    public static void Spawn(Vector3 position, int colorIndex)
    {
        GameObject explosionObject = new GameObject("BossExplosion");
        explosionObject.transform.position = new Vector3(position.x, position.y, 0.0f);

        BossExplosion explosion = explosionObject.AddComponent<BossExplosion>();
        explosion.Setup(Mathf.Clamp(colorIndex, 0, RowByColor.Length - 1));
    }

    private void Setup(int colorIndex)
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 22;

        frames = LoadColor(colorIndex);

        // 2페이즈는 구체가 여러 발이라 폭발이 몰릴 수 있다. 짧은 간격 안에서는 한 번만 낸다.
        if (Time.time - lastSoundTime >= SoundMergeWindow)
        {
            lastSoundTime = Time.time;
            RetroAudio.Play(RetroSound.BossOrbExplode);
        }

        ApplyFrame();
    }

    /// <summary>색마다 한 번만 로드해서 캐시한다. 폴더가 없으면 시트에서 해당 행만 잘라 쓴다.</summary>
    private static Sprite[] LoadColor(int colorIndex)
    {
        if (null != colorFrames[colorIndex] && 0 < colorFrames[colorIndex].Length)
        {
            return colorFrames[colorIndex];
        }

        Vector2 pivot = new Vector2(0.5f, 0.5f);

        Sprite[] loaded = BossFrameLoader.LoadFolder(Folders[colorIndex], PixelsPerUnit, pivot);
        if (0 == loaded.Length)
        {
            loaded = BossFrameLoader.SliceGridRow(
                SheetPath, FrameSize, FrameSize, RowByColor[colorIndex], Columns,
                PixelsPerUnit, pivot);
        }

        if (0 == loaded.Length)
        {
            Debug.LogError(
                $"[BossExplosion] 프레임 없음: Resources/{Folders[colorIndex]}/ 또는 Resources/{SheetPath}.png");
        }

        colorFrames[colorIndex] = loaded;
        return loaded;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        ApplyFrame();

        if (null != frames && 0 < frames.Length && elapsed >= frames.Length / Fps)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyFrame()
    {
        if (null == frames || 0 == frames.Length || null == spriteRenderer)
        {
            return;
        }

        int index = Mathf.Min(Mathf.FloorToInt(elapsed * Fps), frames.Length - 1);
        spriteRenderer.sprite = frames[index];
    }
}
