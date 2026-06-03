using System.Collections.Generic;
using UnityEngine;

namespace MemoryMatchPro
{
    /// <summary>
    /// Enum định nghĩa các chế độ chơi.
    /// Thứ tự quan trọng: Easy=0, Normal=1, Hard=2, Expert=3
    /// </summary>
    public enum GameModeType
    {
        Easy   = 0,
        Normal = 1,
        Hard   = 2,
        Expert = 3
    }

    /// <summary>
    /// ScriptableObject cấu hình cho một chế độ chơi.
    /// Tạo asset: Assets > Create > MemoryMatchPro > GameModeConfig
    /// </summary>
    [CreateAssetMenu(fileName = "Mode_Easy", menuName = "MemoryMatchPro/GameModeConfig", order = 2)]
    public class GameModeConfig : ScriptableObject
    {
        [Header("Mode Identity")]
        public GameModeType modeType   = GameModeType.Normal;
        public string       modeName   = "Normal";
        public string       description = "Classic difficulty";

        [Header("Visual")]
        public Color  modeColor        = new Color(0.25f, 0.60f, 1.00f);

        [Header("Unlock Rules")]
        [Tooltip("true = tất cả level mở sẵn (Easy mode)")]
        public bool   allLevelsUnlocked = false;
        [Tooltip("Mode này luôn mở (không cần hoàn thành mode trước)")]
        public bool   alwaysUnlocked    = false;

        [Header("Levels trong mode này")]
        public List<LevelData> levels = new List<LevelData>();

        // ==================== Accessors ====================

        public int TotalLevels => levels != null ? levels.Count : 0;

        /// <summary>
        /// Lấy LevelData theo 1-based index
        /// </summary>
        public LevelData GetLevel(int levelId)
        {
            if (levels == null || levels.Count == 0)
            {
                Debug.LogError($"[GameModeConfig] Mode {modeName}: levels list rỗng!");
                return null;
            }

            int index = levelId - 1; // convert 1-based → 0-based
            if (index < 0 || index >= levels.Count)
            {
                Debug.LogWarning($"[GameModeConfig] Mode {modeName}: levelId {levelId} không hợp lệ (có {levels.Count} levels)");
                return levels.Count > 0 ? levels[0] : null;
            }
            return levels[index];
        }

        /// <summary>
        /// Kiểm tra mode này có mở khóa dựa trên mode index trước đó không
        /// </summary>
        public bool IsUnlockedByDefault()
        {
            return alwaysUnlocked || modeType == GameModeType.Easy || modeType == GameModeType.Normal;
        }
    }
}
