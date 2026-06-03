using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MemoryMatchPro
{
    /// <summary>
    /// UI component cho mỗi nút level trong màn Level Select.
    /// Hỗ trợ mode-aware unlock check.
    /// </summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button          button;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private TextMeshProUGUI bestScoreText;
        [SerializeField] private TextMeshProUGUI lockText;
        [SerializeField] private GameObject      lockOverlay;
        [SerializeField] private Image[]         starImages;  // 3 sao

        [Header("Colors")]
        [SerializeField] private Color colorUnlocked = new Color(0.20f, 0.48f, 0.90f);
        [SerializeField] private Color colorLocked   = new Color(0.22f, 0.22f, 0.28f);
        [SerializeField] private Color colorStarOn   = new Color(1.00f, 0.82f, 0.10f);
        [SerializeField] private Color colorStarOff  = new Color(0.25f, 0.25f, 0.30f, 0.6f);

        private System.Action<int> _onClick;
        private int  _levelId;
        private bool _isUnlocked;

        // ==================== Setup ====================

        /// <summary>Khởi tạo button với mode-aware data</summary>
        public void Setup(LevelData data, GameModeType mode, bool isUnlocked,
                          int bestScore, int stars, System.Action<int> onClick)
        {
            if (data == null) return;

            _levelId    = data.levelId;
            _isUnlocked = isUnlocked;
            _onClick    = onClick;

            // Level number
            if (levelNumberText != null)
                levelNumberText.text = data.levelId.ToString();

            // Lock UI
            if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);
            if (lockText    != null) lockText.text = isUnlocked ? "" : "Locked";

            // Button interactable + color
            if (button != null)
            {
                button.interactable = isUnlocked;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);

                Image img = button.GetComponent<Image>();
                if (img != null) img.color = isUnlocked ? colorUnlocked : colorLocked;
            }

            // Best score
            if (bestScoreText != null)
            {
                if (!isUnlocked)
                    bestScoreText.text = "";
                else if (bestScore > 0)
                    bestScoreText.text = bestScore.ToString("N0");
                else
                    bestScoreText.text = "--";
            }

            // Stars
            SetStars(isUnlocked ? stars : 0);
        }

        private void SetStars(int count)
        {
            if (starImages == null) return;
            for (int i = 0; i < starImages.Length; i++)
                if (starImages[i] != null)
                    starImages[i].color = i < count ? colorStarOn : colorStarOff;
        }

        private void OnClicked()
        {
            if (!_isUnlocked) return;
            _onClick?.Invoke(_levelId);
        }
    }
}
