using UnityEngine;

public class Boss : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float detectRange = 8.0f;
    public float stopRange = 1.2f;
    public float radius = 0.4f;

    private TileMap tileMap;
    private Player player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public void Init(TileMap tileMap)
    {
        this.tileMap = tileMap;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (null == tileMap)
        {
            return;
        }

        if (null == player)
        {
            player = FindAnyObjectByType<Player>();
        }

        if (null == player)
        {
            return;
        }

        Vector2 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        if (distance > detectRange || distance <= stopRange)
        {
            SetMoving(false);
            return;
        }

        SetMoving(true);

        Vector2 direction = toPlayer.normalized;

        if (null != spriteRenderer && 0.01f < Mathf.Abs(direction.x))
        {
            spriteRenderer.flipX = (direction.x < 0.0f);
        }

        float moveDistance = moveSpeed * Time.deltaTime;
        Vector2 position = transform.position;

        Vector2 movedX = new Vector2(position.x + direction.x * moveDistance, position.y);
        if (true == CanMove(movedX))
        {
            position = movedX;
        }

        Vector2 movedY = new Vector2(position.x, position.y + direction.y * moveDistance);
        if (true == CanMove(movedY))
        {
            position = movedY;
        }

        transform.position = new Vector3(position.x, position.y, 0.0f);
    }

    private void SetMoving(bool moving)
    {
        if (null == animator)
        {
            return;
        }

        animator.SetBool("IsMoving", moving);
    }

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
}
