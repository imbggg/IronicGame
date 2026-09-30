using System.Collections.Generic;
using UnityEngine;

public class RoomCombatSystem : MonoBehaviour
{
    private const float DoorLockDelay = 0.8f;
    private const float SafeDoorDistance = 1.15f;

    /// <summary>다른 UI와 같은 폰트를 쓴다. 없으면 유니티 기본 폰트로 떨어진다.</summary>
    private const string FontPath = "Fonts/Galmuri11-Bold";

    /// <summary>이 화면 높이를 기준으로 글자 크기를 정하고, 실제 높이에 맞춰 늘린다.</summary>
    private const float ReferenceHeight = 900.0f;

    [SerializeField]
    [Tooltip("전투 안내 문구 크기. 화면 높이에 맞춰 자동으로 늘어난다.")]
    private int combatLabelFontSize = 22;

    private GUIStyle combatLabelStyle;

    private DungeonGenerator generator;
    private Player player;
    private int startRoomIndex = -1;
    private int activeRoomIndex = -1;
    private int pendingRoomIndex = -1;
    private float doorLockDelayRemaining;
    private bool enableIronicRewards;

    private readonly HashSet<int> combatRoomIndices = new HashSet<int>();
    private readonly HashSet<int> clearedRoomIndices = new HashSet<int>();

    public void Init(
        DungeonGenerator dungeonGenerator,
        Player target,
        int firstRoomIndex,
        bool useIronicRewards)
    {
        generator = dungeonGenerator;
        player = target;
        startRoomIndex = firstRoomIndex;
        enableIronicRewards = useIronicRewards;
        activeRoomIndex = -1;
        pendingRoomIndex = -1;
        doorLockDelayRemaining = 0.0f;

        combatRoomIndices.Clear();
        clearedRoomIndices.Clear();
        clearedRoomIndices.Add(startRoomIndex);

        Monster[] monsters = FindObjectsByType<Monster>();
        foreach (Monster monster in monsters)
        {
            if (null != monster && 0 <= monster.RoomIndex)
            {
                combatRoomIndices.Add(monster.RoomIndex);
            }
        }

        // 시작 방은 적이 없고 문도 잠기지 않는다.
        UnlockRoomDoors(startRoomIndex, false);
        enabled = true;
    }

    private void Update()
    {
        if (null == generator || null == player || player.IsDead)
        {
            return;
        }

        if (0 <= activeRoomIndex)
        {
            if (0 == CountLivingMonsters(activeRoomIndex))
            {
                int clearedRoomIndex = activeRoomIndex;
                activeRoomIndex = -1;
                clearedRoomIndices.Add(clearedRoomIndex);
                UnlockRoomDoors(clearedRoomIndex, true);

                if (true == enableIronicRewards)
                {
                    IronicRewardUI.Show(player);
                }
            }
            return;
        }

        if (0 <= pendingRoomIndex)
        {
            Block pendingRoom = FindRoomContaining(player.transform.position);
            if (null == pendingRoom || pendingRoom.index != pendingRoomIndex)
            {
                pendingRoomIndex = -1;
                doorLockDelayRemaining = 0.0f;
                return;
            }

            doorLockDelayRemaining -= Time.deltaTime;
            if (0.0f >= doorLockDelayRemaining
                && true == IsPlayerClearOfRoomDoors(pendingRoom))
            {
                activeRoomIndex = pendingRoomIndex;
                pendingRoomIndex = -1;
                LockRoomDoors(activeRoomIndex);
            }
            return;
        }

        Block currentRoom = FindRoomContaining(player.transform.position);
        if (null == currentRoom
            || currentRoom.index == startRoomIndex
            || true == clearedRoomIndices.Contains(currentRoom.index))
        {
            return;
        }

        if (false == combatRoomIndices.Contains(currentRoom.index)
            || 0 == CountLivingMonsters(currentRoom.index))
        {
            clearedRoomIndices.Add(currentRoom.index);
            return;
        }

        pendingRoomIndex = currentRoom.index;
        doorLockDelayRemaining = DoorLockDelay;
    }

    private Block FindRoomContaining(Vector2 position)
    {
        foreach (Block room in generator.rooms)
        {
            if (Block.Type.Room != room.type)
            {
                continue;
            }

            Rect interior = new Rect(
                room.rect.xMin + 0.8f,
                room.rect.yMin + 0.8f,
                Mathf.Max(0.1f, room.rect.width - 1.6f),
                Mathf.Max(0.1f, room.rect.height - 1.6f)
            );

            if (true == interior.Contains(position))
            {
                return room;
            }
        }

        return null;
    }

