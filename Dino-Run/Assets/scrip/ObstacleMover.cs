using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    private float speed = 3.2f;
    private Collider2D obstacleCollider;

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    void Start()
    {
        obstacleCollider = GetComponent<Collider2D>();

        if (obstacleCollider == null)
        {
            obstacleCollider = GetComponentInChildren<Collider2D>();
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            speed = GameManager.Instance.CurrentGameSpeed;
        }

        transform.position += Vector3.left * speed * Time.deltaTime;

        CheckHitDino();

        if (transform.position.x < -12f)
        {
            Destroy(gameObject);
        }
    }

    void CheckHitDino()
    {
        if (DinoController.Instance == null) return;
        if (DinoController.Instance.IsDead) return;
        if (DinoController.Instance.DinoCollider == null) return;
        if (obstacleCollider == null) return;

        if (obstacleCollider.bounds.Intersects(DinoController.Instance.DinoCollider.bounds))
        {
            DinoController.Instance.HitObstacle();
        }
    }
}