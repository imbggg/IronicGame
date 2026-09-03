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

    private Player player;
    private DungeonGenerator generator;
    private int healthPotionCount;
    private int bossFragmentCount;

    public int HealthPotionCount { get { return healthPotionCount; } }
    public int BossFragmentCount { get { return bossFragmentCount; } }
    public int HealthPotionHealAmount { get { return 1; } }

    private void Awake()
    {
        player = GetComponent<Player>();
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

        if (null == generator)
        {
            generator = FindAnyObjectByType<DungeonGenerator>();
        }

        if (null == generator || false == generator.TrySummonBoss())
        {
            return false;
        }

        bossFragmentCount -= BossFragmentsRequired;
        RetroAudio.Play(RetroSound.BossSummon);
        return true;
    }
}