    private int CountLivingMonsters(int roomIndex)
    {
        int count = 0;
        Monster[] monsters = FindObjectsByType<Monster>();

        foreach (Monster monster in monsters)
        {
            if (null != monster
                && roomIndex == monster.RoomIndex
                && false == monster.IsDefeated)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 특정 방의 문을 밖에서 잠근다. 보스방처럼 몬스터 수와 무관하게
    /// 가둬야 하는 경우에 쓴다. 여기서 잠근 문은 이 시스템이 자동으로 열지 않는다.
    /// </summary>
    public void LockRoomFromOutside(int roomIndex)
    {
        LockRoomDoors(roomIndex);
    }

    /// <summary>보스를 잡은 뒤 등 밖에서 다시 열어야 할 때 쓴다.</summary>
    public void UnlockRoomFromOutside(int roomIndex)
    {
        clearedRoomIndices.Add(roomIndex);
        UnlockRoomDoors(roomIndex, true);
    }

    private void LockRoomDoors(int roomIndex)
    {
        Block room = FindRoom(roomIndex);
        if (null == room)
        {
            return;
        }

        foreach (Door door in generator.doors)
        {
            if (true == IsDoorOnRoomBoundary(door, room))
            {
                door.state = Door.State.Lock;
            }
        }
    }

    private void UnlockRoomDoors(int roomIndex, bool openAfterUnlock)
    {
        Block room = FindRoom(roomIndex);
        if (null == room)
        {
            return;
        }

        foreach (Door door in generator.doors)
        {
            if (false == IsDoorOnRoomBoundary(door, room))
            {
                continue;
            }

            if (Door.State.Lock == door.state)
            {
                door.Unlock();
            }

            if (true == openAfterUnlock)
            {
                door.Open();
            }
        }
    }

    private Block FindRoom(int roomIndex)
    {
        foreach (Block room in generator.rooms)
        {
            if (room.index == roomIndex)
            {
                return room;
            }
        }

        return null;
    }

    private bool IsDoorOnRoomBoundary(Door door, Block room)
    {
        if (null == door || null == door.tile)
        {
            return false;
        }

        int x = (int)door.tile.rect.x;
        int y = (int)door.tile.rect.y;
        int xMin = (int)room.rect.xMin;
        int xMax = (int)room.rect.xMax - 1;
        int yMin = (int)room.rect.yMin;
        int yMax = (int)room.rect.yMax - 1;

        bool horizontalEdge = (y == yMin || y == yMax) && xMin <= x && x <= xMax;
        bool verticalEdge = (x == xMin || x == xMax) && yMin <= y && y <= yMax;
        return horizontalEdge || verticalEdge;
    }

    private bool IsPlayerClearOfRoomDoors(Block room)
    {
        foreach (Door door in generator.doors)
        {
            if (false == IsDoorOnRoomBoundary(door, room))
            {
                continue;
            }

            Vector2 doorPosition = new Vector2(
                door.tile.rect.center.x,
                door.tile.rect.center.y
            );

            if (Vector2.Distance(player.transform.position, doorPosition) < SafeDoorDistance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 스타일은 한 번만 만든다. OnGUI는 매 프레임 여러 번 호출되기 때문에
    /// 여기서 new GUIStyle을 하면 계속 새로 만들어진다.
    /// </summary>
    private void EnsureCombatLabelStyle()
    {
        if (null != combatLabelStyle)
        {
            return;
        }

        Font font = Resources.Load<Font>(FontPath);
        if (null == font)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        combatLabelStyle = new GUIStyle();
        combatLabelStyle.font = font;
        combatLabelStyle.fontStyle = FontStyle.Normal;
        combatLabelStyle.alignment = TextAnchor.UpperCenter;
        combatLabelStyle.wordWrap = true;
        combatLabelStyle.clipping = TextClipping.Overflow;
        combatLabelStyle.padding = new RectOffset(4, 4, 10, 10);
    }

    private void OnGUI()
    {
        if (0 > activeRoomIndex || true == IronicRewardUI.IsShowing)
        {
            return;
        }

        // 결과 화면이 떠 있으면 그리지 않는다.
        if (true == GameEndUI.IsShowing)
        {
            return;
        }

        EnsureCombatLabelStyle();

        int livingCount = CountLivingMonsters(activeRoomIndex);
        string text = $"전투 중\n남은 몬스터 {livingCount}";

        // 화면이 커지면 글자도 같이 커진다.
        float scale = Mathf.Max(0.6f, Screen.height / ReferenceHeight);
        combatLabelStyle.fontSize = Mathf.RoundToInt(combatLabelFontSize * scale);

        float textWidth = Mathf.Clamp(Screen.width - 20.0f, 60.0f, 620.0f);
        float textHeight = combatLabelStyle.CalcHeight(new GUIContent(text), textWidth);
        float textX = (Screen.width - textWidth) * 0.5f;

        Rect labelRect = new Rect(textX, 10.0f, textWidth, textHeight);

        // 밝은 바닥 위에서도 읽히도록 어두운 그림자를 먼저 깐다.
        combatLabelStyle.normal.textColor = new Color(0.05f, 0.02f, 0.03f, 0.85f);
        GUI.Label(
            new Rect(labelRect.x + 2.0f, labelRect.y + 2.0f, labelRect.width, labelRect.height),
            text,
            combatLabelStyle);

        combatLabelStyle.normal.textColor = new Color(1.0f, 0.36f, 0.30f, 1.0f);
        GUI.Label(labelRect, text, combatLabelStyle);
    }
}
