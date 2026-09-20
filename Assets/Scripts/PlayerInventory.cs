using UnityEngine;

public enum InventoryItemType
{
    HealthPotion,
    BossSummonStoneFragment
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
public class PlayerInventory : MonoBehaviour
{
    public const int BossFragmentsRequired = 3;

    [SerializeField, Min(1)] private int maximumCountPerItem = 9;

    [SerializeField, Min(0)]
    [Tooltip("테스트용. 시작할 때 들고 있을 보스 소환석 조각 개수. 제출 전에 0으로 되돌릴 것.")]
    private int debugStartingBossFragments = 3;

    private Player player;
    private DungeonGenerator generator;
    private int healthPotionCount;
    private int bossFragmentCount;

    public int HealthPotionCount { get { return healthPotionCount; } }
    public int BossFragmentCount { get { return bossFragmentCount; } }
    public int HealthPotionHealAmount { get { return 1; } }
    public int ItemCapacity { get { return Mathf.Max(1, maximumCountPerItem); } }

    private void Awake()
    {
        player = GetComponent<Player>();

        if (0 < debugStartingBossFragments)
        {
            bossFragmentCount = Mathf.Min(debugStartingBossFragments, maximumCountPerItem);
            Debug.LogWarning(
                $"[PlayerInventory] 테스트 모드: 보스 소환석 조각 {bossFragmentCount}개로 시작합니다.");
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0.0f || null == player || player.IsDead)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            TryUseHealthPotion();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            TryUseBossSummonStone();
        }
    }

    public int GetItemCount(InventoryItemType itemType)
    {
        return InventoryItemType.HealthPotion == itemType
            ? healthPotionCount
            : bossFragmentCount;
    }

    public bool TryAddItem(InventoryItemType itemType, int amount = 1)
    {
        if (0 >= amount)
        {
            return false;
        }

        int currentCount = GetItemCount(itemType);
        if (currentCount >= maximumCountPerItem)
        {
            return false;
        }

        int addedCount = Mathf.Min(amount, maximumCountPerItem - currentCount);

        if (InventoryItemType.HealthPotion == itemType)
        {
            healthPotionCount += addedCount;
        }
        else
        {
            bossFragmentCount += addedCount;
        }

        return 0 < addedCount;
    }

    public bool TryUseHealthPotion()
    {
        if (null == player
            || player.IsDead
            || player.CurrentHealth >= player.MaxHealth
            || 0 >= healthPotionCount)
        {
            return false;
        }

        healthPotionCount--;
        player.Heal(HealthPotionHealAmount);
        RetroAudio.Play(RetroSound.Potion);
        return true;
    }

    public bool TryUseBossSummonStone()
    {
        if (BossFragmentsRequired > bossFragmentCount)
        {
            return false;
        }

        // 보스는 제단 앞에서만 소환할 수 있다.
        BossAltar altar = BossAltar.Instance;
        if (null == altar || true == altar.IsBusy || false == altar.IsPlayerNear(player))
        {
            return false;
        }

        if (null == generator)
        {
            generator = FindAnyObjectByType<DungeonGenerator>();
        }

        if (null == generator)
        {
            return false;
        }

        // 제단이 사라지는 연출이 끝난 뒤에 보스가 등장한다.
        if (false == altar.BeginDisappear(() => generator.TrySummonBoss()))
        {
            return false;
        }

        bossFragmentCount -= BossFragmentsRequired;
        RetroAudio.Play(RetroSound.BossSummon);
        return true;
    }
}
