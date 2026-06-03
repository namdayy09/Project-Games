using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MemoryMatchPro
{
    /// <summary>
    /// Màn Level Select – hiển thị các level theo mode đang chọn.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject      levelButtonPrefab;
        [SerializeField] private Transform       contentParent;
        [SerializeField] private Button          backButton;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private GridLayoutGroup gridLayoutGroup;

        private readonly List<LevelButtonUI> _levelButtons = new List<LevelButtonUI>();

        private void Awake()
        {
            backButton?.onClick.AddListener(OnBackClicked);
        }

        private void Start()      => PopulateLevels();
        private void OnEnable()   => PopulateLevels();

        private void PopulateLevels()
        {
            // Xóa buttons cũ
            foreach (var b in _levelButtons) if (b != null) Destroy(b.gameObject);
            _levelButtons.Clear();

            LevelManager lm = LevelManager.Instance;
            if (lm == null) { Debug.LogError("[LevelSelectUI] LevelManager not found!"); return; }

            GameModeType currentMode = LevelManager.GetSelectedMode();
            GameModeConfig cfg = lm.GetModeConfig(currentMode);

            if (cfg == null || cfg.levels == null || cfg.levels.Count == 0)
            {
                Debug.LogWarning($"[LevelSelectUI] Không có level nào cho mode {currentMode}");
                return;
            }

            // Cập nhật header
            if (headerText != null)
                headerText.text = $"{cfg.modeName.ToUpper()} MODE";

            if (levelButtonPrefab == null)
            {
                Debug.LogError("[LevelSelectUI] levelButtonPrefab chưa gán!");
                return;
            }

            foreach (var levelData in cfg.levels)
            {
                if (levelData == null) continue;

                var btnObj = Instantiate(levelButtonPrefab, contentParent);
                var btnUI  = btnObj.GetComponent<LevelButtonUI>();
                if (btnUI == null) continue;

                bool isUnlocked = SaveManager.IsModeLevelUnlocked(currentMode, levelData.levelId);
                int  bestScore  = SaveManager.GetModeBestScore(currentMode, levelData.levelId);
                int  stars      = SaveManager.GetModeStars(currentMode, levelData.levelId);

                btnUI.Setup(levelData, currentMode, isUnlocked, bestScore, stars, OnLevelClicked);
                _levelButtons.Add(btnUI);
            }
        }

        private void OnLevelClicked(int levelId)
        {
            AudioManager.Instance?.PlayButtonClick();
            LevelManager.SelectLevel(levelId);
            LevelManager.Instance?.LoadGameScene();
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            LevelManager.Instance?.LoadModeSelect();
        }
    }
}
