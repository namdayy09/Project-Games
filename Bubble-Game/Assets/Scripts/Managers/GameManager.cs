using BubbleShooterPro.Data;
using BubbleShooterPro.UI;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BubbleShooterPro.Managers
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Victory,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                }
                return _instance;
            }
        }

        [Header("Trạng thái Game")]
        [SerializeField] private GameState currentState = GameState.MainMenu;

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Tự động thiết lập trạng thái dựa trên Scene hiện tại
                DetermineInitialState();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DetermineInitialState();
        }

        private void DetermineInitialState()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "MainMenuScene" || sceneName == "MainMenu")
            {
                SetState(GameState.MainMenu);
            }
            else
            {
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// Thay đổi trạng thái trò chơi.
        /// </summary>
        public void SetState(GameState newState)
        {
            currentState = newState;
            
            // Xử lý Time.timeScale tùy trạng thái
            if (newState == GameState.Paused || newState == GameState.Victory || newState == GameState.GameOver)
            {
                Time.timeScale = 0f; // Dừng thời gian hệ thống
            }
            else
            {
                Time.timeScale = 1f; // Chạy bình thường
            }

            OnStateChanged?.Invoke(currentState);
            Debug.Log($"Trạng thái game chuyển sang: {currentState}");
        }

        /// <summary>
        /// Bắt đầu màn chơi mới với dữ liệu LevelData.
        /// </summary>
        public void StartGame(LevelData levelData)
        {
            SetState(GameState.Playing);
            
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadLevel(levelData);
            }
        }

        /// <summary>
        /// Tạm dừng trò chơi.
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
        }

        /// <summary>
        /// Tiếp tục chơi game.
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// Xử lý Chiến thắng màn chơi.
        /// </summary>
        public void WinGame()
        {
            SetState(GameState.Victory);

            // Phát nhạc/âm thanh chiến thắng
            if (AudioManager.Instance != null && AudioManager.Instance.winClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.winClip);
            }
        }

        /// <summary>
        /// Xử lý Thất bại màn chơi.
        /// </summary>
        public void LoseGame()
        {
            SetState(GameState.GameOver);

            // Phát nhạc/âm thanh thất bại
            if (AudioManager.Instance != null && AudioManager.Instance.loseClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.loseClip);
            }
        }

        /// <summary>
        /// Chơi lại màn hiện tại.
        /// </summary>
        public void RestartLevel()
        {
            SetState(GameState.Playing);
            
            if (LevelManager.Instance != null && LevelManager.Instance.currentLevelData != null)
            {
                LevelManager.Instance.LoadLevel(LevelManager.Instance.currentLevelData);
            }
            else
            {
                // Nếu chưa có dữ liệu, load lại Scene hiện tại
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        public void GameOver()
        {
            Time.timeScale = 0f;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver();
            }

            Debug.Log("GAME OVER!");
        }

        /// <summary>
        /// Quay về Menu chính.
        /// </summary>
        public void LoadMainMenu()
        {
            SetState(GameState.MainMenu);
            SceneManager.LoadScene("MainMenuScene");
        }

        #region Getters
        public GameState CurrentState => currentState;
        public bool IsPlaying => currentState == GameState.Playing;
        #endregion
    }
}
