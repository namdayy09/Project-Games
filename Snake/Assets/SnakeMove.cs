using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SnakeController : MonoBehaviour
{
    public float moveInterval = 0.2f;
    private float moveTimer = 0f;

    public Vector2 direction = Vector2.right;

    public Transform segmentPrefab;
    public BoxCollider2D gridArea;

    private List<Transform> segments = new List<Transform>();

    // --- Điểm số ---
    public TextMeshProUGUI scoreText;
    private int score = 0;

    void Start()
    {
        segments.Add(this.transform);
        score = 0;
        UpdateScoreUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && direction != Vector2.down) direction = Vector2.up;
        if (Input.GetKeyDown(KeyCode.S) && direction != Vector2.up) direction = Vector2.down;
        if (Input.GetKeyDown(KeyCode.A) && direction != Vector2.right) direction = Vector2.left;
        if (Input.GetKeyDown(KeyCode.D) && direction != Vector2.left) direction = Vector2.right;
    }

    void FixedUpdate()
    {
        moveTimer += Time.fixedDeltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer = 0f;

            for (int i = segments.Count - 1; i > 0; i--)
            {
                segments[i].position = segments[i - 1].position;
            }

            transform.position = new Vector3(
                transform.position.x + direction.x,
                transform.position.y + direction.y,
                0.0f
            );

            // Kiểm tra rắn cắn chính nó
            for (int i = 1; i < segments.Count; i++)
            {
                if (transform.position == segments[i].position)
                {
                    GameOver();
                }
            }

            // Kiểm tra rắn ra ngoài màn hình
            Bounds bounds = gridArea.bounds;
            if (transform.position.x < bounds.min.x || transform.position.x > bounds.max.x ||
                transform.position.y < bounds.min.y || transform.position.y > bounds.max.y)
            {
                GameOver();
            }
        }
    }

    public void Grow()
    {
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position;
        segments.Add(segment);

        score++;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over! Final Score: " + score);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}