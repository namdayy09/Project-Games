using System;
using UnityEngine;

namespace BubbleShooterPro.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        private static ScoreManager _instance;

        public static ScoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ScoreManager>();
                }

                return _instance;
            }
        }

        [Header("Điểm hiện tại")]
        [SerializeField] private int currentScore = 0;

        [Header("Điểm cao nhất")]
        [SerializeField] private int highScore = 0;

        [Header("Combo")]
        [SerializeField] private int combo = 0;
        [SerializeField] private int comboMultiplier = 1;

        [Header("Cấu hình điểm")]
        public int attachScore = 10;       // Điểm khi bắn dính bóng
        public int matchScore = 50;        // Điểm mỗi bóng khi match >= 3
        public int dropScore = 80;         // Điểm mỗi bóng rơi

        public event Action<int> OnScoreChanged;
        public event Action<int> OnHighScoreChanged;
        public event Action<int, int> OnComboChanged;

        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public int Combo => combo;
        public int ComboMultiplier => comboMultiplier;

        private const string HIGH_SCORE_KEY = "BubbleShooter_HighScore";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            LoadHighScore();
        }

        private void Start()
        {
            NotifyAll();
        }

        public void ResetScore()
        {
            currentScore = 0;
            combo = 0;
            comboMultiplier = 1;

            NotifyAll();
        }

        public void AddAttachScore()
        {
            AddScore(attachScore);
        }

        public void AddMatchScore(int bubbleCount)
        {
            if (bubbleCount <= 0) return;

            combo++;
            comboMultiplier = Mathf.Clamp(1 + combo / 2, 1, 10);

            int add = bubbleCount * matchScore * comboMultiplier;
            AddScore(add);

            OnComboChanged?.Invoke(combo, comboMultiplier);
        }

        public void AddDropScore(int bubbleCount)
        {
            if (bubbleCount <= 0) return;

            int add = bubbleCount * dropScore * Mathf.Max(1, comboMultiplier);
            AddScore(add);
        }

        public void ResetCombo()
        {
            combo = 0;
            comboMultiplier = 1;

            OnComboChanged?.Invoke(combo, comboMultiplier);
        }

        private void AddScore(int amount)
        {
            if (amount <= 0) return;

            currentScore += amount;

            if (currentScore > highScore)
            {
                highScore = currentScore;
                SaveHighScore();
                OnHighScoreChanged?.Invoke(highScore);
            }

            OnScoreChanged?.Invoke(currentScore);
        }

        private void LoadHighScore()
        {
            highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        }

        private void SaveHighScore()
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }

        private void NotifyAll()
        {
            OnScoreChanged?.Invoke(currentScore);
            OnHighScoreChanged?.Invoke(highScore);
            OnComboChanged?.Invoke(combo, comboMultiplier);
        }
    }
}