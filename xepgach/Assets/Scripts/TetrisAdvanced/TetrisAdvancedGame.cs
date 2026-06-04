using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
 * TetrisAdvancedGame.cs
 * ---------------------------------------------------------
 * Script chính cho game di động "Xếp Gạch Tetris nâng cao".
 * Anh chỉ cần để file này trong Assets/Scripts/TetrisAdvanced.
 * Script có RuntimeInitializeOnLoadMethod nên khi mở Scene và bấm Play,
 * game sẽ tự tạo Camera, bảng 10x20, UI, nút điều khiển, Hold, Next Queue,
 * Ghost Piece, Score, Level, Combo.
 */
public class TetrisAdvancedGame : MonoBehaviour
{
    // =========================
    // 1. Cấu hình bảng chơi
    // =========================
    private const int Width = 10;
    private const int Height = 20;
    private const float CellSize = 0.48f;
    private readonly Vector2 boardOrigin = new Vector2(-2.4f, -4.75f);

    // grid[y, x] lưu block đã đặt trên bảng. null nghĩa là ô trống.
    private GameObject[,] grid = new GameObject[Width, Height];

    // =========================
    // 2. Dữ liệu khối Tetris
    // =========================
    private enum PieceType { I, O, T, S, Z, J, L }

    private class Piece
    {
        public PieceType type;
        public Vector2Int pivot;
        public Vector2Int[] cells;
        public GameObject root;
        public List<GameObject> blocks = new List<GameObject>();
        public int rotation;
        public bool lastMoveWasRotate;
    }

    private readonly Dictionary<PieceType, Color> pieceColors = new Dictionary<PieceType, Color>
    {
        { PieceType.I, new Color(0.12f, 0.86f, 1f) },
        { PieceType.O, new Color(1f, 0.86f, 0.15f) },
        { PieceType.T, new Color(0.76f, 0.28f, 1f) },
        { PieceType.S, new Color(0.24f, 0.95f, 0.36f) },
        { PieceType.Z, new Color(1f, 0.25f, 0.33f) },
        { PieceType.J, new Color(0.2f, 0.42f, 1f) },
        { PieceType.L, new Color(1f, 0.56f, 0.18f) }
    };

