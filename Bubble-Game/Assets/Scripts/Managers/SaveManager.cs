using UnityEngine;
using System.Collections.Generic;

namespace BubbleShooterPro.Managers
{
    [System.Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int score;
        public string date;

        public LeaderboardEntry(string name, int score, string date)
        {
            this.playerName = name;
            this.score = score;
            this.date = date;
        }
    }

    [System.Serializable]
    public class GameProgress
    {
        public int unlockedLevel = 1; // Level cao nhất đã mở khóa
        public List<int> highScores = new List<int>(); // Điểm cao nhất của từng level
        public List<int> stars = new List<int>();      // Số sao đạt được (0-3) cho mỗi level
        public bool isBgmOn = true;  // Trạng thái nhạc nền
        public bool isSfxOn = true;  // Trạng thái âm thanh hiệu ứng
        public List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>(); // BXH điểm cao local
    }

    public class SaveManager : MonoBehaviour
    {
        private static SaveManager _instance;
        public static SaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private GameProgress _progress;
        private const string SAVE_KEY = "BubbleShooterPro_Progress";
        private const int MAX_LEADERBOARD_ENTRIES = 5;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Load();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Lưu tiến trình hiện tại.
        /// </summary>
        public void Save()
        {
            string json = JsonUtility.ToJson(_progress);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Tải tiến trình đã lưu.
        /// </summary>
        public void Load()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                _progress = JsonUtility.FromJson<GameProgress>(json);
            }
            else
            {
                _progress = new GameProgress();
                InitializeDefaultLeaderboard();
                Save();
            }
        }

        /// <summary>
        /// Khởi tạo bảng điểm cao mẫu nếu chưa có dữ liệu.
        /// </summary>
        private void InitializeDefaultLeaderboard()
        {
            _progress.leaderboard.Clear();
            _progress.leaderboard.Add(new LeaderboardEntry("Hero", 5000, "03/06/2026"));
            _progress.leaderboard.Add(new LeaderboardEntry("ProPlayer", 3000, "03/06/2026"));
            _progress.leaderboard.Add(new LeaderboardEntry("BubbleMaster", 2000, "03/06/2026"));
            _progress.leaderboard.Add(new LeaderboardEntry("CasualGuy", 1000, "03/06/2026"));
            _progress.leaderboard.Add(new LeaderboardEntry("Newbie", 500, "03/06/2026"));
        }

        /// <summary>
        /// Khôi phục toàn bộ tiến trình về mặc định.
        /// </summary>
        public void ResetProgress()
        {
            _progress = new GameProgress();
            InitializeDefaultLeaderboard();
            Save();
        }

        /// <summary>
        /// Thêm một điểm cao mới vào Leaderboard local.
        /// </summary>
        public bool AddLeaderboardEntry(string name, int score)
        {
            string dateStr = System.DateTime.Now.ToString("dd/MM/yyyy");
            LeaderboardEntry newEntry = new LeaderboardEntry(name, score, dateStr);
            _progress.leaderboard.Add(newEntry);

            // Sắp xếp giảm dần theo score
            _progress.leaderboard.Sort((a, b) => b.score.CompareTo(a.score));

            // Cắt bớt phần thừa
            if (_progress.leaderboard.Count > MAX_LEADERBOARD_ENTRIES)
            {
                _progress.leaderboard.RemoveRange(MAX_LEADERBOARD_ENTRIES, _progress.leaderboard.Count - MAX_LEADERBOARD_ENTRIES);
            }

            Save();

            // Trả về true nếu điểm này lọt được vào BXH
            return _progress.leaderboard.Contains(newEntry);
        }

        public List<LeaderboardEntry> GetLeaderboard()
        {
            return _progress.leaderboard;
        }

        #region Getters / Setters

        public int UnlockedLevel
        {
            get => _progress.unlockedLevel;
            set
            {
                _progress.unlockedLevel = value;
                Save();
            }
        }

        public bool IsBgmOn
        {
            get => _progress.isBgmOn;
            set
            {
                _progress.isBgmOn = value;
                Save();
            }
        }

        public bool IsSfxOn
        {
            get => _progress.isSfxOn;
            set
            {
                _progress.isSfxOn = value;
                Save();
            }
        }

        public int GetHighScore(int levelIndex)
        {
            int index = levelIndex - 1;
            if (index >= 0 && index < _progress.highScores.Count)
            {
                return _progress.highScores[index];
            }
            return 0;
        }

        public void SetHighScore(int levelIndex, int score)
        {
            int index = levelIndex - 1;
            while (_progress.highScores.Count <= index)
            {
                _progress.highScores.Add(0);
            }

            if (score > _progress.highScores[index])
            {
                _progress.highScores[index] = score;
                Save();
            }
        }

        public int GetStars(int levelIndex)
        {
            int index = levelIndex - 1;
            if (index >= 0 && index < _progress.stars.Count)
            {
                return _progress.stars[index];
            }
            return 0;
        }

        public void SetStars(int levelIndex, int starsCount)
        {
            int index = levelIndex - 1;
            while (_progress.stars.Count <= index)
            {
                _progress.stars.Add(0);
            }

            if (starsCount > _progress.stars[index])
            {
                _progress.stars[index] = starsCount;
                Save();
            }
        }

        #endregion
    }
}
