using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [SerializeField]
    private float speed = 12.0f;

    [SerializeField]
    private float range = 8.0f;

    private Vector2 direction;
    private Vector2 startPosition;

    public void Initialize(Vector2 fireDirection)
    {
        direction = fireDirection.normalized;
        startPosition = transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x)
            * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(
            0.0f,
            0.0f,
            angle
        );
    }

    private void Update()
    {
        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);

        float traveledDistance =
            Vector2.Distance(startPosition, transform.position);

        if (traveledDistance >= range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}