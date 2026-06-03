using UnityEngine;

namespace MemoryMatchPro
{
    /// <summary>
    /// Quản lý lưu/tải dữ liệu game bằng PlayerPrefs.
    /// Static class – gọi trực tiếp mà không cần instance.
    /// </summary>
    public static class SaveManager
    {
        // === Global keys ===
        private const string KEY_SOUND_ENABLED = "SoundEnabled";
        private const string KEY_MUSIC_ENABLED = "MusicEnabled";
        private const string KEY_DIFFICULTY    = "Difficulty";
        private const string KEY_SELECTED_MODE = "SelectedMode";

        // === Mode-aware key builders ===
        private static string ModeUnlockedKey(GameModeType mode)
            => $"Mode_{(int)mode}_Unlocked";
        private static string ModeLevelKey(GameModeType mode)
            => $"Mode_{(int)mode}_UnlockedLevel";
        private static string ModeBestScoreKey(GameModeType mode, int levelId)
            => $"Mode_{(int)mode}_Best_{levelId}";
        private static string ModeStarsKey(GameModeType mode, int levelId)
            => $"Mode_{(int)mode}_Stars_{levelId}";

        // ==================== Mode Unlock ====================

        /// <summary>Kiểm tra mode có được mở khóa không</summary>
        public static bool IsModeUnlocked(GameModeType mode)
        {
            // Easy và Normal luôn mở
            if (mode == GameModeType.Easy || mode == GameModeType.Normal)
                return true;
            return PlayerPrefs.GetInt(ModeUnlockedKey(mode), 0) == 1;
        }

        /// <summary>Mở khóa một mode mới</summary>
        public static void UnlockMode(GameModeType mode)
        {
            if (!IsModeUnlocked(mode))
            {
                PlayerPrefs.SetInt(ModeUnlockedKey(mode), 1);
                PlayerPrefs.Save();
                Debug.Log($"[PROGRESS UNLOCK] Chế độ {mode} đã được mở khóa thành công!");
            }
        }

        // ==================== Level Unlock per Mode ====================

        /// <summary>Level cao nhất đã mở khóa trong mode (tối thiểu 1)</summary>
        public static int GetModeUnlockedLevel(GameModeType mode)
        {
            // Easy: tất cả level luôn mở (trả về số lớn)
            if (mode == GameModeType.Easy)
                return 999;
            return Mathf.Max(1, PlayerPrefs.GetInt(ModeLevelKey(mode), 1));
        }

        /// <summary>Kiểm tra level có mở trong mode không</summary>
        public static bool IsModeLevelUnlocked(GameModeType mode, int levelId)
        {
            if (mode == GameModeType.Easy) return true;
            return levelId <= GetModeUnlockedLevel(mode);
        }

        /// <summary>Mở khóa level tiếp theo trong mode – không bao giờ giảm</summary>
        public static void UnlockNextModeLevel(GameModeType mode, int completedLevelId)
        {
            int nextLevel = completedLevelId + 1;
            int currentUnlocked = GetModeUnlockedLevel(mode);
            if (nextLevel > currentUnlocked)
            {
                PlayerPrefs.SetInt(ModeLevelKey(mode), nextLevel);
                PlayerPrefs.Save();
                Debug.Log($"[PROGRESS UNLOCK] Chế độ {mode}: Level {nextLevel} đã được mở khóa! (Hoàn thành Level {completedLevelId})");
            }
        }

        // ==================== Score & Stars per Mode ====================

        public static int GetModeBestScore(GameModeType mode, int levelId)
            => PlayerPrefs.GetInt(ModeBestScoreKey(mode, levelId), 0);

