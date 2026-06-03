using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MemoryMatchPro
{
    /// <summary>
    /// Màn chọn chế độ chơi (Mode Select).
    /// Luồng: Main Menu → Mode Select → Level Select → Game
    /// </summary>
    public class ModeSelectUI : MonoBehaviour
    {
        [Header("Mode Buttons")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;
        [SerializeField] private Button expertButton;

        [Header("Mode Button Labels")]
        [SerializeField] private TextMeshProUGUI easyLabel;
        [SerializeField] private TextMeshProUGUI normalLabel;
        [SerializeField] private TextMeshProUGUI hardLabel;
        [SerializeField] private TextMeshProUGUI expertLabel;

        [Header("Lock Overlays")]
        [SerializeField] private GameObject hardLockOverlay;
        [SerializeField] private GameObject expertLockOverlay;

        [Header("Info Panel")]
        [SerializeField] private TextMeshProUGUI modeInfoText;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        [Header("Bright UI Colors")]
        [SerializeField] private Color colorEasy = new Color(0.24f, 0.82f, 0.45f); // xanh lá sáng
        [SerializeField] private Color colorNormal = new Color(0.25f, 0.58f, 1.00f); // xanh dương sáng
        [SerializeField] private Color colorHard = new Color(1.00f, 0.58f, 0.22f); // cam sáng
        [SerializeField] private Color colorExpert = new Color(0.70f, 0.35f, 1.00f); // tím sáng

        [Header("Locked Style")]
        [SerializeField] private Color colorLocked = new Color(0.46f, 0.50f, 0.62f); // xám sáng hơn, không tối trầm
        [SerializeField] private Color lockedTextColor = new Color(0.90f, 0.92f, 1.00f);
        [SerializeField] private Color unlockedTextColor = Color.white;

        private void Awake()
        {
            easyButton?.onClick.AddListener(() => OnModeSelected(GameModeType.Easy));
            normalButton?.onClick.AddListener(() => OnModeSelected(GameModeType.Normal));
            hardButton?.onClick.AddListener(() => OnModeSelected(GameModeType.Hard));
            expertButton?.onClick.AddListener(() => OnModeSelected(GameModeType.Expert));
            backButton?.onClick.AddListener(OnBackClicked);
        }

        private void Start()
        {
            RefreshUI();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            bool hardUnlocked = SaveManager.IsModeUnlocked(GameModeType.Hard);
            bool expertUnlocked = SaveManager.IsModeUnlocked(GameModeType.Expert);

            // Easy và Normal luôn mở
            SetButtonState(easyButton, null, true, colorEasy, colorLocked);
            SetButtonState(normalButton, null, true, colorNormal, colorLocked);

            // Hard và Expert phụ thuộc tiến độ
            SetButtonState(hardButton, hardLockOverlay, hardUnlocked, colorHard, colorLocked);
            SetButtonState(expertButton, expertLockOverlay, expertUnlocked, colorExpert, colorLocked);

            UpdateModeLabel(easyLabel, GameModeType.Easy, 5, true);
            UpdateModeLabel(normalLabel, GameModeType.Normal, 10, true);
            UpdateModeLabel(hardLabel, GameModeType.Hard, 10, hardUnlocked);
            UpdateModeLabel(expertLabel, GameModeType.Expert, 10, expertUnlocked);
        }

        private void SetButtonState(Button btn, GameObject lockOverlay, bool isUnlocked,
                                    Color unlockColor, Color lockColor)
        {
            if (btn == null) return;

            btn.interactable = isUnlocked;

            // Không để Button Color Tint tự làm màu bị xỉn khi chạy
            btn.transition = Selectable.Transition.None;

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isUnlocked ? unlockColor : lockColor;
            }

            lockOverlay?.SetActive(!isUnlocked);

            // Nếu có overlay khóa thì làm overlay nhẹ hơn, không che tối quá
            if (lockOverlay != null)
            {
                Image overlayImage = lockOverlay.GetComponent<Image>();
                if (overlayImage != null)
                {
                    overlayImage.color = new Color(1f, 1f, 1f, 0.08f);
                    overlayImage.raycastTarget = false;
                }
            }
        }

        private void UpdateModeLabel(TextMeshProUGUI label, GameModeType mode, int totalLevels,
                                     bool isUnlocked = true)
        {
            if (label == null) return;

            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;

            if (!isUnlocked)
            {
                label.text = $"{mode.ToString().ToUpper()}\nLOCKED";
                label.color = lockedTextColor;
                return;
            }

            int unlockedLv = Mathf.Min(SaveManager.GetModeUnlockedLevel(mode), totalLevels);

            string modeName = mode.ToString().ToUpper();
            string subText = mode == GameModeType.Easy
                ? "Unlocked - 5 Levels"
                : $"Level {unlockedLv}/{totalLevels}";

            label.text = $"{modeName}\n{subText}";
            label.color = unlockedTextColor;
        }

        private void OnModeSelected(GameModeType mode)
        {
            if (!SaveManager.IsModeUnlocked(mode))
            {
                ShowLockedInfo(mode);
                return;
            }

            AudioManager.Instance?.PlayButtonClick();
            LevelManager.SelectMode(mode);
            LevelManager.Instance?.LoadLevelSelect();
        }

        private void ShowLockedInfo(GameModeType mode)
        {
            AudioManager.Instance?.PlayButtonClick();
            if (modeInfoText == null) return;

            string msg = mode switch
            {
                GameModeType.Hard => "Complete all Normal levels to unlock Hard!",
                GameModeType.Expert => "Complete all Hard levels to unlock Expert!",
                _ => "This mode is locked."
            };

            modeInfoText.text = msg;
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            LevelManager.Instance?.LoadMainMenu();
        }
    }
}