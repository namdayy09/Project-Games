using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public GameObject cactusPrefab;
    public GameObject birdPrefab;
    public Transform spawnPoint;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI restartHintText;

    [Header("Score")]
    public int score = 0;
    public int highScore = 0;

    private bool isGameOver = false;
    private float spawnTimer = 0f;
    private float scoreTimer = 0f;
    private float groundY;

    private const float StartSpeed = 3.2f;
    private const float MaxSpeed = 8.5f;

    private const float StartSpawnInterval = 2.2f;
    private const float MinSpawnInterval = 1.15f;

    private const int BirdUnlockScore = 700;

    private const float GroundFallbackY = -2.65f;

    private const float CactusBushSpacing = 1.15f;
    private const float CactusInBushSpacing = 0.42f;

    private const float BirdLowHeight = 1.05f;
    private const float BirdHighHeight = 1.55f;

    public float CurrentGameSpeed { get; private set; } = StartSpeed;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        groundY = GetGroundY();
        CurrentGameSpeed = StartSpeed;

        highScore = PlayerPrefs.GetInt("HighScore", 0);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateScoreUI();
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }

            return;
        }

        UpdateScore();
        UpdateDifficulty();
        HandleSpawn();
    }

    void UpdateScore()
    {
        scoreTimer += Time.deltaTime * 10f;
        score = Mathf.FloorToInt(scoreTimer);

        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (highScoreText != null)
        {
            highScoreText.text = "Best: " + highScore;
        }
    }

    void UpdateDifficulty()
    {
        float t = Mathf.Clamp01(score / 2500f);
        CurrentGameSpeed = Mathf.Lerp(StartSpeed, MaxSpeed, t);
    }

    void HandleSpawn()
    {
        spawnTimer += Time.deltaTime;

        float difficulty = Mathf.Clamp01(score / 2500f);
        float currentSpawnInterval = Mathf.Lerp(StartSpawnInterval, MinSpawnInterval, difficulty);

        if (spawnTimer >= currentSpawnInterval)
        {
            SpawnObstacleWave();
            spawnTimer = 0f;
        }
    }

    void SpawnObstacleWave()
    {
        if (cactusPrefab == null || birdPrefab == null || spawnPoint == null)
        {
            Debug.LogError("GameManager thiếu cactusPrefab, birdPrefab hoặc spawnPoint!");
            return;
        }

        if (score < BirdUnlockScore)
        {
            SpawnCactusWave();
            return;
        }

        if (Random.value < 0.75f)
        {
            SpawnCactusWave();
        }
        else
        {
            SpawnBird();
        }
    }

    void SpawnCactusWave()
    {
        int maxBushCount = 1;

        if (score >= 500) maxBushCount = 2;
        if (score >= 1200) maxBushCount = 3;

        int bushCount = Random.Range(1, maxBushCount + 1);

        for (int bushIndex = 0; bushIndex < bushCount; bushIndex++)
        {
            float bushX = spawnPoint.position.x + bushIndex * CactusBushSpacing;
            SpawnSingleCactusBush(bushX);
        }
    }

    void SpawnSingleCactusBush(float startX)
    {
        int cactusCount = Random.Range(1, 4);

        for (int i = 0; i < cactusCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                startX + i * CactusInBushSpacing,
                groundY,
                0f
            );

            GameObject cactus = Instantiate(cactusPrefab, spawnPos, Quaternion.identity);

            float scale = GetRandomCactusScale(cactusCount, i);
            cactus.transform.localScale = new Vector3(scale, scale, 1f);

            AlignBottomToGround(cactus, groundY);
            SetupObstacle(cactus);
        }
    }

    float GetRandomCactusScale(int cactusCount, int index)
    {
        if (cactusCount == 1)
        {
            return Random.Range(1.15f, 1.45f);
        }

        if (cactusCount == 2)
        {
            if (index == 0) return Random.Range(1.2f, 1.45f);
            return Random.Range(0.85f, 1.1f);
        }

        if (index == 0) return Random.Range(1.2f, 1.45f);
        if (index == 1) return Random.Range(0.8f, 1.0f);
        return Random.Range(1.0f, 1.25f);
    }

    void SpawnBird()
    {
        Vector3 spawnPos = spawnPoint.position;

        bool highBird = Random.value > 0.5f;
        spawnPos.y = groundY + (highBird ? BirdHighHeight : BirdLowHeight);
        spawnPos.z = 0f;

        GameObject bird = Instantiate(birdPrefab, spawnPos, Quaternion.identity);
        bird.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

        SetupObstacle(bird);
    }

    void SetupObstacle(GameObject obstacle)
    {
        obstacle.tag = "Obstacle";

        EnsureObstacleCollider(obstacle);

        ObstacleMover mover = obstacle.GetComponent<ObstacleMover>();

        if (mover == null)
        {
            mover = obstacle.AddComponent<ObstacleMover>();
        }

        mover.SetSpeed(CurrentGameSpeed);
    }
    void EnsureObstacleCollider(GameObject obstacle)
    {
        BoxCollider2D box = obstacle.GetComponent<BoxCollider2D>();

        if (box == null)
        {
            box = obstacle.AddComponent<BoxCollider2D>();
        }

        // Không cần Trigger nữa vì ObstacleMover tự check bounds
        box.isTrigger = false;

        SpriteRenderer sr = obstacle.GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            sr = obstacle.GetComponentInChildren<SpriteRenderer>();
        }

        if (sr != null && sr.sprite != null)
        {
            Vector2 spriteSize = sr.sprite.bounds.size;
            Vector2 spriteCenter = sr.sprite.bounds.center;

            box.size = new Vector2(spriteSize.x * 0.8f, spriteSize.y * 0.85f);
            box.offset = spriteCenter;
        }
        else
        {
            box.size = new Vector2(0.6f, 0.8f);
            box.offset = Vector2.zero;
        }
    }

    void AlignBottomToGround(GameObject obj, float targetGroundY)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            sr = obj.GetComponentInChildren<SpriteRenderer>();
        }

        if (sr == null) return;

        float bottomY = sr.bounds.min.y;
        float offsetY = targetGroundY - bottomY;

        obj.transform.position += new Vector3(0f, offsetY, 0f);
    }

    float GetGroundY()
    {
        GameObject ground = GameObject.Find("Ground");

        if (ground != null)
        {
            SpriteRenderer sr = ground.GetComponent<SpriteRenderer>();

            if (sr == null)
            {
                sr = ground.GetComponentInChildren<SpriteRenderer>();
            }

            if (sr != null)
            {
                return sr.bounds.max.y;
            }
        }

        return GroundFallbackY;
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        UpdateScoreUI();
        ShowGameOverUI();

        Debug.Log("Game Over! Score: " + score + " | Best: " + highScore);
    }

    void ShowGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = "Score: " + score + "  |  Best: " + highScore;
        }

        if (restartHintText != null)
        {
            restartHintText.text = "Press R to Restart";
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }
}