using System.Collections;
using UnityEngine;

namespace MemoryMatchPro
{
    /// <summary>Trạng thái game</summary>
    public enum GameState { None, Playing, Paused, Win, Lose }

    /// <summary>
    /// Singleton quản lý toàn bộ logic game:
    /// Timer, Moves, Score, Combo, Hint, GameState.
    /// Có Debug Mode để test nhanh trong Editor.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ==================== Inspector ====================
        [Header("References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private GameUI       gameUI;

        [Header("Debug Mode (Editor Only)")]
        [SerializeField] private bool debugMode  = false;
        [SerializeField] private int  debugLevel = 1;  // level ID để load khi debugMode=true

        // ==================== State ====================
        public GameState CurrentState { get; private set; } = GameState.None;

        private LevelData _levelData;
        private GameModeType _currentMode;

        private float _currentTime;
        private Coroutine _timerCoroutine;

        private int   _moves;
        private int   _score;
        private int   _combo;
        private int   _maxCombo;
        private int   _hintCount;
        private bool  _hintOnCooldown;
        private float _hintCooldownRemaining;

        private float _scoreMultiplier = 1f;
        private float _timeMultiplier  = 1f;

        // ==================== Public Accessors ====================
        public int   Moves       => _moves;
        public int   Score       => _score;
        public int   Combo       => _combo;
        public int   MaxCombo    => _maxCombo;
        public float CurrentTime => _currentTime;
        public int   HintCount   => _hintCount;
        public bool  IsPlaying   => CurrentState == GameState.Playing;
        public LevelData CurrentLevelData => _levelData;

        // ==================== Unity Lifecycle ====================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _scoreMultiplier = SaveManager.GetScoreDifficultyMultiplier();
            _timeMultiplier  = SaveManager.GetTimeDifficultyMultiplier();
            StartGame();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            // W = win ngay
            if (Input.GetKeyDown(KeyCode.W)) DebugWinLevel();
            // L = lose ngay
            if (Input.GetKeyDown(KeyCode.L)) DebugLoseLevel();
            // N = sang level tiếp theo
            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("[DEBUG] N pressed – Next Level");
                LevelManager.Instance?.LoadNextLevel();
            }
            // R = reset PlayerPrefs
            if (Input.GetKeyDown(KeyCode.R))
            {
                SaveManager.ResetAll();
                Debug.Log("[DEBUG] R pressed – All PlayerPrefs RESET");
            }
        }
#endif

        // ==================== Game Flow ====================

        public void StartGame()
        {
            LevelManager lm = LevelManager.Instance;

            if (lm == null)
            {
                Debug.LogError("[GameManager] LevelManager không tồn tại! Hãy đảm bảo có LevelManager trong scene.");
                return;
            }

            // Debug mode: load level cụ thể
            if (debugMode)
            {
                LevelManager.SelectLevel(debugLevel);
                Debug.Log($"[GameManager] DEBUG MODE: load Level {debugLevel}");
            }

            _levelData = lm.GetSelectedLevel();

            // Fallback: nếu không có level, dùng level 1
            if (_levelData == null)
            {
                Debug.LogWarning("[GameManager] Không tìm thấy LevelData – thử fallback Level 1.");
                LevelManager.SelectLevel(1);
                _levelData = lm.GetSelectedLevel();
            }

            if (_levelData == null)
            {
                Debug.LogError("[GameManager] Vẫn không có LevelData! Kiểm tra LevelManager.allModes.");
                return;
            }

            _currentMode = LevelManager.GetSelectedMode();

            // Validate level
            if (!_levelData.IsValid(out string err))
            {
                Debug.LogError(err);
                return;
            }

            // Khởi tạo stats
            _score    = _levelData.baseScore;
            _moves    = 0;
            _combo    = 0;
            _maxCombo = 0;
            _hintCount            = _levelData.maxHints;
            _hintOnCooldown       = false;
            _hintCooldownRemaining = 0f;

            _currentTime = _levelData.timeLimit * _timeMultiplier;

            // Khởi tạo board
            if (boardManager != null)
                boardManager.Initialize(_levelData);
            else
                Debug.LogError("[GameManager] BoardManager chưa gán!");

            RefreshUI();
            gameUI?.HideAllPanels();

            CurrentState = GameState.Playing;
            StartTimer();

            Debug.Log($"[GameManager] START: {_currentMode} - {_levelData.levelName} ({_levelData.rows}x{_levelData.columns})");
        }

        // ==================== Timer ====================

