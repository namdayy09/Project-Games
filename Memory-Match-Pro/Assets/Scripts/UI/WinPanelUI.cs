using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MemoryMatchPro
{
    /// <summary>
    /// Component riêng cho Win Panel (có thể dùng thay cho phần WinPanel trong GameUI).
    /// Gắn trực tiếp lên WinPanel GameObject.
    /// </summary>
    public class WinPanelUI : MonoBehaviour
    {
        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI movesText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI newHighScoreText;

        [Header("Stars")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;

        [Header("Buttons")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button menuButton;

        [Header("Scale-in Animation")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private float scaleInDuration = 0.35f;

        private void Awake()
        {
            nextLevelButton?.onClick.AddListener(() => {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.GoToNextLevel();
            });
            replayButton?.onClick.AddListener(() => {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.RestartLevel();
            });
            menuButton?.onClick.AddListener(() => {
                AudioManager.Instance?.PlayButtonClick();
                GameManager.Instance?.GoToMainMenu();
            });
        }

        private void OnEnable()
        {
            // Scale-in animation khi panel hiện
            if (panelRect != null)
                StartCoroutine(ScaleInCoroutine());
        }

        private System.Collections.IEnumerator ScaleInCoroutine()
        {
            float elapsed = 0f;
            panelRect.localScale = Vector3.zero;
            while (elapsed < scaleInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scaleInDuration);
                // Ease out back
                float overshoot = 1.70158f;
                t = t - 1f;
                float scale = t * t * ((overshoot + 1f) * t + overshoot) + 1f;
                panelRect.localScale = Vector3.one * scale;
                yield return null;
            }
            panelRect.localScale = Vector3.one;
        }

        /// <summary>
        /// Điền dữ liệu vào panel
        /// </summary>
        public void Show(int finalScore, float timeUsed, int moves, int maxCombo, int stars, bool isNewHighScore)
        {
            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = "🎉 YOU WIN!";

            if (scoreText != null)
                scoreText.text = $"Score\n<b>{finalScore}</b>";

            int t = Mathf.CeilToInt(timeUsed);
            if (timeText != null)
                timeText.text = $"Time\n<b>{t / 60:00}:{t % 60:00}</b>";

            if (movesText != null)
                movesText.text = $"Moves\n<b>{moves}</b>";

            if (comboText != null)
                comboText.text = $"Best Combo\n<b>x{maxCombo}</b>";

            if (newHighScoreText != null)
                newHighScoreText.gameObject.SetActive(isNewHighScore);

            SetStars(stars);

            // Ẩn Next Level nếu không còn level tiếp
            if (nextLevelButton != null)
            {
                int nextId = LevelManager.GetSelectedLevelId() + 1;
                bool hasNext = LevelManager.Instance != null && nextId <= LevelManager.Instance.TotalLevels;
                nextLevelButton.gameObject.SetActive(hasNext);
            }
        }

        private void SetStars(int count)
        {
            if (starImages == null) return;
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                bool filled = i < count;
                if (starFilledSprite != null && starEmptySprite != null)
                    starImages[i].sprite = filled ? starFilledSprite : starEmptySprite;
                else
                    starImages[i].color = filled ? Color.yellow : new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }
}
