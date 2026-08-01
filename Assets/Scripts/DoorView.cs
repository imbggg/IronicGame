using UnityEngine;

public class DoorView : MonoBehaviour
{
    [SerializeField] private float openDistance = 1.25f;
    [SerializeField] private float closeDistance = 1.75f;
    [SerializeField] private float animationSpeed = 6.0f;

    private Door door;
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames;
    private Player player;

    private float raisedAmount;

    public void Init(Door door, SpriteRenderer spriteRenderer, Sprite[] frames)
    {
        this.door = door;
        this.spriteRenderer = spriteRenderer;
        this.frames = frames;

        raisedAmount = Door.State.Open == door.state ? 0.0f : 1.0f;
        ApplyFrame();
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
                player = FindAnyObjectByType<Player>();
            }

            if (null != player)
            {
                float distance = Vector2.Distance(transform.position, player.transform.position);

                if (Door.State.Close == door.state
                    && distance <= openDistance
                    && true == Input.GetKeyDown(KeyCode.E))
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
        raisedAmount = Mathf.MoveTowards(raisedAmount, targetAmount, animationSpeed * Time.deltaTime);

        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (null == spriteRenderer || null == frames || 0 == frames.Length)
        {
            return;
        }

        int index = Mathf.RoundToInt(raisedAmount * (frames.Length - 1));
        index = Mathf.Clamp(index, 0, frames.Length - 1);

        spriteRenderer.sprite = frames[index];
    }
}
