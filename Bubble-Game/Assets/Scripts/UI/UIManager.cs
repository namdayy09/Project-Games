using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Text")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI highScoreText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI shotsText;
        public TextMeshProUGUI comboText;
        public GameObject comboGroup;

        [Header("Pause")]
        public GameObject pausePanel;
        public Button resumeButton;
        public Button pauseRestartButton;
        public Button pauseMenuButton;

        [Header("Victory")]
        public GameObject victoryPanel;
        public TextMeshProUGUI victoryScoreText;
        public TextMeshProUGUI victoryHighScoreText;
        public TextMeshProUGUI victoryStarsText;
        public Button nextLevelButton;
        public Button victoryReplayButton;
        public Button victoryMenuButton;

        [Header("Game Over")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI gameOverScoreText;
        public Button gameOverReplayButton;
        public Button gameOverMenuButton;

        private Canvas canvas;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            canvas = FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (gameOverPanel == null)
            {
                CreateGameOverPanel();
            }

            if (victoryPanel == null)
            {
                CreateVictoryPanel();
            }

            if (pausePanel == null)
            {
                CreatePausePanel();
            }

            BindButtons();
            HideAllPanels();
        }

        private void Start()
        {
            RefreshHud();
        }

        private void BindButtons()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (pauseRestartButton != null)
            {
                pauseRestartButton.onClick.RemoveAllListeners();
                pauseRestartButton.onClick.AddListener(RestartGame);
            }

            if (pauseMenuButton != null)
            {
                pauseMenuButton.onClick.RemoveAllListeners();
                pauseMenuButton.onClick.AddListener(GoToMainMenu);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(NextLevel);
            }

            if (victoryReplayButton != null)
            {
                victoryReplayButton.onClick.RemoveAllListeners();
                victoryReplayButton.onClick.AddListener(RestartGame);
            }

            if (victoryMenuButton != null)
            {
                victoryMenuButton.onClick.RemoveAllListeners();
                victoryMenuButton.onClick.AddListener(GoToMainMenu);
            }

            if (gameOverReplayButton != null)
            {
                gameOverReplayButton.onClick.RemoveAllListeners();
                gameOverReplayButton.onClick.AddListener(RestartGame);
            }

            if (gameOverMenuButton != null)
            {
                gameOverMenuButton.onClick.RemoveAllListeners();
                gameOverMenuButton.onClick.AddListener(GoToMainMenu);
            }
        }

        private void RefreshHud()
        {
            if (ScoreManager.Instance != null)
            {
                UpdateScore(ScoreManager.Instance.CurrentScore);
                UpdateHighScore(ScoreManager.Instance.HighScore);
                UpdateCombo(ScoreManager.Instance.Combo, ScoreManager.Instance.ComboMultiplier);
            }

            if (LevelManager.Instance != null)
            {
                UpdateLevel(LevelManager.Instance.CurrentLevelIndex);
                UpdateShots(LevelManager.Instance.ShotsRemaining);
            }
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score}";
            }

            UpdateHudText("score", $"SCORE: {score}");
        }

        public void UpdateHighScore(int highScore)
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"HIGH: {highScore}";
            }

            UpdateHudText("high", $"HIGH: {highScore}");
        }

        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"LEVEL: {level}";
            }

            UpdateHudText("level", $"LEVEL: {level}");
        }

        public void UpdateShots(int shots)
        {
            if (shotsText != null)
            {
                shotsText.text = $"BALLS: {shots}";
            }

            UpdateHudText("ball", $"BALLS: {shots}");
            UpdateHudText("shot", $"BALLS: {shots}");
        }

        public void UpdateCombo(int combo, int multiplier)
        {
            string text = combo > 0 ? $"COMBO x{multiplier}" : "";

            if (comboText != null)
            {
                comboText.text = text;
            }

            if (comboGroup != null)
            {
                comboGroup.SetActive(combo > 0);
            }

            UpdateHudText("combo", text);
        }

        private void UpdateHudText(string keyword, string value)
        {
            TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

            foreach (TextMeshProUGUI t in texts)
            {
                string n = t.gameObject.name.ToLower();

                if (n.Contains(keyword))
                {
                    t.text = value;
                }
            }
        }

        public void ShowGameOver()
        {
            Time.timeScale = 0f;
            HideAllPanels();

            if (gameOverPanel == null)
            {
                CreateGameOverPanel();
            }

            int score = 0;

            if (BubbleShooterPro.Core.BubbleGrid.Instance != null)
            {
                score = BubbleShooterPro.Core.BubbleGrid.Instance.runtimeScore;
            }
            else if (ScoreManager.Instance != null)
            {
                score = ScoreManager.Instance.CurrentScore;
            }

            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = $"SCORE: {score}";
            }

            gameOverPanel.SetActive(true);
        }

        public void ShowVictory()
        {
            Time.timeScale = 0f;
            HideAllPanels();

            if (victoryPanel == null)
            {
                CreateVictoryPanel();
            }

            int score = 0;
            int high = 0;

            if (BubbleShooterPro.Core.BubbleGrid.Instance != null)
            {
                score = BubbleShooterPro.Core.BubbleGrid.Instance.runtimeScore;
                high = Mathf.Max(PlayerPrefs.GetInt("BubbleShooter_HighScore", 0), score);
            }
            else if (ScoreManager.Instance != null)
            {
                score = ScoreManager.Instance.CurrentScore;
                high = ScoreManager.Instance.HighScore;
            }

            if (victoryScoreText != null)
            {
                victoryScoreText.text = $"SCORE: {score}";
            }

            if (victoryHighScoreText != null)
            {
                victoryHighScoreText.text = $"HIGH: {high}";
            }

            if (victoryStarsText != null)
            {
                victoryStarsText.text = "★★★";
            }

            victoryPanel.SetActive(true);
        }

        public void ShowPause()
        {
            Time.timeScale = 0f;

            if (pausePanel == null)
            {
                CreatePausePanel();
            }

            pausePanel.SetActive(true);
        }

        public void HideAllPanels()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            HideAllPanels();
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameScene");
        }

        public void NextLevel()
        {
            Time.timeScale = 1f;

            int nextLevel = 1;

            if (LevelManager.Instance != null)
            {
                nextLevel = LevelManager.Instance.CurrentLevelIndex + 1;
                LevelManager.Instance.SetSelectedLevelIndex(nextLevel);
            }
            else
            {
                nextLevel = PlayerPrefs.GetInt("BubbleShooter_SelectedLevel", 1) + 1;
                PlayerPrefs.SetInt("BubbleShooter_SelectedLevel", nextLevel);
                PlayerPrefs.Save();
            }

            SceneManager.LoadScene("GameScene");
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenuScene");
        }

        public void PauseGame()
        {
            ShowPause();
        }

        private void CreateGameOverPanel()
        {
            gameOverPanel = CreateBasePanel("GameOverPanel", new Color(0f, 0f, 0f, 0.82f));

            CreateText(gameOverPanel.transform, "GameOverTitle", "GAME OVER", 52, new Vector2(0, 150));
            gameOverScoreText = CreateText(gameOverPanel.transform, "GameOverScoreText", "SCORE: 0", 32, new Vector2(0, 80));

            gameOverReplayButton = CreateButton(gameOverPanel.transform, "GameOverReplayButton", "PLAY AGAIN", new Vector2(0, 10), RestartGame);
            gameOverMenuButton = CreateButton(gameOverPanel.transform, "GameOverMenuButton", "MAIN MENU", new Vector2(0, -60), GoToMainMenu);

            gameOverPanel.SetActive(false);
        }

        private void CreateVictoryPanel()
        {
            victoryPanel = CreateBasePanel("VictoryPanel", new Color(0f, 0f, 0f, 0.82f));

            CreateText(victoryPanel.transform, "VictoryTitle", "VICTORY!", 56, new Vector2(0, 165));
            victoryScoreText = CreateText(victoryPanel.transform, "VictoryScoreText", "SCORE: 0", 32, new Vector2(0, 95));
            victoryHighScoreText = CreateText(victoryPanel.transform, "VictoryHighScoreText", "HIGH: 0", 28, new Vector2(0, 55));
            victoryStarsText = CreateText(victoryPanel.transform, "VictoryStarsText", "★★★", 36, new Vector2(0, 15));

            nextLevelButton = CreateButton(victoryPanel.transform, "NextLevelButton", "NEXT LEVEL", new Vector2(0, -55), NextLevel);
            victoryReplayButton = CreateButton(victoryPanel.transform, "VictoryReplayButton", "REPLAY", new Vector2(0, -125), RestartGame);
            victoryMenuButton = CreateButton(victoryPanel.transform, "VictoryMenuButton", "MAIN MENU", new Vector2(0, -195), GoToMainMenu);

            victoryPanel.SetActive(false);
        }

        private void CreatePausePanel()
        {
            pausePanel = CreateBasePanel("PausePanel", new Color(0f, 0f, 0f, 0.75f));

            CreateText(pausePanel.transform, "PauseTitle", "PAUSED", 52, new Vector2(0, 130));

            resumeButton = CreateButton(pausePanel.transform, "ResumeButton", "RESUME", new Vector2(0, 40), ResumeGame);
            pauseRestartButton = CreateButton(pausePanel.transform, "PauseRestartButton", "RESTART", new Vector2(0, -30), RestartGame);
            pauseMenuButton = CreateButton(pausePanel.transform, "PauseMenuButton", "MAIN MENU", new Vector2(0, -100), GoToMainMenu);

            pausePanel.SetActive(false);
        }

        private GameObject CreateBasePanel(string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = panel.GetComponent<Image>();
            img.color = color;

            return panel;
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, int size, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(700, 70);
            rect.anchoredPosition = anchoredPos;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return tmp;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260, 52);
            rect.anchoredPosition = anchoredPos;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.08f, 0.16f, 0.26f, 1f);

            Button btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);

            TextMeshProUGUI label = CreateText(go.transform, name + "_Text", text, 22, Vector2.zero);
            label.rectTransform.sizeDelta = rect.sizeDelta;

            return btn;
        }
    }
}