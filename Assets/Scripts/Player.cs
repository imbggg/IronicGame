using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 6.0f;
    public float radius = 0.3f;

    private TileMap tileMap;

    public void Init(TileMap tileMap, Vector2 startPosition)
    {
        this.tileMap = tileMap;
        transform.position = new Vector3(startPosition.x, startPosition.y, 0.0f);
    }

    private void Update()
    {
        if (null == tileMap)
        {
            return;
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
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

if (null != spriteRenderer)
{
    if (horizontal < 0.0f)
    {
        // 왼쪽 이동
        spriteRenderer.flipX = true;
    }
    else if (horizontal > 0.0f)
    {
        // 오른쪽 이동
        spriteRenderer.flipX = false;
    }
}

        Vector2 direction = new Vector2(horizontal, vertical);
        Animator animator = GetComponent<Animator>();
        if (0.0f == direction.sqrMagnitude)
{
    if (null != animator)
    {
        animator.speed = 0.0f;
    }

    return;
}

if (null != animator)
{
    animator.speed = 0.4f;
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

        if (null != tile.door && Door.State.Open != tile.door.state)
        {
            return false;
        }

        return Tile.Type.Floor == tile.type;
    }
}