    private readonly Dictionary<PieceType, Vector2Int[]> spawnShapes = new Dictionary<PieceType, Vector2Int[]>
    {
        // Tọa độ tương đối quanh pivot. Spawn ở gần đỉnh bảng.
        { PieceType.I, new [] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) } },
        { PieceType.O, new [] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) } },
        { PieceType.T, new [] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1) } },
        { PieceType.S, new [] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1), new Vector2Int(0,1) } },
        { PieceType.Z, new [] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) } },
        { PieceType.J, new [] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(-1,1) } },
        { PieceType.L, new [] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1) } }
    };

    private readonly List<PieceType> bag = new List<PieceType>();
    private readonly Queue<PieceType> nextQueue = new Queue<PieceType>();

    private Piece activePiece;
    private PieceType? holdPiece = null;
    private bool canHold = true;

    // =========================
    // 3. Gameplay state
    // =========================
    private int score;
    private int level = 1;
    private int totalLines;
    private int combo = -1;
    private bool gameOver;

    private float fallTimer;
    private float baseFallInterval = 0.85f;
    private bool softDropping;

    // Touch gesture
    private Vector2 touchStart;
    private float touchStartTime;
    private const float SwipeDistance = 70f;
    private const float TapMaxTime = 0.22f;

    // =========================
    // 4. Visual references
    // =========================
    private Sprite squareSprite;
    private Transform boardRoot;
    private Transform activeRoot;
    private Transform ghostRoot;
    private Transform previewRoot;
    private Text scoreText;
    private Text levelText;
    private Text lineText;
    private Text comboText;
    private Text messageText;

    // Tự tạo GameObject chạy game, tránh việc anh phải kéo script vào scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStartGame()
    {
        if (FindObjectOfType<TetrisAdvancedGame>() != null) return;
        GameObject go = new GameObject("Tetris Advanced Game - Auto Bootstrap");
        go.AddComponent<TetrisAdvancedGame>();
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        squareSprite = CreateSquareSprite();
        BuildScene();
        RestartGame();
    }

    private void Update()
    {
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartGame();
            return;
        }

        HandleKeyboardForEditor();
        HandleTouchInput();
        TickFall();
        UpdateGhostPiece();
    }

    // =========================
    // 5. Khởi tạo Scene/UI đẹp, cân đối dọc mobile
    // =========================
    private void BuildScene()
    {
        Camera.main.transform.position = new Vector3(0f, 0f, -10f);
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = 6.3f;
        Camera.main.backgroundColor = new Color(0.035f, 0.045f, 0.075f);

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        boardRoot = new GameObject("Board Root").transform;
        activeRoot = new GameObject("Active Piece Root").transform;
        ghostRoot = new GameObject("Ghost Piece Root").transform;
        previewRoot = new GameObject("Preview Root").transform;

        CreateBackgroundPanel("Board Background", new Vector2(0f, 0f), new Vector2(5.25f, 10.45f), new Color(0.08f, 0.105f, 0.16f));
        CreateGridLines();
        CreateSideWorldLabels();
        BuildCanvasUI();
    }

    private void CreateGridLines()
    {
        // Các ô nền mờ để bảng rõ ràng, nhìn giống game mobile hiện đại.
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                GameObject cell = CreateBlockVisual("Grid Cell", new Color(1f, 1f, 1f, 0.045f), boardRoot);
                cell.transform.position = CellToWorld(new Vector2Int(x, y));
                cell.transform.localScale = Vector3.one * (CellSize * 0.88f);
            }
        }
    }

    private void CreateSideWorldLabels()
    {
        CreateBackgroundPanel("Hold Panel", new Vector2(-4.75f, 2.9f), new Vector2(1.75f, 1.8f), new Color(0.075f, 0.09f, 0.14f));
        CreateBackgroundPanel("Next Panel", new Vector2(4.75f, 2.15f), new Vector2(1.75f, 3.2f), new Color(0.075f, 0.09f, 0.14f));
        CreateBackgroundPanel("Info Panel", new Vector2(4.75f, -2.05f), new Vector2(1.75f, 2.75f), new Color(0.075f, 0.09f, 0.14f));
    }

    private void BuildCanvasUI()
    {
        GameObject canvasGo = new GameObject("Mobile UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateUIText(canvas.transform, "XẾP GẠCH TETRIS", new Vector2(0, -48), 44, TextAnchor.MiddleCenter, font, Color.white);
        scoreText = CreateUIText(canvas.transform, "Score: 0", new Vector2(350, -205), 30, TextAnchor.MiddleLeft, font, Color.white);
        levelText = CreateUIText(canvas.transform, "Level: 1", new Vector2(350, -255), 30, TextAnchor.MiddleLeft, font, Color.white);
        lineText = CreateUIText(canvas.transform, "Lines: 0", new Vector2(350, -305), 30, TextAnchor.MiddleLeft, font, Color.white);
        comboText = CreateUIText(canvas.transform, "Combo: -", new Vector2(350, -355), 30, TextAnchor.MiddleLeft, font, new Color(1f, 0.86f, 0.26f));
        CreateUIText(canvas.transform, "HOLD", new Vector2(-420, -205), 32, TextAnchor.MiddleCenter, font, new Color(0.72f, 0.86f, 1f));
        CreateUIText(canvas.transform, "NEXT", new Vector2(420, -205), 32, TextAnchor.MiddleCenter, font, new Color(0.72f, 0.86f, 1f));

        messageText = CreateUIText(canvas.transform, "", new Vector2(0, -935), 30, TextAnchor.MiddleCenter, font, new Color(1f, 0.95f, 0.65f));

        CreateUIButton(canvas.transform, "◀", new Vector2(-330, 780), new Vector2(180, 125), () => Move(-1, 0, false));
        CreateUIButton(canvas.transform, "▶", new Vector2(-120, 780), new Vector2(180, 125), () => Move(1, 0, false));
        CreateUIButton(canvas.transform, "⟳", new Vector2(115, 780), new Vector2(180, 125), RotateActivePiece);
        CreateUIButton(canvas.transform, "HOLD", new Vector2(330, 780), new Vector2(190, 125), HoldCurrentPiece);
        CreateUIButton(canvas.transform, "DROP", new Vector2(0, 925), new Vector2(390, 105), HardDrop);
        CreateUIButton(canvas.transform, "RESTART", new Vector2(0, 1060), new Vector2(360, 88), RestartGame);
    }

    private Text CreateUIText(Transform parent, string text, Vector2 anchoredPos, int size, TextAnchor anchor, Font font, Color color)
    {
        GameObject go = new GameObject(text, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(700, 70);

        Text t = go.GetComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        return t;
    }

    private void CreateUIButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size, Action onClick)
    {
        GameObject go = new GameObject("Button " + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.12f, 0.18f, 0.30f, 0.92f);

        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        Text txt = CreateUIText(go.transform, label, Vector2.zero, 34, TextAnchor.MiddleCenter, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), Color.white);
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.anchoredPosition = Vector2.zero;
        tr.sizeDelta = Vector2.zero;
    }

    // =========================
    // 6. Game flow: restart, spawn, hold, next queue
    // =========================
    private void RestartGame()
    {
        foreach (Transform child in activeRoot) Destroy(child.gameObject);
        foreach (Transform child in ghostRoot) Destroy(child.gameObject);
        foreach (Transform child in previewRoot) Destroy(child.gameObject);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] != null) Destroy(grid[x, y]);
                grid[x, y] = null;
            }
        }

        score = 0;
        level = 1;
        totalLines = 0;
        combo = -1;
        gameOver = false;
        holdPiece = null;
        canHold = true;
        bag.Clear();
        nextQueue.Clear();

        while (nextQueue.Count < 3) nextQueue.Enqueue(GetNextFromBag());
        SpawnPiece(nextQueue.Dequeue());
        nextQueue.Enqueue(GetNextFromBag());
        RefreshUI();
        ShowMessage("Vuốt lên: rơi nhanh | Vuốt xuống: rơi chậm");
    }

    private PieceType GetNextFromBag()
    {
        if (bag.Count == 0)
        {
            foreach (PieceType t in Enum.GetValues(typeof(PieceType))) bag.Add(t);
            for (int i = 0; i < bag.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, bag.Count);
                PieceType temp = bag[i];
                bag[i] = bag[r];
                bag[r] = temp;
            }
        }

        PieceType result = bag[0];
        bag.RemoveAt(0);
        return result;
    }

    private void SpawnPiece(PieceType type)
    {
        activePiece = CreatePiece(type, new Vector2Int(Width / 2, Height - 2), activeRoot, false);
        canHold = true;
        activePiece.lastMoveWasRotate = false;

        if (!IsValid(activePiece.cells))
        {
            gameOver = true;
            ShowMessage("GAME OVER - bấm RESTART để chơi lại");
        }

        DrawNextAndHoldPreview();
        UpdateGhostPiece();
    }

    private Piece CreatePiece(PieceType type, Vector2Int pivot, Transform parent, bool ghost)
    {
        Piece piece = new Piece();
        piece.type = type;
        piece.pivot = pivot;
        piece.cells = CloneShape(spawnShapes[type], pivot);
        piece.root = new GameObject(type + (ghost ? " Ghost" : " Piece"));
        piece.root.transform.SetParent(parent, false);

        Color color = pieceColors[type];
        if (ghost) color = new Color(1f, 1f, 1f, 0.18f);

        for (int i = 0; i < 4; i++)
        {
            GameObject block = CreateBlockVisual(type + " Block", color, piece.root.transform);
            block.transform.localScale = Vector3.one * (CellSize * (ghost ? 0.83f : 0.93f));
            piece.blocks.Add(block);
        }

        ApplyPieceVisual(piece);
        return piece;
    }

    private void HoldCurrentPiece()
    {
        if (!canHold || activePiece == null || gameOver) return;

        PieceType current = activePiece.type;
        Destroy(activePiece.root);

        if (holdPiece == null)
        {
            holdPiece = current;
            SpawnPiece(nextQueue.Dequeue());
            nextQueue.Enqueue(GetNextFromBag());
        }
        else
        {
            PieceType temp = holdPiece.Value;
            holdPiece = current;
            SpawnPiece(temp);
        }

        canHold = false;
        DrawNextAndHoldPreview();
        ShowMessage("Đã HOLD khối " + current);
    }

    // =========================
    // 7. Di chuyển / xoay / wall kick / drop
    // =========================
    private void TickFall()
    {
        fallTimer += Time.deltaTime;
        float interval = softDropping ? 0.045f : Mathf.Max(0.08f, baseFallInterval - (level - 1) * 0.065f);

        if (fallTimer >= interval)
        {
            fallTimer = 0f;
            if (!Move(0, -1, softDropping))
            {
                LockPiece();
            }
        }
    }

    private bool Move(int dx, int dy, bool addSoftScore)
    {
        if (activePiece == null || gameOver) return false;

        Vector2Int[] moved = new Vector2Int[activePiece.cells.Length];
        for (int i = 0; i < moved.Length; i++) moved[i] = activePiece.cells[i] + new Vector2Int(dx, dy);

        if (!IsValid(moved)) return false;

        activePiece.pivot += new Vector2Int(dx, dy);
        activePiece.cells = moved;
        activePiece.lastMoveWasRotate = false;
        ApplyPieceVisual(activePiece);

        if (addSoftScore && dy < 0)
        {
            score += 1;
            RefreshUI();
        }

        return true;
    }

    private void RotateActivePiece()
    {
        if (activePiece == null || gameOver || activePiece.type == PieceType.O) return;

        Vector2Int[] rotated = new Vector2Int[activePiece.cells.Length];
        for (int i = 0; i < activePiece.cells.Length; i++)
        {
            Vector2Int rel = activePiece.cells[i] - activePiece.pivot;
            // Xoay 90 độ theo chiều kim đồng hồ: (x, y) -> (y, -x)
            Vector2Int newRel = new Vector2Int(rel.y, -rel.x);
            rotated[i] = activePiece.pivot + newRel;
        }

        // Wall kick đơn giản: nếu xoay vướng tường/khối, thử dịch sang hai bên hoặc lên 1 ô.
        Vector2Int[] kicks = activePiece.type == PieceType.I
            ? new[] { Vector2Int.zero, new Vector2Int(-2, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) }
            : new[] { Vector2Int.zero, new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(1, 1) };

        foreach (Vector2Int kick in kicks)
        {
            Vector2Int[] test = new Vector2Int[rotated.Length];
            for (int i = 0; i < rotated.Length; i++) test[i] = rotated[i] + kick;

            if (IsValid(test))
            {
                activePiece.cells = test;
                activePiece.pivot += kick;
                activePiece.rotation = (activePiece.rotation + 1) % 4;
                activePiece.lastMoveWasRotate = true;
                ApplyPieceVisual(activePiece);
                ShowMessage(kick == Vector2Int.zero ? "Rotate" : "Wall kick!");
                return;
            }
        }
    }

    private void HardDrop()
    {
        if (activePiece == null || gameOver) return;

        int dropped = 0;
        while (Move(0, -1, false)) dropped++;
        score += dropped * 2;
        LockPiece();
        RefreshUI();
    }

    private void LockPiece()
    {
        bool tSpin = DetectTSpin(activePiece);

        foreach (GameObject block in activePiece.blocks)
        {
            block.transform.SetParent(boardRoot, true);
        }

        for (int i = 0; i < activePiece.cells.Length; i++)
        {
            Vector2Int c = activePiece.cells[i];
            if (c.y >= 0 && c.y < Height && c.x >= 0 && c.x < Width)
            {
                grid[c.x, c.y] = activePiece.blocks[i];
            }
        }

        Destroy(activePiece.root);
        int cleared = ClearLines();
        AddScoreForLines(cleared, tSpin);

        SpawnPiece(nextQueue.Dequeue());
        nextQueue.Enqueue(GetNextFromBag());
        RefreshUI();
    }

    // T-spin detection cơ bản: khối T vừa rotate, 3/4 góc quanh pivot bị chặn.
    private bool DetectTSpin(Piece piece)
    {
        if (piece == null || piece.type != PieceType.T || !piece.lastMoveWasRotate) return false;

        Vector2Int[] corners =
        {
            piece.pivot + new Vector2Int(-1, -1),
            piece.pivot + new Vector2Int(1, -1),
            piece.pivot + new Vector2Int(-1, 1),
            piece.pivot + new Vector2Int(1, 1)
        };

        int blocked = 0;
        foreach (Vector2Int c in corners)
        {
            if (c.x < 0 || c.x >= Width || c.y < 0 || c.y >= Height || grid[c.x, c.y] != null)
                blocked++;
        }
        return blocked >= 3;
    }

    // =========================
    // 8. Xóa hàng, điểm, combo, level
    // =========================
    private int ClearLines()
    {
        int cleared = 0;
        for (int y = 0; y < Height; y++)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (grid[x, y] == null)
                {
                    full = false;
                    break;
                }
            }

            if (!full) continue;

            for (int x = 0; x < Width; x++)
            {
                Destroy(grid[x, y]);
                grid[x, y] = null;
            }

            for (int row = y + 1; row < Height; row++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, row] != null)
                    {
                        grid[x, row - 1] = grid[x, row];
                        grid[x, row] = null;
                        grid[x, row - 1].transform.position = CellToWorld(new Vector2Int(x, row - 1));
                    }
                }
            }

            cleared++;
            y--; // sau khi kéo dòng xuống cần kiểm tra lại dòng hiện tại.
        }

        return cleared;
    }

    private void AddScoreForLines(int cleared, bool tSpin)
    {
        if (cleared <= 0)
        {
            combo = -1;
            if (tSpin)
            {
                score += 400 * level;
                ShowMessage("T-SPIN!");
            }
            return;
        }

        combo++;
        totalLines += cleared;
        level = 1 + totalLines / 10;

        int lineScore = 0;
        if (tSpin)
        {
            int[] tSpinScores = { 0, 800, 1200, 1600, 2000 };
            lineScore = tSpinScores[Mathf.Clamp(cleared, 0, 4)] * level;
            ShowMessage("T-SPIN +" + cleared + " LINE!");
        }
        else
        {
            switch (cleared)
            {
                case 1: lineScore = 100 * level; break;
                case 2: lineScore = 300 * level; break;
                case 3: lineScore = 500 * level; break;
                default: lineScore = 800 * level; ShowMessage("TETRIS!"); break;
            }
        }

        int comboBonus = combo > 0 ? combo * 50 * level : 0;
        score += lineScore + comboBonus;
        if (comboBonus > 0) ShowMessage("Combo x" + combo + " +" + comboBonus);
    }

    // =========================
    // 9. Ghost piece + preview
    // =========================
    private void UpdateGhostPiece()
    {
        foreach (Transform child in ghostRoot) Destroy(child.gameObject);
        if (activePiece == null || gameOver) return;

        Vector2Int[] ghostCells = CloneCells(activePiece.cells);
        while (IsValid(MoveCells(ghostCells, 0, -1)))
        {
            ghostCells = MoveCells(ghostCells, 0, -1);
        }

        foreach (Vector2Int c in ghostCells)
        {
            GameObject g = CreateBlockVisual("Ghost", new Color(1f, 1f, 1f, 0.15f), ghostRoot);
            g.transform.position = CellToWorld(c);
            g.transform.localScale = Vector3.one * (CellSize * 0.82f);
        }
    }

    private void DrawNextAndHoldPreview()
    {
        foreach (Transform child in previewRoot) Destroy(child.gameObject);

        if (holdPiece != null)
        {
            DrawMiniPiece(holdPiece.Value, new Vector2(-4.75f, 2.75f), 0.26f);
        }

        int index = 0;
        foreach (PieceType t in nextQueue)
        {
            DrawMiniPiece(t, new Vector2(4.75f, 2.95f - index * 1.05f), 0.24f);
            index++;
            if (index >= 3) break;
        }
    }

    private void DrawMiniPiece(PieceType type, Vector2 center, float scale)
    {
        Vector2Int[] shape = spawnShapes[type];
        foreach (Vector2Int p in shape)
        {
            GameObject b = CreateBlockVisual("Preview " + type, pieceColors[type], previewRoot);
            b.transform.position = new Vector3(center.x + p.x * scale, center.y + p.y * scale, -0.5f);
            b.transform.localScale = Vector3.one * scale * 0.92f;
        }
    }

    // =========================
    // 10. Input mobile + phím test trong Editor
    // =========================
    private void HandleKeyboardForEditor()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Move(-1, 0, false);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Move(1, 0, false);
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) RotateActivePiece();
        if (Input.GetKeyDown(KeyCode.Space)) HardDrop();
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.C)) HoldCurrentPiece();
        softDropping = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

        if (touch.phase == TouchPhase.Began)
        {
            touchStart = touch.position;
            touchStartTime = Time.time;
        }
        else if (touch.phase == TouchPhase.Moved)
        {
            Vector2 delta = touch.position - touchStart;
            softDropping = delta.y < -SwipeDistance;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            softDropping = false;
            Vector2 swipe = touch.position - touchStart;
            float duration = Time.time - touchStartTime;

            if (Mathf.Abs(swipe.y) > Mathf.Abs(swipe.x) && swipe.y > SwipeDistance)
            {
                HardDrop();
                return;
            }

            if (Mathf.Abs(swipe.x) > SwipeDistance)
            {
                Move(swipe.x > 0 ? 1 : -1, 0, false);
                return;
            }

            // Tap nhanh: nửa trái di chuyển trái, giữa xoay, nửa phải di chuyển phải.
            if (duration <= TapMaxTime)
            {
                float x = touch.position.x / Screen.width;
                if (x < 0.33f) Move(-1, 0, false);
                else if (x > 0.66f) Move(1, 0, false);
                else RotateActivePiece();
            }
        }
    }

    // =========================
    // 11. Helper: validate, render, tạo sprite
    // =========================
    private bool IsValid(Vector2Int[] cells)
    {
        foreach (Vector2Int c in cells)
        {
            if (c.x < 0 || c.x >= Width || c.y < 0) return false;
            if (c.y < Height && grid[c.x, c.y] != null) return false;
        }
        return true;
    }

    private void ApplyPieceVisual(Piece piece)
    {
        for (int i = 0; i < piece.blocks.Count; i++)
        {
            piece.blocks[i].transform.position = CellToWorld(piece.cells[i]);
        }
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(boardOrigin.x + cell.x * CellSize + CellSize / 2f, boardOrigin.y + cell.y * CellSize + CellSize / 2f, 0f);
    }

    private Vector2Int[] CloneShape(Vector2Int[] shape, Vector2Int pivot)
    {
        Vector2Int[] result = new Vector2Int[shape.Length];
        for (int i = 0; i < shape.Length; i++) result[i] = pivot + shape[i];
        return result;
    }

    private Vector2Int[] CloneCells(Vector2Int[] cells)
    {
        Vector2Int[] result = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++) result[i] = cells[i];
        return result;
    }

    private Vector2Int[] MoveCells(Vector2Int[] cells, int dx, int dy)
    {
        Vector2Int[] result = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++) result[i] = cells[i] + new Vector2Int(dx, dy);
        return result;
    }

    private GameObject CreateBlockVisual(string name, Color color, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = color;
        sr.sortingOrder = 5;
        return go;
    }

    private void CreateBackgroundPanel(string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = CreateBlockVisual(name, color, boardRoot);
        go.transform.position = new Vector3(pos.x, pos.y, 1f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.GetComponent<SpriteRenderer>().sortingOrder = -5;
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        tex.filterMode = FilterMode.Point;
        Color[] pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                bool border = x < 2 || x > 29 || y < 2 || y > 29;
                pixels[y * 32 + x] = border ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    private void RefreshUI()
    {
        if (scoreText != null) scoreText.text = "Score: " + score;
        if (levelText != null) levelText.text = "Level: " + level;
        if (lineText != null) lineText.text = "Lines: " + totalLines;
        if (comboText != null) comboText.text = combo > 0 ? "Combo: x" + combo : "Combo: -";
        DrawNextAndHoldPreview();
    }

    private void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
        StopCoroutine(nameof(ClearMessageAfterDelay));
        StartCoroutine(nameof(ClearMessageAfterDelay));
    }

    private IEnumerator ClearMessageAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        if (!gameOver && messageText != null) messageText.text = "";
    }
}
