using UnityEngine;

public class DoorView : MonoBehaviour
{
    [SerializeField] private float openDistance = 1.25f;
    [SerializeField] private float closeDistance = 1.75f;
    [SerializeField] private float animationSpeed = 4.0f;

    private Door door;
    private SpriteRenderer spriteRenderer;
    private Player player;
    private Vector3 raisedPosition;
    private float raisedAmount;

    public void Init(Door door, SpriteRenderer spriteRenderer, Sprite doorSprite)
    {
        this.door = door;
        this.spriteRenderer = spriteRenderer;
        this.spriteRenderer.sprite = doorSprite;

        raisedPosition = transform.position;
        raisedAmount = Door.State.Open == door.state ? 0.0f : 1.0f;
        ApplyHeight();
    }

    private void Update()
    {
        if (null == door)
        {
            return;
        }

        if (Door.State.Lock != door.state)
        {
            if (null == player)
            {
                player = FindFirstObjectByType<Player>();
            }

            if (null != player)
            {
                float distance =
                    Vector2.Distance(raisedPosition, player.transform.position);

                if (Door.State.Close == door.state && distance <= openDistance)
                {
                    door.Open();
                }
                else if (Door.State.Open == door.state && distance >= closeDistance)
                {
                    door.Close();
                }
            }
        }

        float targetAmount = Door.State.Open == door.state ? 0.0f : 1.0f;
        raisedAmount = Mathf.MoveTowards(
            raisedAmount,
            targetAmount,
            animationSpeed * Time.deltaTime
        );

        ApplyHeight();
    }

    private void ApplyHeight()
    {
        if (null == spriteRenderer)
        {
            return;
        }

        Vector3 scale = Vector3.one;
        scale.y = raisedAmount;
        transform.localScale = scale;

        transform.position =
            raisedPosition + Vector3.down * ((1.0f - raisedAmount) * 0.5f);

        spriteRenderer.enabled = 0.01f < raisedAmount;
    }
}
