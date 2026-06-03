using System.Collections.Generic;
using UnityEngine;

namespace MemoryMatchPro
{
    /// <summary>
    /// ScriptableObject chứa dữ liệu cấu hình cho từng level.
    /// Tạo asset: Assets > Create > MemoryMatchPro > LevelData
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "MemoryMatchPro/LevelData", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Level Identity")]
        public int          levelId    = 1;
        public string       levelName  = "Level 1";
        public GameModeType targetMode = GameModeType.Normal;

        [Header("Grid Settings")]
        [Tooltip("Số hàng của bảng thẻ")]
        public int rows    = 2;
        [Tooltip("Số cột của bảng thẻ")]
        public int columns = 2;

        [Header("Timer")]
        [Tooltip("Thời gian giới hạn (giây) – trước khi áp difficulty multiplier")]
        public float timeLimit = 60f;

        [Header("Scoring")]
        public int baseScore    = 500;
        public int matchScore   = 100;
        public int comboBonus   = 20;
        public int wrongPenalty = 30;

        [Header("Hint System")]
        public float hintCooldown = 8f;
        public int   maxHints     = 5;

        [Header("Visuals (optional)")]
        public Sprite       backgroundSprite;
        public List<Sprite> cardSprites = new List<Sprite>();

        // ==================== Computed ====================

        public int TotalCards => rows * columns;
        public int TotalPairs => TotalCards / 2;

        /// <summary>
        /// Validate dữ liệu level – gọi trước khi bắt đầu game
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            if (rows <= 0 || columns <= 0)
            {
                errorMessage = $"[LevelData] {levelName}: rows/columns phải > 0";
                return false;
            }
            if (TotalCards % 2 != 0)
            {
                errorMessage = $"[LevelData] {levelName}: Tổng thẻ {TotalCards} ({rows}x{columns}) phải là số chẵn!";
                return false;
            }
            if (timeLimit <= 0f)
            {
                errorMessage = $"[LevelData] {levelName}: timeLimit phải > 0";
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rows > 0 && columns > 0 && TotalCards % 2 != 0)
                Debug.LogWarning($"[LevelData] '{name}': {rows}x{columns}={TotalCards} là số lẻ!");
        }
#endif
    }
}
