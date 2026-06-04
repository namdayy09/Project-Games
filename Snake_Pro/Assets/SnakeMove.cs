using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeController : MonoBehaviour
{
    [Header("Core References")]
    public Transform segmentPrefab;
    public BoxCollider2D gridArea;

    [Header("Game Settings")]
    public int startLength = 3;
    public float baseMoveInterval = 0.22f;
    public int normalFoodScore = 10;
    public int scorePerLevel = 50;
    public bool soundOn = true;
    public Difficulty difficulty = Difficulty.Normal;

    private readonly List<Transform> segments = new List<Transform>();
    private readonly List<Vector2Int> obstacles = new List<Vector2Int>();
    private Vector2Int direction = Vector2Int.right;
    private Vector2Int nextDirection = Vector2Int.right;
    private float moveTimer;
    private float currentMoveInterval;
    private int score;
    private int level = 1;
    private bool isPlaying;
    private bool isPaused;
    private bool isGameOver;
    private FoodSpawner food;
    private Camera mainCamera;
    private Canvas rootCanvas;

    private const string HighScoreKey1 = "SNAKE_TOP_1";
    private const string HighScoreKey2 = "SNAKE_TOP_2";
    private const string HighScoreKey3 = "SNAKE_TOP_3";
    private const string SoundKey = "SNAKE_SOUND";
    private const string DifficultyKey = "SNAKE_DIFFICULTY";
    private const string SkinKey = "SNAKE_SKIN";

    private GameObject mainMenuPanel;
    private GameObject hudPanel;
    private GameObject pausePanel;
    private GameObject gameOverPanel;
    private GameObject highScorePanel;
    private GameObject settingsPanel;
    private GameObject skinPanel;
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI topPauseText;
    private TextMeshProUGUI pauseInfoText;
    private TextMeshProUGUI gameOverInfoText;
    private TextMeshProUGUI gameOverTopText;
    private TextMeshProUGUI highScoreTopText;
    private Toggle soundToggle;
    private TextMeshProUGUI difficultyValueText;
    private TextMeshProUGUI skinStatusText;
    private int selectedSkin = 0;

    private AudioSource audioSource;
    private AudioClip clickClip;
    private AudioClip eatClip;
    private AudioClip gameOverClip;
    private AudioClip lockedClip;

    private Vector2 touchStart;
    private bool hasTouchStart;
    private readonly Dictionary<Vector2Int, GameObject> obstacleObjects = new Dictionary<Vector2Int, GameObject>();
    private Sprite squareSprite;
    private Sprite circleSprite;
    private Sprite foodSprite;

    // Modern minimal premium palette: tối, rõ bố cục, ít màu nhưng có điểm nhấn.
    private readonly Color premiumBgTop = new Color(0.04f, 0.07f, 0.13f, 1f);
    private readonly Color premiumBgBottom = new Color(0.01f, 0.02f, 0.05f, 1f);
    private readonly Color premiumBoardA = new Color(0.07f, 0.11f, 0.19f, 1f);
    private readonly Color premiumBoardB = new Color(0.03f, 0.06f, 0.11f, 1f);
    private readonly Color premiumPanel = new Color(0.07f, 0.10f, 0.17f, 0.96f);
    private readonly Color premiumPanelSoft = new Color(0.10f, 0.14f, 0.23f, 0.92f);
    private readonly Color premiumAccent = new Color(0.22f, 0.74f, 0.96f, 1f);
    private readonly Color premiumAccent2 = new Color(0.20f, 0.84f, 0.52f, 1f);
    private readonly Color premiumText = new Color(0.96f, 0.98f, 1f, 1f);
    private readonly Color premiumMuted = new Color(0.58f, 0.66f, 0.76f, 1f);

    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    private void Awake()
    {
        mainCamera = Camera.main;
        if (gridArea == null)
        {
            GameObject area = GameObject.Find("SpawnArea");
            if (area != null) gridArea = area.GetComponent<BoxCollider2D>();
        }

        food = FindFirstObjectByType<FoodSpawner>();
        squareSprite = CreateSquareSprite(64, new Color(1f, 1f, 1f, 1f));
        circleSprite = CreateCircleSprite(96, new Color(1f, 1f, 1f, 1f));
        foodSprite = CreateAppleSprite();
        SetupRuntimeAudio();

        LoadSettings();
        SetupCameraAndScene();
        BuildMobileCanvas();
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void Update()
    {
        HandleKeyboardInput();
        HandleSwipeInput();

        if (!isPlaying || isPaused || isGameOver) return;

        moveTimer += Time.deltaTime;
        if (moveTimer >= currentMoveInterval)
        {
            moveTimer = 0f;
            MoveSnake();
        }
    }

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPlaying && !isGameOver)
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }

        if (!isPlaying || isPaused || isGameOver) return;

        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && direction != Vector2Int.down) nextDirection = Vector2Int.up;
        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && direction != Vector2Int.up) nextDirection = Vector2Int.down;
        if ((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) && direction != Vector2Int.right) nextDirection = Vector2Int.left;
        if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) && direction != Vector2Int.left) nextDirection = Vector2Int.right;
    }

    private void HandleSwipeInput()
    {
        if (!isPlaying || isPaused || isGameOver) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
                hasTouchStart = true;
            }
            else if (touch.phase == TouchPhase.Ended && hasTouchStart)
            {
                ApplySwipe(touch.position - touchStart);
                hasTouchStart = false;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            touchStart = Input.mousePosition;
            hasTouchStart = true;
        }
        else if (Input.GetMouseButtonUp(0) && hasTouchStart)
        {
            ApplySwipe((Vector2)Input.mousePosition - touchStart);
            hasTouchStart = false;
        }
    }

    private void ApplySwipe(Vector2 delta)
    {
        if (delta.magnitude < 60f) return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x > 0 && direction != Vector2Int.left) nextDirection = Vector2Int.right;
            else if (delta.x < 0 && direction != Vector2Int.right) nextDirection = Vector2Int.left;
        }
        else
        {
            if (delta.y > 0 && direction != Vector2Int.down) nextDirection = Vector2Int.up;
            else if (delta.y < 0 && direction != Vector2Int.up) nextDirection = Vector2Int.down;
        }
    }

    private void MoveSnake()
    {
        direction = nextDirection;
        RotateHeadToDirection();
        Vector3 oldTailPosition = segments[segments.Count - 1].position;

        for (int i = segments.Count - 1; i > 0; i--)
        {
            segments[i].position = segments[i - 1].position;
        }

        Vector2Int headGrid = WorldToGrid(segments[0].position) + direction;
        segments[0].position = GridToWorld(headGrid);

        if (HitWall(headGrid) || HitSelf(headGrid) || obstacles.Contains(headGrid))
        {
            GameOver();
            return;
        }

        if (food != null && headGrid == WorldToGrid(food.transform.position))
        {
            EatFood(food.currentType, oldTailPosition);
            SpawnFood();
        }
    }

    private void EatFood(FoodSpawner.FoodType type, Vector3 tailPosition)
    {
        Grow(tailPosition);

        switch (type)
        {
            case FoodSpawner.FoodType.Normal:
                AddScore(normalFoodScore);
                break;
            case FoodSpawner.FoodType.SpeedBoost:
                AddScore(15);
                currentMoveInterval = Mathf.Max(0.07f, currentMoveInterval * 0.78f);
                break;
            case FoodSpawner.FoodType.Slow:
                AddScore(8);
                currentMoveInterval = Mathf.Min(baseMoveInterval * 1.35f, currentMoveInterval * 1.18f);
                break;
        }

        AnimatePulse(segments[0]);
        PlayClip(eatClip);
        UpdateDynamicDifficulty();
        UpdateUI();
    }

    private void AddScore(int amount)
    {
        score += amount;
        level = Mathf.Max(1, score / scorePerLevel + 1);
    }

    private void Grow(Vector3 position)
    {
        Transform segment;
        if (segmentPrefab != null)
        {
            segment = Instantiate(segmentPrefab, position, Quaternion.identity);
        }
        else
        {
            GameObject obj = new GameObject("SnakeSegment");
            segment = obj.transform;
            segment.position = position;
        }

        PrepareSnakeSegment(segment, false, segments.Count);
        segments.Add(segment);
    }

    private void PrepareSnakeSegment(Transform segment, bool isHead, int index)
    {
        segment.name = isHead ? "SnakeHead" : "SnakeSegment_" + index;
        segment.localScale = Vector3.one * (isHead ? 0.92f : 0.84f);

        SpriteRenderer sr = segment.GetComponent<SpriteRenderer>();
        if (sr == null) sr = segment.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.sortingOrder = isHead ? 9 : 7;

        Color headColor, bodyA, bodyB, shine;
        GetSkinPalette(selectedSkin, out headColor, out bodyA, out bodyB, out shine);
        sr.color = isHead ? headColor : Color.Lerp(bodyA, bodyB, Mathf.PingPong(index * 0.22f, 1f));

        Collider2D col = segment.GetComponent<Collider2D>();
        if (col == null) col = segment.gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;

        RebuildSegmentDetails(segment, isHead, index, shine);

        if (isHead)
        {
            segment.tag = "Player";
            RebuildHeadFace(segment);
            RotateHeadToDirection();
        }
    }

    private void GetSkinPalette(int skin, out Color head, out Color bodyA, out Color bodyB, out Color shine)
    {
        // 0: Emerald Premium, 1: Arctic Blue. Skin 2 là bí ẩn và bị khóa.
        if (skin == 1)
        {
            head = new Color(0.54f, 0.90f, 1f, 1f);
            bodyA = new Color(0.10f, 0.42f, 0.88f, 1f);
            bodyB = new Color(0.48f, 0.78f, 1f, 1f);
            shine = new Color(0.90f, 0.98f, 1f, 0.48f);
            return;
        }

        head = new Color(0.30f, 0.96f, 0.62f, 1f);
        bodyA = new Color(0.10f, 0.58f, 0.34f, 1f);
        bodyB = new Color(0.43f, 0.96f, 0.58f, 1f);
        shine = new Color(0.88f, 1f, 0.92f, 0.44f);
    }

    private string GetSkinName(int skin)
    {
        if (skin == 1) return "ARCTIC BLUE";
        if (skin == 2) return "MYSTERY";
        return "EMERALD";
    }

    private void RebuildSegmentDetails(Transform segment, bool isHead, int index, Color shine)
    {
        for (int i = segment.childCount - 1; i >= 0; i--)
        {
            Transform child = segment.GetChild(i);
            if (child.name.StartsWith("BodyDetail_")) Destroy(child.gameObject);
        }

        if (isHead) return;

        GameObject gloss = new GameObject("BodyDetail_Gloss");
        gloss.transform.SetParent(segment, false);
        gloss.transform.localPosition = new Vector3(-0.12f, 0.16f, -0.04f);
        gloss.transform.localScale = new Vector3(0.26f, 0.12f, 1f);
        SpriteRenderer sr = gloss.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = shine;
        sr.sortingOrder = 8;
    }

    private void RebuildHeadFace(Transform head)
    {
        // Xóa mắt/mũi cũ để mỗi lần restart không bị nhân đôi chi tiết trên đầu rắn.
        for (int i = head.childCount - 1; i >= 0; i--)
        {
            Transform child = head.GetChild(i);
            if (child.name.StartsWith("HeadFace_")) Destroy(child.gameObject);
        }

        CreateFaceDot(head, "HeadFace_LeftEye", new Vector3(0.22f, 0.17f, -0.03f), 0.115f, new Color(0.02f, 0.03f, 0.05f, 1f), 12);
        CreateFaceDot(head, "HeadFace_RightEye", new Vector3(0.22f, -0.17f, -0.03f), 0.115f, new Color(0.02f, 0.03f, 0.05f, 1f), 12);
        CreateFaceDot(head, "HeadFace_Nose", new Vector3(0.39f, 0.00f, -0.035f), 0.075f, new Color(0.03f, 0.06f, 0.07f, 1f), 13);
        CreateFaceDot(head, "HeadFace_Shine", new Vector3(-0.13f, 0.20f, -0.04f), 0.11f, new Color(1f, 1f, 1f, 0.50f), 14);
    }

    private void CreateFaceDot(Transform parent, string name, Vector3 localPos, float scale, Color color, int sortingOrder)
    {
        GameObject dot = new GameObject(name);
        dot.transform.SetParent(parent, false);
        dot.transform.localPosition = localPos;
        dot.transform.localScale = Vector3.one * scale;
        SpriteRenderer sr = dot.AddComponent<SpriteRenderer>();
        sr.sprite = circleSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private void RotateHeadToDirection()
    {
        if (segments.Count == 0 || segments[0] == null) return;

        float z = 0f;
        if (direction == Vector2Int.right) z = 0f;
        else if (direction == Vector2Int.up) z = 90f;
        else if (direction == Vector2Int.left) z = 180f;
        else if (direction == Vector2Int.down) z = -90f;

        segments[0].rotation = Quaternion.Euler(0f, 0f, z);
    }

    private void ResetSnake()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null) Destroy(segments[i].gameObject);
        }
        segments.Clear();
        segments.Add(transform);
        PrepareSnakeSegment(transform, true, 0);

        Vector2Int center = GetGridCenter();
        transform.position = GridToWorld(center);
        direction = Vector2Int.right;
        nextDirection = Vector2Int.right;
        RotateHeadToDirection();

        for (int i = 1; i < startLength; i++)
        {
            Grow(GridToWorld(center - new Vector2Int(i, 0)));
        }
    }

    private void UpdateDynamicDifficulty()
    {
        float difficultyBonus = difficulty == Difficulty.Easy ? 0.02f : difficulty == Difficulty.Normal ? 0.03f : 0.045f;
        currentMoveInterval = Mathf.Max(0.06f, baseMoveInterval - ((level - 1) * difficultyBonus));

        int targetObstacleCount = GetTargetObstacleCount();
        while (obstacles.Count < targetObstacleCount) AddObstacle();
    }

    private int GetTargetObstacleCount()
    {
        int baseCount = difficulty == Difficulty.Easy ? 0 : difficulty == Difficulty.Normal ? 1 : 2;
        int add = Mathf.Clamp(level - 1, 0, 10);
        return Mathf.Clamp(baseCount + add, 0, difficulty == Difficulty.Easy ? 5 : difficulty == Difficulty.Normal ? 9 : 14);
    }

    private void ClearObstacles()
    {
        foreach (GameObject obj in obstacleObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        obstacleObjects.Clear();
        obstacles.Clear();
    }

    private void AddObstacle()
    {
        Vector2Int pos;
        if (!TryGetFreeCell(out pos)) return;
        obstacles.Add(pos);

        GameObject obj = new GameObject("Obstacle");
        obj.transform.position = GridToWorld(pos);
        obj.transform.localScale = Vector3.one * 0.92f;
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.20f, 0.25f, 0.34f, 1f);
        sr.sortingOrder = 4;
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        obstacleObjects[pos] = obj;
    }

    private void SpawnFood()
    {
        if (food == null) return;
        Vector2Int pos;
        if (!TryGetFreeCell(out pos)) return;
        food.transform.position = GridToWorld(pos);
        food.SetRandomType(foodSprite, circleSprite);
        AnimatePulse(food.transform);
    }

    private bool TryGetFreeCell(out Vector2Int cell)
    {
        Bounds b = gridArea.bounds;
        int minX = Mathf.CeilToInt(b.min.x) + 1;
        int maxX = Mathf.FloorToInt(b.max.x) - 1;
        int minY = Mathf.CeilToInt(b.min.y) + 1;
        int maxY = Mathf.FloorToInt(b.max.y) - 1;

        for (int i = 0; i < 300; i++)
        {
            cell = new Vector2Int(Random.Range(minX, maxX + 1), Random.Range(minY, maxY + 1));
            if (IsCellFree(cell)) return true;
        }

        cell = Vector2Int.zero;
        return false;
    }

    private bool IsCellFree(Vector2Int cell)
    {
        if (HitWall(cell)) return false;
        if (obstacles.Contains(cell)) return false;
        if (food != null && cell == WorldToGrid(food.transform.position)) return false;
        foreach (Transform seg in segments)
        {
            if (seg != null && WorldToGrid(seg.position) == cell) return false;
        }
        return true;
    }

    private bool HitWall(Vector2Int cell)
    {
        Bounds b = gridArea.bounds;
        return cell.x <= Mathf.FloorToInt(b.min.x) || cell.x >= Mathf.CeilToInt(b.max.x) || cell.y <= Mathf.FloorToInt(b.min.y) || cell.y >= Mathf.CeilToInt(b.max.y);
    }

    private bool HitSelf(Vector2Int headCell)
    {
        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i] != null && WorldToGrid(segments[i].position) == headCell) return true;
        }
        return false;
    }

    private Vector2Int WorldToGrid(Vector3 pos)
    {
        return new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
    }

    private Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x, cell.y, 0f);
    }

    private Vector2Int GetGridCenter()
    {
        if (gridArea == null) return Vector2Int.zero;
        Bounds b = gridArea.bounds;
        return new Vector2Int(Mathf.RoundToInt(b.center.x), Mathf.RoundToInt(b.center.y));
    }

    public void StartGame()
    {
        score = 0;
        level = 1;
        isPlaying = true;
        isPaused = false;
        isGameOver = false;
        moveTimer = 0f;
        currentMoveInterval = baseMoveInterval;
        ClearObstacles();
        ResetSnake();
        UpdateDynamicDifficulty();
        SpawnFood();
        ShowOnly(hudPanel);
        UpdateUI();
    }

    public void PauseGame()
    {
        if (!isPlaying || isGameOver) return;
        isPaused = true;
        pausePanel.SetActive(true);
        UpdateMenuTexts("Tạm dừng");
        AnimatePanel(pausePanel.transform);
    }

    public void ResumeGame()
    {
        if (!isPlaying || isGameOver) return;
        isPaused = false;
        pausePanel.SetActive(false);
    }

    public void RestartGame()
    {
        StartGame();
    }

    public void BackToMainMenu()
    {
        isPlaying = false;
        isPaused = false;
        isGameOver = false;
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        ShowOnly(mainMenuPanel);
        UpdateMenuTexts("Menu chính");
    }

    public void ShowHighScore()
    {
        highScorePanel.SetActive(true);
        UpdateMenuTexts("Điểm cao");
        AnimatePanel(highScorePanel.transform);
    }

    public void CloseHighScore()
    {
        highScorePanel.SetActive(false);
    }

    public void ShowSettings()
    {
        settingsPanel.SetActive(true);
        if (soundToggle != null) soundToggle.isOn = soundOn;
        UpdateDifficultyValueText();
        AnimatePanel(settingsPanel.transform);
    }

    public void CloseSettings()
    {
        if (soundToggle != null) soundOn = soundToggle.isOn;
        SaveSettings();
        settingsPanel.SetActive(false);
    }

    public void ShowSkinMenu()
    {
        skinPanel.SetActive(true);
        if (skinStatusText != null) skinStatusText.text = "Đang dùng: " + GetSkinName(selectedSkin);
        AnimatePanel(skinPanel.transform);
    }

    public void CloseSkinMenu()
    {
        skinPanel.SetActive(false);
    }

    public void SelectClassicSkin()
    {
        ApplySkin(0);
    }

    public void SelectArcticSkin()
    {
        ApplySkin(1);
    }

    private void ApplySkin(int skin)
    {
        selectedSkin = Mathf.Clamp(skin, 0, 1);
        SaveSettings();
        if (skinStatusText != null) skinStatusText.text = "Đang dùng: " + GetSkinName(selectedSkin) + " ✓";
        if (segments.Count > 0)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i] != null) PrepareSnakeSegment(segments[i], i == 0, i);
            }
        }
    }

    public void ShowFutureSkinMessage()
    {
        if (skinStatusText != null) skinStatusText.text = "MYSTERY đang khóa — cần mở bằng nhiệm vụ sau.";
        PlayClip(lockedClip);
    }

    public void QuitGame()
    {
        Debug.Log("Thoát game");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ClearHighScore()
    {
        PlayerPrefs.DeleteKey(HighScoreKey1);
        PlayerPrefs.DeleteKey(HighScoreKey2);
        PlayerPrefs.DeleteKey(HighScoreKey3);
        PlayerPrefs.Save();
        UpdateMenuTexts("Đã xóa điểm cao");
    }

    private void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        isPlaying = false;
        isPaused = false;
        SaveHighScore(score);
        PlayClip(eatClip);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);
        UpdateMenuTexts("Game Over");
        AnimatePanel(gameOverPanel.transform);
    }

    public void SetDifficulty(int value)
    {
        difficulty = (Difficulty)Mathf.Clamp(value, 0, 2);
        UpdateDifficultyValueText();
    }

    private void UpdateDifficultyValueText()
    {
        if (difficultyValueText == null) return;
        if (difficulty == Difficulty.Easy) difficultyValueText.text = "DỄ";
        else if (difficulty == Difficulty.Normal) difficultyValueText.text = "BÌNH THƯỜNG";
        else difficultyValueText.text = "KHÓ";
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "ĐIỂM: " + score;
        if (levelText != null) levelText.text = "LEVEL: " + level;
    }

    private void UpdateMenuTexts(string status)
    {
        int[] top = GetTopScores();
        string topString = "TOP 3 ĐIỂM CAO\n" +
                           "1. " + top[0] + " điểm\n" +
                           "2. " + top[1] + " điểm\n" +
                           "3. " + top[2] + " điểm";

        string info = "Trạng thái: " + status + "\n" +
                      "Điểm hiện tại: " + score + "\n" +
                      "Level: " + level + "\n" +
                      "Độ dài rắn: " + segments.Count + "\n" +
                      "Độ khó: " + difficulty + "\n" +
                      "Điều khiển: Vuốt / WASD / phím mũi tên";

        if (pauseInfoText != null) pauseInfoText.text = info;
        if (topPauseText != null) topPauseText.text = topString;
        if (gameOverInfoText != null) gameOverInfoText.text = "GAME OVER\nĐiểm của bạn: " + score + "\nLevel đạt được: " + level;
        if (gameOverTopText != null) gameOverTopText.text = topString;
        if (highScoreTopText != null) highScoreTopText.text = topString;
    }

    private void SaveHighScore(int newScore)
    {
        List<int> scores = GetTopScores().ToList();
        scores.Add(newScore);
        scores = scores.OrderByDescending(x => x).Take(3).ToList();
        PlayerPrefs.SetInt(HighScoreKey1, scores[0]);
        PlayerPrefs.SetInt(HighScoreKey2, scores.Count > 1 ? scores[1] : 0);
        PlayerPrefs.SetInt(HighScoreKey3, scores.Count > 2 ? scores[2] : 0);
        PlayerPrefs.Save();
    }

    private int[] GetTopScores()
    {
        return new[]
        {
            PlayerPrefs.GetInt(HighScoreKey1, 0),
            PlayerPrefs.GetInt(HighScoreKey2, 0),
            PlayerPrefs.GetInt(HighScoreKey3, 0)
        };
    }

    private void LoadSettings()
    {
        soundOn = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        difficulty = (Difficulty)PlayerPrefs.GetInt(DifficultyKey, 1);
        selectedSkin = Mathf.Clamp(PlayerPrefs.GetInt(SkinKey, 0), 0, 1);
        baseMoveInterval = difficulty == Difficulty.Easy ? 0.26f : difficulty == Difficulty.Normal ? 0.22f : 0.18f;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(SoundKey, soundOn ? 1 : 0);
        PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
        PlayerPrefs.SetInt(SkinKey, selectedSkin);
        PlayerPrefs.Save();
        LoadSettings();
    }

    private void SetupCameraAndScene()
    {
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 9f;
            mainCamera.transform.position = new Vector3(0, 0, -10);
            mainCamera.backgroundColor = premiumBgBottom;
        }

        if (gridArea != null)
        {
            gridArea.size = new Vector2(18, 14);
            gridArea.offset = Vector2.zero;
            gridArea.isTrigger = true;
            CreatePlayFieldVisual(gridArea.bounds);
        }

        if (food != null)
        {
            food.Prepare(foodSprite, circleSprite);
        }
    }

    private void CreatePlayFieldVisual(Bounds b)
    {
        GameObject old = GameObject.Find("Auto_Beautiful_Field");
        if (old != null) Destroy(old);

        GameObject root = new GameObject("Auto_Premium_Dark_Field");
        root.transform.position = Vector3.zero;

        GameObject bg = new GameObject("PremiumDarkBackground");
        bg.transform.SetParent(root.transform);
        bg.transform.position = new Vector3(0, 0, 3f);
        bg.transform.localScale = new Vector3(b.size.x + 8, b.size.y + 6, 1);
        SpriteRenderer bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreateGradientSprite(256, 256, premiumBgTop, premiumBgBottom);
        bgSr.sortingOrder = -30;

        GameObject boardShadow = new GameObject("BoardSoftShadow");
        boardShadow.transform.SetParent(root.transform);
        boardShadow.transform.position = new Vector3(b.center.x, b.center.y - 0.16f, 1.2f);
        boardShadow.transform.localScale = new Vector3(b.size.x + 0.75f, b.size.y + 0.75f, 1f);
        SpriteRenderer shadowSr = boardShadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = squareSprite;
        shadowSr.color = new Color(0f, 0f, 0f, 0.30f);
        shadowSr.sortingOrder = -16;

        GameObject board = new GameObject("MinimalPlayBoard");
        board.transform.SetParent(root.transform);
        board.transform.position = new Vector3(b.center.x, b.center.y, 1f);
        board.transform.localScale = new Vector3(b.size.x, b.size.y, 1f);
        SpriteRenderer boardSr = board.AddComponent<SpriteRenderer>();
        boardSr.sprite = CreateGradientSprite(128, 128, premiumBoardA, premiumBoardB);
        boardSr.sortingOrder = -15;

        for (int x = Mathf.CeilToInt(b.min.x); x <= Mathf.FloorToInt(b.max.x); x++)
        {
            CreateLine(root.transform, "Grid_V", new Vector3(x, b.center.y, 0.9f), new Vector3(0.012f, b.size.y, 1), new Color(1f, 1f, 1f, 0.045f), -12);
        }
        for (int y = Mathf.CeilToInt(b.min.y); y <= Mathf.FloorToInt(b.max.y); y++)
        {
            CreateLine(root.transform, "Grid_H", new Vector3(b.center.x, y, 0.9f), new Vector3(b.size.x, 0.012f, 1), new Color(1f, 1f, 1f, 0.045f), -12);
        }

        Color wall = new Color(0.11f, 0.16f, 0.25f, 1f);
        Color glow = new Color(0.22f, 0.74f, 0.96f, 0.92f);
        float wallThickness = 0.42f;

        CreateLine(root.transform, "Wall_Top", new Vector3(b.center.x, b.max.y, 0.55f), new Vector3(b.size.x + wallThickness, wallThickness, 1), wall, 2);
        CreateLine(root.transform, "Wall_Bottom", new Vector3(b.center.x, b.min.y, 0.55f), new Vector3(b.size.x + wallThickness, wallThickness, 1), wall, 2);
        CreateLine(root.transform, "Wall_Left", new Vector3(b.min.x, b.center.y, 0.55f), new Vector3(wallThickness, b.size.y + wallThickness, 1), wall, 2);
        CreateLine(root.transform, "Wall_Right", new Vector3(b.max.x, b.center.y, 0.55f), new Vector3(wallThickness, b.size.y + wallThickness, 1), wall, 2);

        CreateLine(root.transform, "Wall_Top_Glow", new Vector3(b.center.x, b.max.y - 0.30f, 0.50f), new Vector3(b.size.x - 0.25f, 0.055f, 1), glow, 3);
        CreateLine(root.transform, "Wall_Bottom_Glow", new Vector3(b.center.x, b.min.y + 0.30f, 0.50f), new Vector3(b.size.x - 0.25f, 0.055f, 1), glow, 3);
        CreateLine(root.transform, "Wall_Left_Glow", new Vector3(b.min.x + 0.30f, b.center.y, 0.50f), new Vector3(0.055f, b.size.y - 0.25f, 1), glow, 3);
        CreateLine(root.transform, "Wall_Right_Glow", new Vector3(b.max.x - 0.30f, b.center.y, 0.50f), new Vector3(0.055f, b.size.y - 0.25f, 1), glow, 3);
    }

    private void CreateLine(Transform parent, Vector3 pos, Vector3 scale, Color color)
    {
        CreateLine(parent, "FieldLine", pos, scale, color, -8);
    }

    private void CreateLine(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int sortingOrder)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent);
        line.transform.position = pos;
        line.transform.localScale = scale;
        SpriteRenderer sr = line.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private void BuildMobileCanvas()
    {
        Canvas[] oldCanvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in oldCanvas)
        {
            if (c.name != "SnakeMobileCanvas") c.gameObject.SetActive(false);
        }

        GameObject canvasObj = GameObject.Find("SnakeMobileCanvas");
        if (canvasObj == null) canvasObj = new GameObject("SnakeMobileCanvas");
        rootCanvas = canvasObj.GetComponent<Canvas>();
        if (rootCanvas == null) rootCanvas = canvasObj.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObj.GetComponent<GraphicRaycaster>() == null) canvasObj.AddComponent<GraphicRaycaster>();

        mainMenuPanel = CreateFullPanel("MainMenuPanel", new Color(0.02f, 0.03f, 0.06f, 0.94f));
        hudPanel = CreateFullPanel("HudPanel", Color.clear);
        pausePanel = CreateFullPanel("PausePanel", new Color(0f, 0f, 0f, 0.62f));
        gameOverPanel = CreateFullPanel("GameOverPanel", new Color(0f, 0f, 0f, 0.66f));
        highScorePanel = CreateFullPanel("HighScorePanel", new Color(0f, 0f, 0f, 0.62f));
        settingsPanel = CreateFullPanel("SettingsPanel", new Color(0f, 0f, 0f, 0.66f));
        skinPanel = CreateFullPanel("SkinPanel", new Color(0f, 0f, 0f, 0.68f));

        BuildMainMenu(mainMenuPanel.transform);
        BuildHUD(hudPanel.transform);
        BuildPauseMenu(pausePanel.transform);
        BuildGameOver(gameOverPanel.transform);
        BuildHighScores(highScorePanel.transform);
        BuildSettings(settingsPanel.transform);
        BuildSkinMenu(skinPanel.transform);

        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        highScorePanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (skinPanel != null) skinPanel.SetActive(false);
        skinPanel.SetActive(false);
        hudPanel.SetActive(false);
    }

    private GameObject CreateFullPanel(string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(rootCanvas.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        Stretch(rt);
        Image img = panel.AddComponent<Image>();
        img.color = color;
        return panel;
    }

    private void BuildMainMenu(Transform parent)
    {
        CreateDecorGlow(parent, "GlowTop", new Vector2(0, 640), new Vector2(820, 230), new Color(0.22f, 0.74f, 0.96f, 0.10f));
        CreateDecorGlow(parent, "GlowBottom", new Vector2(0, -570), new Vector2(900, 280), new Color(0.20f, 0.84f, 0.52f, 0.08f));

        TextMeshProUGUI title = CreateTMP(parent, "Title", "SNAKE\nMOBILE", new Vector2(0, 565), TextAlignmentOptions.Center, 86);
        title.color = premiumText;
        title.fontStyle = FontStyles.Bold;

        TextMeshProUGUI sub = CreateTMP(parent, "SubTitle", "LE HOAI NAM • MODERN SNAKE", new Vector2(0, 375), TextAlignmentOptions.Center, 30);
        sub.color = premiumMuted;

        Transform quickCard = CreateCenterBox(parent, "TopQuickCard", new Vector2(650, 105), new Color(0.08f, 0.12f, 0.20f, 0.72f));
        quickCard.localPosition = new Vector3(0, 275, 0);
        TextMeshProUGUI topMini = CreateTMP(quickCard, "TopMini", "BEST  " + GetTopScores()[0] + "     TOP 3  " + GetTopScores()[0] + " • " + GetTopScores()[1] + " • " + GetTopScores()[2], Vector2.zero, TextAlignmentOptions.Center, 28);
        topMini.color = premiumAccent;
        topMini.fontStyle = FontStyles.Bold;

        CreateButton(parent, "BtnPlay", "CHƠI NGAY", new Vector2(0, 135), new Vector2(620, 96), premiumAccent2).onClick.AddListener(StartGame);
        CreateButton(parent, "BtnHighScore", "ĐIỂM CAO", new Vector2(0, 18), new Vector2(620, 86), premiumPanelSoft).onClick.AddListener(ShowHighScore);
        CreateButton(parent, "BtnSkin", "SKIN RẮN", new Vector2(0, -92), new Vector2(620, 86), premiumPanelSoft).onClick.AddListener(ShowSkinMenu);
        CreateButton(parent, "BtnSettings", "CÀI ĐẶT", new Vector2(0, -202), new Vector2(620, 86), premiumPanelSoft).onClick.AddListener(ShowSettings);
        CreateButton(parent, "BtnQuit", "THOÁT", new Vector2(0, -312), new Vector2(620, 86), new Color(0.20f, 0.24f, 0.32f, 0.95f)).onClick.AddListener(QuitGame);

        TextMeshProUGUI hint = CreateTMP(parent, "Hint", "Vuốt để đổi hướng • WASD / mũi tên để test", new Vector2(0, -515), TextAlignmentOptions.Center, 28);
        hint.color = premiumMuted;
    }

    private void BuildHUD(Transform parent)
    {
        scoreText = CreateTMP(parent, "TxtScore", "ĐIỂM: 0", new Vector2(44, -42), TextAlignmentOptions.Left, 34);
        SetAnchor(scoreText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        scoreText.rectTransform.sizeDelta = new Vector2(310, 80);
        scoreText.color = Color.white;
        scoreText.fontStyle = FontStyles.Bold;

        levelText = CreateTMP(parent, "TxtLevel", "LEVEL: 1", new Vector2(0, -42), TextAlignmentOptions.Center, 34);
        SetAnchor(levelText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        levelText.rectTransform.sizeDelta = new Vector2(310, 80);
        levelText.color = new Color(1f, 0.90f, 0.35f);
        levelText.fontStyle = FontStyles.Bold;

        CreateButton(parent, "BtnPause", "DỪNG", new Vector2(-44, -42), new Vector2(170, 70), new Color(0.05f, 0.19f, 0.20f, 0.90f), TextAlignmentOptions.Center, 30, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1)).onClick.AddListener(PauseGame);
    }

    private void BuildPauseMenu(Transform parent)
    {
        Transform box = CreateCenterBox(parent, "PauseBox", new Vector2(790, 970), premiumPanel);
        TextMeshProUGUI title = CreateTMP(box, "PauseTitle", "TẠM DỪNG", new Vector2(0, 390), TextAlignmentOptions.Center, 58);
        title.color = new Color(0.80f, 1f, 0.90f);
        title.fontStyle = FontStyles.Bold;

        pauseInfoText = CreateTMP(box, "PauseInfo", "", new Vector2(-175, 215), TextAlignmentOptions.Left, 29);
        pauseInfoText.rectTransform.sizeDelta = new Vector2(370, 260);
        pauseInfoText.color = Color.white;

        topPauseText = CreateTMP(box, "PauseTop3", "", new Vector2(205, 215), TextAlignmentOptions.Left, 30);
        topPauseText.rectTransform.sizeDelta = new Vector2(330, 260);
        topPauseText.color = new Color(1f, 0.91f, 0.38f);
        topPauseText.fontStyle = FontStyles.Bold;

        CreateButton(box, "BtnResume", "TIẾP TỤC", new Vector2(0, -40), new Vector2(560, 82), new Color(0.10f, 0.73f, 0.36f, 1f)).onClick.AddListener(ResumeGame);
        CreateButton(box, "BtnRestart", "CHƠI LẠI", new Vector2(0, -145), new Vector2(560, 82), new Color(0.13f, 0.50f, 0.95f, 1f)).onClick.AddListener(RestartGame);
        CreateButton(box, "BtnSettingsPause", "CÀI ĐẶT", new Vector2(0, -250), new Vector2(560, 82), new Color(0.58f, 0.38f, 0.92f, 1f)).onClick.AddListener(ShowSettings);
        CreateButton(box, "BtnMenu", "MENU CHÍNH", new Vector2(0, -355), new Vector2(560, 82), new Color(0.88f, 0.25f, 0.22f, 1f)).onClick.AddListener(BackToMainMenu);
    }

    private void BuildGameOver(Transform parent)
    {
        Transform box = CreateCenterBox(parent, "GameOverBox", new Vector2(790, 900), premiumPanel);
        gameOverInfoText = CreateTMP(box, "GameOverInfo", "GAME OVER", new Vector2(0, 285), TextAlignmentOptions.Center, 54);
        gameOverInfoText.rectTransform.sizeDelta = new Vector2(650, 220);
        gameOverInfoText.color = new Color(1f, 0.84f, 0.72f);
        gameOverInfoText.fontStyle = FontStyles.Bold;

        gameOverTopText = CreateTMP(box, "GameOverTop", "", new Vector2(0, 65), TextAlignmentOptions.Center, 34);
        gameOverTopText.rectTransform.sizeDelta = new Vector2(650, 230);
        gameOverTopText.color = new Color(1f, 0.91f, 0.38f);
        gameOverTopText.fontStyle = FontStyles.Bold;

        CreateButton(box, "BtnRetry", "CHƠI LẠI", new Vector2(0, -210), new Vector2(560, 86), new Color(0.10f, 0.73f, 0.36f, 1f)).onClick.AddListener(RestartGame);
        CreateButton(box, "BtnBackMenu", "MENU CHÍNH", new Vector2(0, -320), new Vector2(560, 86), new Color(0.13f, 0.50f, 0.95f, 1f)).onClick.AddListener(BackToMainMenu);
    }

    private void BuildHighScores(Transform parent)
    {
        Transform box = CreateCenterBox(parent, "HighScoreBox", new Vector2(760, 760), premiumPanel);
        TextMeshProUGUI title = CreateTMP(box, "HighTitle", "BẢNG XẾP HẠNG", new Vector2(0, 270), TextAlignmentOptions.Center, 52);
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;

        highScoreTopText = CreateTMP(box, "HighTop3", "", new Vector2(0, 95), TextAlignmentOptions.Center, 40);
        highScoreTopText.rectTransform.sizeDelta = new Vector2(620, 300);
        highScoreTopText.color = new Color(1f, 0.91f, 0.38f);
        highScoreTopText.fontStyle = FontStyles.Bold;

        CreateButton(box, "BtnClearTop", "XÓA ĐIỂM", new Vector2(0, -170), new Vector2(540, 82), new Color(0.88f, 0.25f, 0.22f, 1f)).onClick.AddListener(ClearHighScore);
        CreateButton(box, "BtnCloseHigh", "QUAY LẠI", new Vector2(0, -275), new Vector2(540, 82), new Color(0.13f, 0.50f, 0.95f, 1f)).onClick.AddListener(CloseHighScore);
    }

    private void BuildSkinMenu(Transform parent)
    {
        Transform box = CreateCenterBox(parent, "SkinBox", new Vector2(900, 880), premiumPanel);

        TextMeshProUGUI title = CreateTMP(box, "SkinTitle", "SKIN RẮN", new Vector2(0, 325), TextAlignmentOptions.Center, 56);
        title.color = premiumText;
        title.fontStyle = FontStyles.Bold;

        skinStatusText = CreateTMP(box, "SkinStatus", "Đang dùng: " + GetSkinName(selectedSkin), new Vector2(0, 255), TextAlignmentOptions.Center, 29);
        skinStatusText.color = premiumAccent;

        Transform card1 = CreateSkinCard(box, "SkinEmeraldCard", new Vector3(-285, 25, 0), "EMERALD", new Color(0.08f, 0.18f, 0.14f, 0.96f), 0, false);
        CreateButton(card1, "BtnUseEmerald", "MẶC", new Vector2(0, -135), new Vector2(220, 62), premiumAccent2, TextAlignmentOptions.Center, 25).onClick.AddListener(SelectClassicSkin);

        Transform card2 = CreateSkinCard(box, "SkinArcticCard", new Vector3(0, 25, 0), "ARCTIC\\nBLUE", new Color(0.08f, 0.13f, 0.22f, 0.96f), 1, false);

        CreateButton(card2, "BtnUseArctic", "MẶC", new Vector2(0, -135), new Vector2(220, 62), premiumAccent, TextAlignmentOptions.Center, 25).onClick.AddListener(SelectArcticSkin);

        Transform card3 = CreateSkinCard(box, "SkinMysteryCard", new Vector3(285, 25, 0), "MYSTERY", new Color(0.08f, 0.09f, 0.12f, 0.96f), 2, true);
        CreateButton(card3, "BtnMysterySkin", "KHÓA", new Vector2(0, -135), new Vector2(220, 62), new Color(0.22f, 0.25f, 0.30f, 1f), TextAlignmentOptions.Center, 25).onClick.AddListener(ShowFutureSkinMessage);

        CreateButton(box, "BtnCloseSkin", "QUAY LẠI", new Vector2(0, -335), new Vector2(560, 78), premiumPanelSoft).onClick.AddListener(CloseSkinMenu);
    }

    private Transform CreateSkinCard(Transform parent, string name, Vector3 pos, string label, Color bg, int previewSkin, bool locked)
    {
        Transform card = CreateCenterBox(parent, name, new Vector2(260, 360), bg);
        card.localPosition = pos;
        TextMeshProUGUI title = CreateTMP(card, name + "Name", label, new Vector2(0, 105), TextAlignmentOptions.Center, 30);
        title.color = locked ? premiumMuted : premiumText;
        title.fontStyle = FontStyles.Bold;
        CreateSnakePreview(card, new Vector2(0, -22), locked, previewSkin);
        if (locked)
        {
            TextMeshProUGUI lockIcon = CreateTMP(card, name + "Lock", "?", new Vector2(0, -18), TextAlignmentOptions.Center, 76);
            lockIcon.color = new Color(1f, 1f, 1f, 0.30f);
            lockIcon.fontStyle = FontStyles.Bold;
        }
        return card;
    }

    private void BuildSettings(Transform parent)
    {
        Transform box = CreateCenterBox(parent, "SettingsBox", new Vector2(760, 720), premiumPanel);
        TextMeshProUGUI title = CreateTMP(box, "SettingsTitle", "CÀI ĐẶT", new Vector2(0, 245), TextAlignmentOptions.Center, 52);
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;

        soundToggle = CreateToggle(box, "ToggleSound", "Âm thanh", new Vector2(0, 105));
        soundToggle.isOn = soundOn;
        soundToggle.onValueChanged.AddListener((isOn) => { soundOn = isOn; SaveSettings(); });

        TextMeshProUGUI label = CreateTMP(box, "DifficultyLabel", "Độ khó", new Vector2(-210, -25), TextAlignmentOptions.Left, 32);
        label.rectTransform.sizeDelta = new Vector2(180, 60);
        label.color = Color.white;

        difficultyValueText = CreateTMP(box, "DifficultyValue", "Bình thường", new Vector2(145, -25), TextAlignmentOptions.Center, 32);
        difficultyValueText.rectTransform.sizeDelta = new Vector2(340, 70);
        difficultyValueText.color = new Color(1f, 0.91f, 0.38f);
        difficultyValueText.fontStyle = FontStyles.Bold;

        CreateButton(box, "BtnDifficultyEasy", "DỄ", new Vector2(-210, -115), new Vector2(190, 70), new Color(0.10f, 0.73f, 0.36f, 1f), TextAlignmentOptions.Center, 28).onClick.AddListener(() => SetDifficulty(0));
        CreateButton(box, "BtnDifficultyNormal", "THƯỜNG", new Vector2(0, -115), new Vector2(190, 70), new Color(0.13f, 0.50f, 0.95f, 1f), TextAlignmentOptions.Center, 28).onClick.AddListener(() => SetDifficulty(1));
        CreateButton(box, "BtnDifficultyHard", "KHÓ", new Vector2(210, -115), new Vector2(190, 70), new Color(0.88f, 0.25f, 0.22f, 1f), TextAlignmentOptions.Center, 28).onClick.AddListener(() => SetDifficulty(2));

        CreateButton(box, "BtnCloseSettings", "LƯU & ĐÓNG", new Vector2(0, -275), new Vector2(540, 84), new Color(0.10f, 0.73f, 0.36f, 1f)).onClick.AddListener(CloseSettings);
    }

    private Transform CreateCenterBox(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject box = new GameObject(name);
        box.transform.SetParent(parent, false);
        RectTransform rt = box.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        Image img = box.AddComponent<Image>();
        img.color = color;
        return box.transform;
    }

    private void CreateDecorGlow(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject glow = new GameObject(name);
        glow.transform.SetParent(parent, false);
        RectTransform rt = glow.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = glow.AddComponent<Image>();
        img.sprite = circleSprite;
        img.color = color;
        img.raycastTarget = false;
    }

    private void CreateSnakePreview(Transform parent, Vector2 center, bool locked)
    {
        CreateSnakePreview(parent, center, locked, locked ? 2 : 0);
    }

    private void CreateSnakePreview(Transform parent, Vector2 center, bool locked, int skin)
    {
        Color head, bodyA, bodyB, shine;
        GetSkinPalette(Mathf.Clamp(skin, 0, 1), out head, out bodyA, out bodyB, out shine);
        for (int i = 0; i < 5; i++)
        {
            GameObject dot = new GameObject("SkinPreviewDot_" + i);
            dot.transform.SetParent(parent, false);
            RectTransform rt = dot.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center + new Vector2((i - 2) * 34, Mathf.Sin(i * 0.9f) * 12);
            rt.sizeDelta = new Vector2(i == 4 ? 56 : 44, i == 4 ? 56 : 44);
            Image img = dot.AddComponent<Image>();
            img.sprite = circleSprite;
            img.color = locked ? new Color(0.35f, 0.39f, 0.46f, 0.45f) : (i == 4 ? head : Color.Lerp(bodyA, bodyB, i / 4f));
            img.raycastTarget = false;
        }
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, Vector2 pos, TextAlignmentOptions align, int size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(760, 160);
        return tmp;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, int fontSize = 34, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        Button btn = obj.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        cb.pressedColor = Color.Lerp(color, Color.black, 0.22f);
        cb.selectedColor = color;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin ?? new Vector2(0.5f, 0.5f);
        rt.anchorMax = anchorMax ?? new Vector2(0.5f, 0.5f);
        rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI text = CreateTMP(obj.transform, "Text", label, Vector2.zero, align, fontSize);
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
        btn.onClick.AddListener(PlayButtonClick);
        return btn;
    }

    private Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(520, 80);
        Toggle toggle = obj.AddComponent<Toggle>();

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(obj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.22f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0, 0.5f);
        bgRt.anchoredPosition = new Vector2(0, 0);
        bgRt.sizeDelta = new Vector2(60, 60);

        GameObject check = new GameObject("Checkmark");
        check.transform.SetParent(bg.transform, false);
        Image checkImg = check.AddComponent<Image>();
        checkImg.color = new Color(0.15f, 0.95f, 0.45f);
        Stretch(check.GetComponent<RectTransform>(), 10);

        TextMeshProUGUI txt = CreateTMP(obj.transform, "Label", label, new Vector2(115, 0), TextAlignmentOptions.Left, 34);
        txt.rectTransform.sizeDelta = new Vector2(370, 80);
        txt.color = Color.white;
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        return toggle;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string name, Vector2 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(360, 70);
        Image img = obj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.18f);
        TMP_Dropdown dd = obj.AddComponent<TMP_Dropdown>();
        TextMeshProUGUI label = CreateTMP(obj.transform, "Label", "Bình thường", Vector2.zero, TextAlignmentOptions.Center, 28);
        Stretch(label.rectTransform);
        label.color = Color.white;
        dd.captionText = label;
        dd.options = new List<TMP_Dropdown.OptionData>
        {
            new TMP_Dropdown.OptionData("Dễ"),
            new TMP_Dropdown.OptionData("Bình thường"),
            new TMP_Dropdown.OptionData("Khó")
        };
        return dd;
    }

    private void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
    }

    private void Stretch(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    private void ShowOnly(GameObject active)
    {
        mainMenuPanel.SetActive(active == mainMenuPanel);
        hudPanel.SetActive(active == hudPanel);
        pausePanel.SetActive(active == pausePanel);
        gameOverPanel.SetActive(active == gameOverPanel);
        highScorePanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (skinPanel != null) skinPanel.SetActive(false);
    }

    private void AnimatePanel(Transform panel)
    {
        panel.localScale = Vector3.one;
    }

    private void AnimatePulse(Transform target)
    {
        if (target == null) return;
        target.localScale = Vector3.one * 1.05f;
    }

    private void SetupRuntimeAudio()
    {
        GameObject audioObj = GameObject.Find("Auto_Snake_Audio");
        if (audioObj == null) audioObj = new GameObject("Auto_Snake_Audio");
        audioSource = audioObj.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.55f;

        clickClip = CreateToneClip("ui_click", 620f, 0.045f, 0.24f);
        eatClip = CreateToneClip("eat", 880f, 0.065f, 0.34f);
        gameOverClip = CreateToneClip("game_over", 170f, 0.22f, 0.38f);
        lockedClip = CreateToneClip("locked", 260f, 0.09f, 0.28f);
    }

    public void PlayButtonClick()
    {
        PlayClip(clickClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, 1.5f);
    }

    private AudioClip CreateToneClip(string clipName, float frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - (i / (float)sampleCount));
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
        }
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private Sprite CreateSquareSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - d + 1f);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateAppleSprite()
    {
        return CreateCircleSprite(96, Color.white);
    }

    private Sprite CreateGradientSprite(int width, int height, Color top, Color bottom)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (float)(height - 1));
            for (int x = 0; x < width; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
    }
}