        private void StartTimer()
        {
            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        private IEnumerator TimerCoroutine()
        {
            while (_currentTime > 0f && CurrentState == GameState.Playing)
            {
                yield return null;
                _currentTime -= Time.deltaTime;
                _currentTime = Mathf.Max(0f, _currentTime);
                gameUI?.UpdateTime(_currentTime);

                if (_hintOnCooldown)
                {
                    _hintCooldownRemaining -= Time.deltaTime;
                    if (_hintCooldownRemaining <= 0f)
                    {
                        _hintOnCooldown        = false;
                        _hintCooldownRemaining = 0f;
                    }
                    gameUI?.UpdateHint(_hintCount, _hintOnCooldown, _hintCooldownRemaining);
                }
            }

            if (CurrentState == GameState.Playing && _currentTime <= 0f)
                OnTimeUp();
        }

        // ==================== Card Events ====================

        public void IncrementMoves()
        {
            _moves++;
            gameUI?.UpdateMoves(_moves);
        }

        public void OnCorrectMatch()
        {
            _combo++;
            if (_combo > _maxCombo) _maxCombo = _combo;

            int gained = Mathf.RoundToInt(
                (_levelData.matchScore + _combo * _levelData.comboBonus) * _scoreMultiplier);
            _score += gained;

            AudioManager.Instance?.PlayMatch();
            gameUI?.UpdateScore(_score);
            gameUI?.UpdateCombo(_combo);
        }

        public void OnWrongMatch()
        {
            _combo  = 0;
            _score -= _levelData.wrongPenalty;
            if (_score < 0) _score = 0;

            AudioManager.Instance?.PlayWrong();
            gameUI?.UpdateScore(_score);
            gameUI?.UpdateCombo(_combo);
        }

        // ==================== Win / Lose ====================

        public void OnGameWin()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Win;
            StopTimerCoroutine();

            int timeBonus   = Mathf.RoundToInt(_currentTime * 2f);
            int movePenalty = _moves * 2;
            int finalScore  = Mathf.Max(0, _score + timeBonus - movePenalty);

            float timeFrac = (_levelData.timeLimit * _timeMultiplier) > 0
                ? _currentTime / (_levelData.timeLimit * _timeMultiplier) : 0f;
            int stars = timeFrac >= 0.6f ? 3 : timeFrac >= 0.35f ? 2 : 1;

            // Lưu tiến độ theo mode
            bool isNewHigh = SaveManager.SaveModeBestScore(_currentMode, _levelData.levelId, finalScore);
            SaveManager.SaveModeStars(_currentMode, _levelData.levelId, stars);
            SaveManager.UnlockNextModeLevel(_currentMode, _levelData.levelId);

            // Kiểm tra mở khóa mode mới
            CheckModeUnlock();

            AudioManager.Instance?.PlayWin();

            float timeUsed = _levelData.timeLimit * _timeMultiplier - _currentTime;
            gameUI?.ShowWinPanel(finalScore, timeUsed, _moves, _maxCombo, stars, isNewHigh);

            Debug.Log($"[GameManager] WIN! Score={finalScore} Stars={stars} NewHigh={isNewHigh}");
        }

        private void CheckModeUnlock()
        {
            // Hoàn thành Normal level cuối → mở Hard
            if (_currentMode == GameModeType.Normal)
            {
                var cfg = LevelManager.Instance?.GetCurrentModeConfig();
                if (cfg != null && _levelData.levelId >= cfg.TotalLevels)
                    SaveManager.UnlockMode(GameModeType.Hard);
            }
            // Hoàn thành Hard level cuối → mở Expert
            else if (_currentMode == GameModeType.Hard)
            {
                var cfg = LevelManager.Instance?.GetCurrentModeConfig();
                if (cfg != null && _levelData.levelId >= cfg.TotalLevels)
                    SaveManager.UnlockMode(GameModeType.Expert);
            }
        }

        private void OnTimeUp()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Lose;
            boardManager?.SetAllCardsInteractable(false);
            AudioManager.Instance?.PlayLose();
            gameUI?.ShowLosePanel();
            Debug.Log("[GameManager] TIME UP – LOSE");
        }

        // ==================== Pause ====================

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Paused;
            StopTimerCoroutine();
            boardManager?.SetAllCardsInteractable(false);
            gameUI?.ShowPausePanel();
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            CurrentState = GameState.Playing;
            boardManager?.SetAllCardsInteractable(true);
            gameUI?.HidePausePanel();
            StartTimer();
        }

        // ==================== Hint ====================

        public void UseHint()
        {
            if (CurrentState != GameState.Playing) return;
            if (_hintCount <= 0 || _hintOnCooldown) return;

            bool ok = boardManager?.ShowHint() ?? false;
            if (ok)
            {
                _hintCount--;
                _hintOnCooldown        = true;
                _hintCooldownRemaining = _levelData.hintCooldown;
                gameUI?.UpdateHint(_hintCount, true, _hintCooldownRemaining);
            }
        }

        // ==================== Debug Methods ====================

        /// <summary>[DEBUG] Thắng ngay level hiện tại</summary>
        public void DebugWinLevel()
        {
            Debug.Log("[DEBUG] DebugWinLevel()");
            boardManager?.ForceMatchAll();
        }

        /// <summary>[DEBUG] Thua ngay level hiện tại</summary>
        public void DebugLoseLevel()
        {
            Debug.Log("[DEBUG] DebugLoseLevel()");
            _currentTime = 0f;
            OnTimeUp();
        }

        // ==================== Navigation ====================

        public void RestartLevel()      => LevelManager.Instance?.ReloadCurrentScene();
        public void GoToMainMenu()      => LevelManager.Instance?.LoadMainMenu();
        public void GoToModeSelect()    => LevelManager.Instance?.LoadModeSelect();
        public void GoToNextLevel()     => LevelManager.Instance?.LoadNextLevel();

        // ==================== Helpers ====================

        private void StopTimerCoroutine()
        {
            if (_timerCoroutine != null) { StopCoroutine(_timerCoroutine); _timerCoroutine = null; }
        }

        private void RefreshUI()
        {
            gameUI?.UpdateTime(_currentTime);
            gameUI?.UpdateMoves(_moves);
            gameUI?.UpdateScore(_score);
            gameUI?.UpdateCombo(_combo);
            gameUI?.UpdateHint(_hintCount, false, 0f);
        }
    }
}
