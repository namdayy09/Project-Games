using BubbleShooterPro.Core;
using BubbleShooterPro.Data;
using BubbleShooterPro.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BubbleShooterPro.Managers
{
    public class LevelManager : MonoBehaviour
    {
        private static LevelManager _instance;

        public static LevelManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LevelManager>();
                }

                return _instance;
            }
        }

        [Header("Level hiện tại")]
        public LevelData currentLevelData;

        [SerializeField] private int currentLevelIndex = 1;
        [SerializeField] private int shotsRemaining = 25;

        [Header("Fallback nếu không có LevelData")]
        public int fallbackRows = 4;
        public int fallbackCols = 8;
        public int fallbackShots = 25;

        public event Action<int> OnLevelChanged;
        public event Action<int> OnShotsChanged;

        public int CurrentLevelIndex => currentLevelIndex;
        public int ShotsRemaining => shotsRemaining;

        private const string SELECTED_LEVEL_KEY = "BubbleShooter_SelectedLevel";
        private const string UNLOCKED_LEVEL_KEY = "BubbleShooter_UnlockedLevel";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            int selectedLevel = PlayerPrefs.GetInt(SELECTED_LEVEL_KEY, 1);
            LoadLevelByIndex(selectedLevel);
        }

        public void SetSelectedLevelIndex(int levelIndex)
        {
            levelIndex = Mathf.Max(1, levelIndex);

            PlayerPrefs.SetInt(SELECTED_LEVEL_KEY, levelIndex);
            PlayerPrefs.Save();

            currentLevelIndex = levelIndex;
        }

        public void LoadLevelByIndex(int levelIndex)
        {
            currentLevelIndex = Mathf.Max(1, levelIndex);
            PlayerPrefs.SetInt(SELECTED_LEVEL_KEY, currentLevelIndex);
            PlayerPrefs.Save();

            LevelData loadedLevel = Resources.Load<LevelData>($"Levels/Level_{currentLevelIndex}");

            if (loadedLevel != null)
            {
                LoadLevel(loadedLevel);
            }
            else if (currentLevelData != null)
            {
                Debug.LogWarning($"Không tìm thấy Resources/Levels/Level_{currentLevelIndex}. Dùng currentLevelData trong Inspector.");
                LoadLevel(currentLevelData);
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy LevelData. Tạo level fallback.");
                LoadFallbackLevel();
            }
        }

        public void LoadLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                LoadFallbackLevel();
                return;
            }

            currentLevelData = levelData;
            currentLevelIndex = Mathf.Max(1, levelData.levelIndex);
            shotsRemaining = Mathf.Max(1, levelData.maxShots);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            if (BubbleGrid.Instance != null)
            {
                BubbleGrid.Instance.LoadLevelGrid(levelData);
            }

            UpdateLauncherColors();

            if (BubbleLauncher.Instance != null)
            {
                BubbleLauncher.Instance.SpawnInitialBubbles();
            }

            OnLevelChanged?.Invoke(currentLevelIndex);
            OnShotsChanged?.Invoke(shotsRemaining);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLevel(currentLevelIndex);
                UIManager.Instance.UpdateShots(shotsRemaining);
            }

            Debug.Log($"Đã nạp Level {currentLevelIndex}. Số lượt bắn: {shotsRemaining}");
        }

        private void LoadFallbackLevel()
        {
            currentLevelIndex = PlayerPrefs.GetInt(SELECTED_LEVEL_KEY, 1);
            shotsRemaining = fallbackShots;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            if (BubbleGrid.Instance != null)
            {
                BubbleGrid.Instance.InitializeEmptyGrid();

                BubbleColor[] colors =
                {
                    BubbleColor.Red,
                    BubbleColor.Blue,
                    BubbleColor.Green,
                    BubbleColor.Yellow
                };

                for (int r = 0; r < fallbackRows; r++)
                {
                    int colCount = BubbleGrid.Instance.GetColumnCount(r);

                    for (int c = 0; c < colCount; c++)
                    {
                        BubbleColor color = colors[(r + c) % colors.Length];
                        BubbleGrid.Instance.SpawnGridBubble(r, c, color);
                    }
                }
            }

            UpdateLauncherColors();

            if (BubbleLauncher.Instance != null)
            {
                BubbleLauncher.Instance.SpawnInitialBubbles();
            }

            OnLevelChanged?.Invoke(currentLevelIndex);
            OnShotsChanged?.Invoke(shotsRemaining);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLevel(currentLevelIndex);
                UIManager.Instance.UpdateShots(shotsRemaining);
            }

            Debug.Log($"Đã nạp fallback Level {currentLevelIndex}. Số lượt bắn: {shotsRemaining}");
        }

        private void UpdateLauncherColors()
        {
            if (BubbleLauncher.Instance == null) return;

            List<BubbleColor> activeColors = GetActiveColorsFromGrid();

            if (activeColors.Count == 0)
            {
                activeColors.Add(BubbleColor.Red);
                activeColors.Add(BubbleColor.Blue);
                activeColors.Add(BubbleColor.Green);
                activeColors.Add(BubbleColor.Yellow);
            }

            BubbleLauncher.Instance.SetActiveColors(activeColors);
        }

        private List<BubbleColor> GetActiveColorsFromGrid()
        {
            List<BubbleColor> colors = new List<BubbleColor>();

            if (BubbleGrid.Instance == null)
            {
                return colors;
            }

            for (int r = 0; r < BubbleGrid.Instance.maxRows; r++)
            {
                int colCount = BubbleGrid.Instance.GetColumnCount(r);

                for (int c = 0; c < colCount; c++)
                {
                    Bubble bubble = BubbleGrid.Instance.GetBubbleAt(r, c);

                    if (bubble == null) continue;
                    if (bubble.colorType == BubbleColor.None) continue;

                    if (!colors.Contains(bubble.colorType))
                    {
                        colors.Add(bubble.colorType);
                    }
                }
            }

            return colors;
        }

        public bool CanShoot()
        {
            return shotsRemaining > 0;
        }

        public void UseShot()
        {
            if (shotsRemaining <= 0)
            {
                CheckGameStatusAfterSnap();
                return;
            }

            shotsRemaining--;

            OnShotsChanged?.Invoke(shotsRemaining);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateShots(shotsRemaining);
            }

            Debug.Log("Số bóng còn lại: " + shotsRemaining);
        }

        public void CheckGameStatusAfterSnap()
        {
            if (BubbleGrid.Instance == null)
            {
                return;
            }

            if (BubbleGrid.Instance.IsGridEmpty())
            {
                WinLevel();
                return;
            }

            if (BubbleGrid.Instance.HasBubbleBelowLoseLine())
            {
                LoseLevel();
                return;
            }

            if (shotsRemaining <= 0 && BubbleGrid.Instance.HasAnyBubble())
            {
                LoseLevel();
                return;
            }
        }

        private void WinLevel()
        {
            Debug.Log("Chiến thắng màn chơi!");

            int stars = CalculateStars();

            SaveStars(currentLevelIndex, stars);
            UnlockLevel(currentLevelIndex + 1);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.WinGame();
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowVictory();
            }
        }

        private void LoseLevel()
        {
            Debug.Log("Thất bại màn chơi!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseGame();
            }
            else if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver();
            }
        }

        private int CalculateStars()
        {
            int maxShots = currentLevelData != null ? Mathf.Max(1, currentLevelData.maxShots) : Mathf.Max(1, fallbackShots);

            float percent = (float)shotsRemaining / maxShots;

            if (percent >= 0.5f) return 3;
            if (percent >= 0.25f) return 2;
            return 1;
        }

        private void UnlockLevel(int level)
        {
            int currentUnlocked = PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);

            if (level > currentUnlocked)
            {
                PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, level);
                PlayerPrefs.Save();
            }

            // Đồng bộ thêm key đơn giản nếu LevelSelectManager cũ dùng tên khác
            int oldUnlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);
            if (level > oldUnlocked)
            {
                PlayerPrefs.SetInt("UnlockedLevel", level);
                PlayerPrefs.Save();
            }
        }

        private void SaveStars(int level, int stars)
        {
            string key = $"BubbleShooter_Level_{level}_Stars";
            int oldStars = PlayerPrefs.GetInt(key, 0);

            if (stars > oldStars)
            {
                PlayerPrefs.SetInt(key, stars);
                PlayerPrefs.Save();
            }
        }

        public void RestartCurrentLevel()
        {
            SceneManager.LoadScene("GameScene");
        }

        public void LoadNextLevel()
        {
            int nextLevel = currentLevelIndex + 1;
            SetSelectedLevelIndex(nextLevel);
            SceneManager.LoadScene("GameScene");
        }
    }
}