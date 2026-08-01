using UnityEngine;

public class ShurikenProjectile : MonoBehaviour
{
    [SerializeField]
    private float speed = 10.0f;

    [SerializeField]
    private float range = 6.0f;

    [SerializeField]
    private float rotationSpeed = 720.0f;

    private Vector2 direction;
    private Vector2 startPosition;

    public void Initialize(Vector2 fireDirection)
    {
        direction = fireDirection.normalized;
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);

        transform.Rotate(
            0.0f,
            0.0f,
            -rotationSpeed * Time.deltaTime
        );

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