        /// <summary>Lưu best score. Trả về true nếu là high score mới.</summary>
        public static bool SaveModeBestScore(GameModeType mode, int levelId, int score)
        {
            if (score > GetModeBestScore(mode, levelId))
            {
                PlayerPrefs.SetInt(ModeBestScoreKey(mode, levelId), score);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public static int GetModeStars(GameModeType mode, int levelId)
            => PlayerPrefs.GetInt(ModeStarsKey(mode, levelId), 0);

        public static void SaveModeStars(GameModeType mode, int levelId, int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            if (stars > GetModeStars(mode, levelId))
            {
                PlayerPrefs.SetInt(ModeStarsKey(mode, levelId), stars);
                PlayerPrefs.Save();
            }
        }

        // ==================== Legacy keys (backward compat) ====================
        // Các key cũ vẫn dùng được để tránh crash nếu code nào đó còn gọi

        public static int  GetUnlockedLevel()              => GetModeUnlockedLevel(GameModeType.Normal);
        public static void UnlockLevel(int levelId)        => UnlockNextModeLevel(GameModeType.Normal, levelId - 1);
        public static bool IsLevelUnlocked(int levelId)    => IsModeLevelUnlocked(GameModeType.Normal, levelId);
        public static int  GetBestScore(int levelId)       => GetModeBestScore(GameModeType.Normal, levelId);
        public static bool SaveBestScore(int levelId, int score) => SaveModeBestScore(GameModeType.Normal, levelId, score);
        public static int  GetStars(int levelId)           => GetModeStars(GameModeType.Normal, levelId);
        public static void SaveStars(int levelId, int stars)    => SaveModeStars(GameModeType.Normal, levelId, stars);

        // ==================== Audio Settings ====================

        public static bool IsSoundEnabled() => PlayerPrefs.GetInt(KEY_SOUND_ENABLED, 1) == 1;
        public static void SetSoundEnabled(bool v) { PlayerPrefs.SetInt(KEY_SOUND_ENABLED, v ? 1 : 0); PlayerPrefs.Save(); }

        public static bool IsMusicEnabled() => PlayerPrefs.GetInt(KEY_MUSIC_ENABLED, 1) == 1;
        public static void SetMusicEnabled(bool v) { PlayerPrefs.SetInt(KEY_MUSIC_ENABLED, v ? 1 : 0); PlayerPrefs.Save(); }

        // ==================== Difficulty ====================

        public static int  GetDifficulty()        => PlayerPrefs.GetInt(KEY_DIFFICULTY, 1);
        public static void SetDifficulty(int val) { PlayerPrefs.SetInt(KEY_DIFFICULTY, Mathf.Clamp(val, 0, 2)); PlayerPrefs.Save(); }

        public static float GetTimeDifficultyMultiplier()
        {
            switch (GetDifficulty()) { case 0: return 1.2f; case 2: return 0.8f; default: return 1f; }
        }
        public static float GetScoreDifficultyMultiplier()
        {
            return GetDifficulty() == 2 ? 1.2f : 1f;
        }

        // ==================== Selected Mode ====================

        public static GameModeType GetSelectedMode()
            => (GameModeType)PlayerPrefs.GetInt(KEY_SELECTED_MODE, (int)GameModeType.Normal);

        public static void SetSelectedMode(GameModeType mode)
        {
            PlayerPrefs.SetInt(KEY_SELECTED_MODE, (int)mode);
            PlayerPrefs.Save();
        }

        // ==================== Reset ====================

        /// <summary>Xóa tiến độ level + score, giữ lại settings</summary>
        public static void ResetProgress()
        {
            // Reset tất cả modes
            for (int m = 0; m < 4; m++)
            {
                PlayerPrefs.DeleteKey($"Mode_{m}_Unlocked");
                PlayerPrefs.DeleteKey($"Mode_{m}_UnlockedLevel");
                for (int l = 1; l <= 50; l++)
                {
                    PlayerPrefs.DeleteKey($"Mode_{m}_Best_{l}");
                    PlayerPrefs.DeleteKey($"Mode_{m}_Stars_{l}");
                }
            }
            // Legacy keys
            PlayerPrefs.DeleteKey("UnlockedLevel");
            for (int i = 1; i <= 50; i++)
            {
                PlayerPrefs.DeleteKey($"BestScore_Level_{i}");
                PlayerPrefs.DeleteKey($"Stars_Level_{i}");
            }
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] Progress reset.");
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] All PlayerPrefs RESET. Game set to default state: Easy fully unlocked, Normal Level 1 unlocked, Hard/Expert locked.");
        }
    }
}
