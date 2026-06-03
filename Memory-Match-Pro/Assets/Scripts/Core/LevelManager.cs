using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryMatchPro
{
    /// <summary>
    /// Singleton quản lý việc chọn mode, chọn level và chuyển scene.
    /// DontDestroyOnLoad – tồn tại xuyên suốt game.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        // === Scene Names ===
        public const string SCENE_MAIN_MENU    = "MainMenuScene";
        public const string SCENE_MODE_SELECT  = "ModeSelectScene";
        public const string SCENE_LEVEL_SELECT = "LevelSelectScene";
        public const string SCENE_GAME         = "GameScene";

        // === Inspector ===
        [Header("All Mode Configs (Easy, Normal, Hard, Expert)")]
        [SerializeField] private GameModeConfig[] allModes;

        // === Static state (truyền giữa scenes) ===
        private static GameModeType _selectedMode    = GameModeType.Normal;
        private static int          _selectedLevelId = 1;   // 1-based

        // ==================== Singleton ====================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Khôi phục từ PlayerPrefs backup
            _selectedMode    = SaveManager.GetSelectedMode();
            _selectedLevelId = Mathf.Max(1, PlayerPrefs.GetInt("SelectedLevelId", 1));
        }

        // ==================== Mode Selection ====================

        public static void SelectMode(GameModeType mode)
        {
            _selectedMode = mode;
            SaveManager.SetSelectedMode(mode);
        }

        public static GameModeType GetSelectedMode() => _selectedMode;

        // ==================== Level Selection ====================

        /// <summary>
        /// Chọn level (1-based) trong mode hiện tại và lưu backup.
        /// </summary>
        public static void SelectLevel(int levelId)
        {
            _selectedLevelId = Mathf.Max(1, levelId);
            PlayerPrefs.SetInt("SelectedLevelId", _selectedLevelId);
            PlayerPrefs.Save();
        }

        public static int GetSelectedLevelId() => _selectedLevelId;

        // ==================== Get Level Data ====================

        /// <summary>
        /// Lấy GameModeConfig của mode hiện tại.
        /// </summary>
        public GameModeConfig GetCurrentModeConfig()
        {
            return GetModeConfig(_selectedMode);
        }

        public GameModeConfig GetModeConfig(GameModeType mode)
        {
            if (allModes == null || allModes.Length == 0)
            {
                Debug.LogError("[LevelManager] allModes chưa được gán trong Inspector!");
                return null;
            }
            foreach (var cfg in allModes)
            {
                if (cfg != null && cfg.modeType == mode)
                    return cfg;
            }
            Debug.LogWarning($"[LevelManager] Không tìm thấy config cho mode {mode}");
            return allModes.Length > 0 ? allModes[0] : null;
        }

        /// <summary>
        /// Lấy LevelData đang được chọn.
        /// FIX: dùng 1-based levelId, convert sang 0-based index đúng cách.
        /// </summary>
        public LevelData GetSelectedLevel()
        {
            GameModeConfig cfg = GetCurrentModeConfig();
            if (cfg == null) return null;

            // Clamp levelId về range hợp lệ
            int clampedId = Mathf.Clamp(_selectedLevelId, 1, cfg.TotalLevels);
            if (clampedId != _selectedLevelId)
            {
                Debug.LogWarning($"[LevelManager] levelId {_selectedLevelId} ngoài range, clamp về {clampedId}");
                _selectedLevelId = clampedId;
            }

            return cfg.GetLevel(_selectedLevelId);
        }

        /// <summary>
        /// Lấy toàn bộ modes
        /// </summary>
        public GameModeConfig[] GetAllModes() => allModes;

        public int TotalLevelsInCurrentMode
        {
            get
            {
                var cfg = GetCurrentModeConfig();
                return cfg != null ? cfg.TotalLevels : 0;
            }
        }

        // ==================== Scene Loading ====================

        public void LoadMainMenu()    => SceneManager.LoadScene(SCENE_MAIN_MENU);
        public void LoadModeSelect()  => SceneManager.LoadScene(SCENE_MODE_SELECT);
        public void LoadLevelSelect() => SceneManager.LoadScene(SCENE_LEVEL_SELECT);
        public void LoadGameScene()   => SceneManager.LoadScene(SCENE_GAME);

        public void ReloadCurrentScene()
            => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        public void LoadNextLevel()
        {
            var cfg = GetCurrentModeConfig();
            int nextId = _selectedLevelId + 1;
            if (cfg != null && nextId <= cfg.TotalLevels)
            {
                SelectLevel(nextId);
                LoadGameScene();
            }
            else
            {
                Debug.Log("[LevelManager] Hết level trong mode này – về Mode Select.");
                LoadModeSelect();
            }
        }

        // Legacy: dùng cho code cũ
        public LevelData[] GetAllLevels()
        {
            var cfg = GetCurrentModeConfig();
            return cfg?.levels?.ToArray();
        }

        public int TotalLevels => TotalLevelsInCurrentMode;
    }
}
