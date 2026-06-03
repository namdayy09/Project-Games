using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MemoryMatchPro
{
    /// <summary>
    /// GameUI – quản lý toàn bộ HUD và panels trong GameScene.
    /// Không dùng emoji để tránh lỗi font.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // ==================== Top HUD ====================
        [Header("Top HUD")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI movesText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;

        // ==================== Bottom Controls ====================
        [Header("Bottom Controls")]
        [SerializeField] private Button          hintButton;
        [SerializeField] private TextMeshProUGUI hintButtonText;
        [SerializeField] private Button          pauseButton;
        [SerializeField] private TextMeshProUGUI pauseButtonText;

        // ==================== Panels ====================
        [Header("Game Panels")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject pausePanel;

        // ==================== Win Panel ====================
        [Header("Win Panel")]
        [SerializeField] private TextMeshProUGUI winTitleText;
        [SerializeField] private TextMeshProUGUI winScoreText;
        [SerializeField] private TextMeshProUGUI winTimeText;
        [SerializeField] private TextMeshProUGUI winMovesText;
        [SerializeField] private TextMeshProUGUI winComboText;
        [SerializeField] private TextMeshProUGUI winHighScoreText;
        [SerializeField] private Image[]         winStarImages;
        [SerializeField] private Button          nextLevelButton;
        [SerializeField] private Button          winReplayButton;
        [SerializeField] private Button          winMenuButton;

        // ==================== Lose Panel ====================
        [Header("Lose Panel")]
        [SerializeField] private TextMeshProUGUI loseTitleText;
        [SerializeField] private Button          loseReplayButton;
        [SerializeField] private Button          loseMenuButton;

        // ==================== Pause Panel ====================
        [Header("Pause Panel")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseReplayButton;
        [SerializeField] private Button pauseMenuButton;

        // ==================== Colors ====================
        [Header("UI Colors")]
        [SerializeField] private Color timeNormalColor = new Color(0.90f, 0.95f, 1.00f);
        [SerializeField] private Color timeWarningColor = new Color(1.00f, 0.35f, 0.35f);
        [SerializeField] private Color comboNormalColor = new Color(0.90f, 0.95f, 1.00f);
        [SerializeField] private Color comboHighColor   = new Color(1.00f, 0.85f, 0.20f);
        [SerializeField] private Color starOnColor      = new Color(1.00f, 0.82f, 0.10f);
        [SerializeField] private Color starOffColor     = new Color(0.25f, 0.25f, 0.30f, 0.5f);

        // ==================== Init ====================

        private void Awake()
        {
            // Bottom controls
            hintButton?.onClick.AddListener(OnHintClicked);
            pauseButton?.onClick.AddListener(OnPauseClicked);

            // Win panel buttons
            nextLevelButton?.onClick.AddListener(() => { Click(); GameManager.Instance?.GoToNextLevel();  });
            winReplayButton?.onClick.AddListener(() => { Click(); GameManager.Instance?.RestartLevel();   });
            winMenuButton?.onClick.AddListener(()   => { Click(); GameManager.Instance?.GoToModeSelect(); });

            // Lose panel buttons
            loseReplayButton?.onClick.AddListener(() => { Click(); GameManager.Instance?.RestartLevel();   });
            loseMenuButton?.onClick.AddListener(()   => { Click(); GameManager.Instance?.GoToModeSelect(); });

            // Pause panel buttons
            resumeButton?.onClick.AddListener(()       => { Click(); GameManager.Instance?.ResumeGame();    });
            pauseReplayButton?.onClick.AddListener(()  => { Click(); GameManager.Instance?.RestartLevel();  });
            pauseMenuButton?.onClick.AddListener(()    => { Click(); GameManager.Instance?.GoToModeSelect();});
        }

        private void Click() => AudioManager.Instance?.PlayButtonClick();

        // ==================== HUD Updates ====================

        public void UpdateTime(float seconds)
        {
            if (timeText == null) return;
            int t = Mathf.CeilToInt(Mathf.Max(0, seconds));
            timeText.text  = $"{t / 60:00}:{t % 60:00}";
            timeText.color = seconds <= 10f ? timeWarningColor : timeNormalColor;
        }

        public void UpdateMoves(int moves)
        {
            if (movesText != null) movesText.text = moves.ToString();
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = score.ToString("N0");
        }

        public void UpdateCombo(int combo)
        {
            if (comboText == null) return;
            comboText.text  = combo > 1 ? $"x{combo}" : "x1";
            comboText.color = combo >= 3 ? comboHighColor : comboNormalColor;
        }

        public void UpdateHint(int count, bool onCooldown, float cooldownTime)
        {
            if (hintButton == null) return;

            if (count <= 0)
            {
                hintButton.interactable = false;
                if (hintButtonText != null) hintButtonText.text = "Hint (0)";
            }
            else if (onCooldown)
            {
                hintButton.interactable = false;
                if (hintButtonText != null)
                    hintButtonText.text = $"Hint {Mathf.CeilToInt(cooldownTime)}s";
            }
            else
            {
                hintButton.interactable = true;
                if (hintButtonText != null) hintButtonText.text = $"Hint ({count})";
            }
        }

        // ==================== Panel Control ====================

        public void HideAllPanels()
        {
            winPanel?.SetActive(false);
            losePanel?.SetActive(false);
            pausePanel?.SetActive(false);
        }

        public void ShowWinPanel(int finalScore, float timeUsed, int moves,
                                 int maxCombo, int stars, bool isNewHigh)
        {
            if (winPanel == null) return;
            winPanel.SetActive(true);

            if (winTitleText   != null) winTitleText.text   = "Level Complete!";
            if (winScoreText   != null) winScoreText.text   = finalScore.ToString("N0");
            if (winMovesText   != null) winMovesText.text   = moves.ToString();
            if (winComboText   != null) winComboText.text   = $"x{maxCombo}";

            int t = Mathf.CeilToInt(timeUsed);
            if (winTimeText != null) winTimeText.text = $"{t / 60:00}:{t % 60:00}";

            if (winHighScoreText != null)
                winHighScoreText.gameObject.SetActive(isNewHigh);

            SetWinStars(stars);

            // Ẩn Next nếu hết level
            if (nextLevelButton != null)
            {
                int next = LevelManager.GetSelectedLevelId() + 1;
                bool has = LevelManager.Instance != null
                    && next <= LevelManager.Instance.TotalLevelsInCurrentMode;
                nextLevelButton.gameObject.SetActive(has);
            }
        }

        private void SetWinStars(int count)
        {
            if (winStarImages == null) return;
            for (int i = 0; i < winStarImages.Length; i++)
                if (winStarImages[i] != null)
                    winStarImages[i].color = i < count ? starOnColor : starOffColor;
        }

        public void ShowLosePanel()
        {
            if (losePanel == null) return;
            losePanel.SetActive(true);
            if (loseTitleText != null) loseTitleText.text = "Time's Up!";
        }

        public void ShowPausePanel()  => pausePanel?.SetActive(true);
        public void HidePausePanel()  => pausePanel?.SetActive(false);

        // ==================== Button Handlers ====================

        private void OnHintClicked()
        {
            Click();
            GameManager.Instance?.UseHint();
        }

        private void OnPauseClicked()
        {
            Click();
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.CurrentState == GameState.Playing) gm.PauseGame();
            else if (gm.CurrentState == GameState.Paused) gm.ResumeGame();
        }
    }
}
