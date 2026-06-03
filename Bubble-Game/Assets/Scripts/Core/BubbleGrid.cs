using UnityEngine;
using System.Collections.Generic;
using TMPro;
using BubbleShooterPro.Data;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.Core
{
    public class BubbleGrid : MonoBehaviour
    {
        private static BubbleGrid _instance;

        public static BubbleGrid Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<BubbleGrid>();

                return _instance;
            }
        }

        [Header("Kích thước lưới")]
        public int maxRows = 12;
        public int cols = 8;
        public int minPlayableRows = 12;

        [Header("Thông số bóng")]
        public float bubbleRadius = 0.38f;

        [Header("Vị trí lưới")]
        public Vector2 gridOrigin = new Vector2(0f, 3.45f);

        [Header("Biên vùng chơi")]
        public float leftWallX = -3.25f;
        public float rightWallX = 3.25f;
        public float ceilingY = 3.7f;
        public float loseLineY = -3.0f;

        [Header("Prefab")]
        public GameObject bubblePrefab;

        [Header("Debug Runtime")]
        public int runtimeScore = 0;
        public int runtimeBalls = 25;
        public bool gameEnded = false;

        private Bubble[,] _grid;

        private static readonly int[][] EvenRowOffsets =
        {
            new[] { 0, -1 },
            new[] { 0,  1 },
            new[] { -1, -1 },
            new[] { -1,  0 },
            new[] { 1, -1 },
            new[] { 1,  0 }
        };

        private static readonly int[][] OddRowOffsets =
        {
            new[] { 0, -1 },
            new[] { 0,  1 },
            new[] { -1,  0 },
            new[] { -1,  1 },
            new[] { 1,  0 },
            new[] { 1,  1 }
        };

        private void Awake()
        {
            if (_instance == null)
                _instance = this;

            maxRows = Mathf.Max(maxRows, minPlayableRows);
            InitializeEmptyGrid();
        }

        private void Start()
        {
            runtimeBalls = GetLevelShots();
            runtimeScore = 0;
            gameEnded = false;
            ForceUpdateHud();
        }

        public void InitializeEmptyGrid()
        {
            maxRows = Mathf.Max(maxRows, minPlayableRows);
            _grid = new Bubble[maxRows, cols];
        }

        public float BubbleDiameter => bubbleRadius * 2f;
        public float RowHeight => BubbleDiameter * 0.8660254f;

        public int GetColumnCount(int row)
        {
            if (row < 0) return cols;
            return row % 2 == 0 ? cols : Mathf.Max(1, cols - 1);
        }

        public Vector3 GridToWorldPosition(int row, int col)
        {
            int colCount = GetColumnCount(row);
            float diameter = BubbleDiameter;

            float startX = gridOrigin.x - ((colCount - 1) * diameter) / 2f;
            float x = startX + col * diameter;
            float y = gridOrigin.y - row * RowHeight;

            return new Vector3(x, y, 0f);
        }

        public bool IsValidGridPosition(int row, int col)
        {
            if (row < 0 || row >= maxRows) return false;
            if (col < 0 || col >= GetColumnCount(row)) return false;
            return true;
        }

        public Bubble GetBubbleAt(int row, int col)
        {
            if (!IsValidGridPosition(row, col)) return null;
            return _grid[row, col];
        }

        public List<Vector2Int> GetNeighbors(int row, int col)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            int[][] offsets = row % 2 == 0 ? EvenRowOffsets : OddRowOffsets;

            foreach (int[] offset in offsets)
            {
                int nr = row + offset[0];
                int nc = col + offset[1];

                if (IsValidGridPosition(nr, nc))
                    result.Add(new Vector2Int(nr, nc));
            }

            return result;
        }

        // ============================================================
        // PROJECTILE TOUCH
        // ============================================================

        public bool IsProjectileTouchingGrid(Vector2 projectilePosition)
        {
            Vector2 contact;
            return IsProjectileTouchingGrid(projectilePosition, out contact);
        }

        public bool IsProjectileTouchingGrid(Vector2 projectilePosition, float projectileRadius)
        {
            Vector2 contact;
            return IsProjectileTouchingGrid(projectilePosition, out contact);
        }

        public bool IsProjectileTouchingGrid(Vector2 projectilePosition, out Vector2 contactPoint)
        {
            contactPoint = projectilePosition;

            if (_grid == null) return false;

            float touchDistance = BubbleDiameter * 0.95f;
            float bestDistance = float.MaxValue;
            bool found = false;

            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < GetColumnCount(r); c++)
                {
                    Bubble b = _grid[r, c];
                    if (b == null) continue;

                    float distance = Vector2.Distance(projectilePosition, b.transform.position);

                    if (distance <= touchDistance && distance < bestDistance)
                    {
                        bestDistance = distance;
                        contactPoint = projectilePosition;
                        found = true;
                    }
                }
            }

            return found;
        }

        // ============================================================
        // ATTACH
        // ============================================================

        public bool AttachBubbleAtPoint(Bubble bubble, Vector2 landingPoint)
        {
            return AttachBubbleAtPoint(bubble, landingPoint, true);
        }

        public bool AttachBubbleAtPoint(Bubble bubble, Vector2 landingPoint, bool checkResultAfterAttach)
        {
            if (bubble == null) return false;
            if (gameEnded)
            {
                Destroy(bubble.gameObject);
                return false;
            }

            int row;
            int col;

            bool found = FindClosestEmptyCellToPoint(landingPoint, out row, out col);

            if (!found)
            {
                Destroy(bubble.gameObject);
                CheckAfterSnapOrDestroy();
                return false;
            }

            AttachBubbleToCell(bubble, row, col, checkResultAfterAttach);
            return true;
        }

        private bool FindClosestEmptyCellToPoint(Vector2 point, out int bestRow, out int bestCol)
        {
            bestRow = -1;
            bestCol = -1;

            float bestDistance = float.MaxValue;

            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < GetColumnCount(r); c++)
                {
                    if (_grid[r, c] != null) continue;

                    Vector2 cellPos = GridToWorldPosition(r, c);
                    float distance = Vector2.Distance(point, cellPos);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestRow = r;
                        bestCol = c;
                    }
                }
            }

            return bestRow != -1;
        }

        private void AttachBubbleToCell(Bubble bubble, int row, int col, bool checkResultAfterAttach)
        {
            if (bubble == null) return;

            if (!IsValidGridPosition(row, col))
            {
                Destroy(bubble.gameObject);
                CheckAfterSnapOrDestroy();
                return;
            }

            if (_grid[row, col] != null)
            {
                Destroy(bubble.gameObject);
                CheckAfterSnapOrDestroy();
                return;
            }

            Rigidbody2D rb = bubble.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            bubble.transform.SetParent(transform);
            bubble.transform.position = GridToWorldPosition(row, col);
            bubble.SetGridPosition(row, col);
            bubble.SetState(BubbleState.Snapped);

            _grid[row, col] = bubble;

            Debug.Log($"Snap bóng vào row={row}, col={col}");

            // CỨNG: cộng điểm và trừ bóng ngay tại đây, không phụ thuộc Manager event.
            runtimeScore += 10;
            runtimeBalls = Mathf.Max(0, runtimeBalls - 1);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddAttachScore();
            }

            ForceUpdateHud();

            ProcessMatchesAndFalls(row, col, bubble.colorType);

            if (checkResultAfterAttach)
                CheckAfterSnapOrDestroy();
        }

        // ============================================================
        // MATCH 3
        // ============================================================

        public List<Vector2Int> FindMatches(int startRow, int startCol, BubbleColor color)
        {
            List<Vector2Int> matched = new List<Vector2Int>();

            if (!IsValidGridPosition(startRow, startCol)) return matched;
            if (_grid[startRow, startCol] == null) return matched;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            Vector2Int start = new Vector2Int(startRow, startCol);
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                Bubble currentBubble = GetBubbleAt(current.x, current.y);

                if (currentBubble == null) continue;
                if (currentBubble.colorType != color) continue;

                matched.Add(current);

                foreach (Vector2Int neighbor in GetNeighbors(current.x, current.y))
                {
                    if (visited.Contains(neighbor)) continue;

                    Bubble neighborBubble = GetBubbleAt(neighbor.x, neighbor.y);

                    if (neighborBubble != null && neighborBubble.colorType == color)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return matched;
        }

        private void ProcessMatchesAndFalls(int row, int col, BubbleColor color)
        {
            List<Vector2Int> matched = FindMatches(row, col, color);

            if (matched.Count >= 3)
            {
                foreach (Vector2Int cell in matched)
                {
                    Bubble b = _grid[cell.x, cell.y];

                    if (b != null)
                    {
                        _grid[cell.x, cell.y] = null;
                        b.Pop();
                    }
                }

                int bonus = matched.Count * 50;
                runtimeScore += bonus;

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddMatchScore(matched.Count);
                }

                ForceUpdateHud();

                DropFloatingBubbles();
            }
        }

        // ============================================================
        // DROP
        // ============================================================

        public void DropFloatingBubbles()
        {
            HashSet<Vector2Int> connected = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

            for (int c = 0; c < GetColumnCount(0); c++)
            {
                if (_grid[0, c] != null)
                {
                    Vector2Int root = new Vector2Int(0, c);
                    connected.Add(root);
                    queue.Enqueue(root);
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (Vector2Int neighbor in GetNeighbors(current.x, current.y))
                {
                    if (connected.Contains(neighbor)) continue;

                    if (_grid[neighbor.x, neighbor.y] != null)
                    {
                        connected.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            int dropped = 0;

            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < GetColumnCount(r); c++)
                {
                    Bubble b = _grid[r, c];
                    if (b == null) continue;

                    Vector2Int cell = new Vector2Int(r, c);

                    if (!connected.Contains(cell))
                    {
                        _grid[r, c] = null;
                        b.SetState(BubbleState.Falling);
                        dropped++;
                    }
                }
            }

            if (dropped > 0)
            {
                runtimeScore += dropped * 80;

                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddDropScore(dropped);
                }

                ForceUpdateHud();
            }
        }

        // ============================================================
        // WIN / LOSE
        // ============================================================

        private void CheckAfterSnapOrDestroy()
        {
            if (gameEnded) return;

            if (HasBubbleBelowLoseLine())
            {
                gameEnded = true;
                Debug.Log("GAME OVER: bóng chạm/vượt vạch đỏ.");

                ForceShowGameOver();
                return;
            }

            if (IsGridEmpty())
            {
                gameEnded = true;
                Debug.Log("VICTORY: hết bóng trên lưới.");

                ForceShowVictory();
                return;
            }

            if (runtimeBalls <= 0)
            {
                gameEnded = true;
                Debug.Log("GAME OVER: hết bóng bắn.");

                ForceShowGameOver();
                return;
            }
        }

        public bool HasBubbleBelowLoseLine()
        {
            if (_grid == null) return false;

            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < GetColumnCount(r); c++)
                {
                    Bubble b = _grid[r, c];

                    if (b == null) continue;
                    if (b.state != BubbleState.Snapped) continue;

                    // Mép dưới chạm vạch là thua.
                    float bottomY = b.transform.position.y - bubbleRadius;

                    if (bottomY <= loseLineY)
                    {
                        Debug.Log($"LOSE LINE HIT: row={r}, col={c}, bottomY={bottomY}, loseLineY={loseLineY}");
                        return true;
                    }
                }
            }

            return false;
        }

        public bool HasAnyBubble()
        {
            if (_grid == null) return false;

            for (int r = 0; r < maxRows; r++)
            {
                for (int c = 0; c < GetColumnCount(r); c++)
                {
                    if (_grid[r, c] != null)
                        return true;
                }
            }

            return false;
        }

        public bool IsGridEmpty()
        {
            return !HasAnyBubble();
        }

        // ============================================================
        // UI FORCE UPDATE
        // ============================================================

        private void ForceUpdateHud()
        {
            TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

            foreach (TextMeshProUGUI t in texts)
            {
                string n = t.gameObject.name.ToLower();

                if (n.Contains("score") && !n.Contains("high") && !n.Contains("victory") && !n.Contains("gameover"))
                {
                    t.text = $"SCORE: {runtimeScore}";
                }
                else if (n.Contains("high"))
                {
                    int high = Mathf.Max(PlayerPrefs.GetInt("BubbleShooter_HighScore", 0), runtimeScore);
                    PlayerPrefs.SetInt("BubbleShooter_HighScore", high);
                    t.text = $"HIGH: {high}";
                }
                else if (n.Contains("ball") || n.Contains("shot"))
                {
                    t.text = $"BALLS: {runtimeBalls}";
                }
            }
        }

        private void ForceShowGameOver()
        {
            Time.timeScale = 0f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseGame();
            }

            Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform tr in all)
            {
                string n = tr.gameObject.name.ToLower().Replace("_", "");

                if (n.Contains("gameover"))
                    tr.gameObject.SetActive(true);
            }
        }

        private void ForceShowVictory()
        {
            Time.timeScale = 0f;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.WinGame();
            }

            Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform tr in all)
            {
                string n = tr.gameObject.name.ToLower().Replace("_", "");

                if (n.Contains("victory"))
                    tr.gameObject.SetActive(true);
            }
        }

        private int GetLevelShots()
        {
            if (LevelManager.Instance != null)
                return Mathf.Max(1, LevelManager.Instance.ShotsRemaining);

            return 25;
        }

        // ============================================================
        // LOAD LEVEL
        // ============================================================

        public void LoadLevelGrid(LevelData levelData)
        {
            ClearGrid();

            if (levelData == null)
            {
                Debug.LogError("LevelData null, không thể load lưới.");
                return;
            }

            maxRows = Mathf.Max(levelData.rows, minPlayableRows);
            cols = Mathf.Max(1, levelData.cols);

            InitializeEmptyGrid();

            runtimeBalls = Mathf.Max(1, levelData.maxShots);
            runtimeScore = 0;
            gameEnded = false;
            ForceUpdateHud();

            for (int r = 0; r < levelData.initialGrid.Length; r++)
            {
                if (r >= maxRows) break;

                BubbleColor[] rowColors = levelData.initialGrid[r].rowColors;
                int colCount = GetColumnCount(r);

                for (int c = 0; c < rowColors.Length; c++)
                {
                    if (c >= colCount) break;

                    BubbleColor color = rowColors[c];

                    if (color != BubbleColor.None)
                    {
                        SpawnGridBubble(r, c, color);
                    }
                }
            }
        }

        private void ClearGrid()
        {
            Bubble[] all = FindObjectsByType<Bubble>(FindObjectsSortMode.None);

            foreach (Bubble bubble in all)
            {
                if (bubble == null) continue;

                if (bubble.state == BubbleState.Snapped)
                    Destroy(bubble.gameObject);
            }

            InitializeEmptyGrid();
        }

        public void SpawnGridBubble(int row, int col, BubbleColor color)
        {
            if (bubblePrefab == null)
            {
                Debug.LogError("Chưa gán Bubble Prefab trong BubbleGrid!");
                return;
            }

            if (!IsValidGridPosition(row, col)) return;
            if (_grid[row, col] != null) return;

            Vector3 pos = GridToWorldPosition(row, col);
            GameObject obj = Instantiate(bubblePrefab, pos, Quaternion.identity, transform);
            obj.name = $"Bubble_{row}_{col}";

            Bubble bubble = obj.GetComponent<Bubble>();
            bubble.Initialize(color, BubbleState.Snapped);
            bubble.SetGridPosition(row, col);

            _grid[row, col] = bubble;
        }
    }
}