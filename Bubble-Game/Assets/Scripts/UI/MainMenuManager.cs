using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using BubbleShooterPro.Managers;
using BubbleShooterPro.Data;

namespace BubbleShooterPro.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Các Khung Panel UI")]
        public GameObject mainMenuPanel;
        public GameObject levelSelectPanel;
        public GameObject settingsPanel;
        public GameObject leaderboardPanel;

        [Header("Thiết lập Settings")]
        public Toggle bgmToggle;
        public Toggle sfxToggle;

        [Header("Thiết lập Bảng Điểm Cao")]
        public TextMeshProUGUI leaderboardText;

        private void Start()
        {
            // Thiết lập trạng thái ban đầu của các Panel
            ShowPanel(mainMenuPanel);

            // Đồng bộ trạng thái Toggle với dữ liệu đã lưu
            if (SaveManager.Instance != null)
            {
                if (bgmToggle != null) bgmToggle.isOn = SaveManager.Instance.IsBgmOn;
                if (sfxToggle != null) sfxToggle.isOn = SaveManager.Instance.IsSfxOn;
            }

            // Gán sự kiện lắng nghe thay đổi Toggle
            if (bgmToggle != null) bgmToggle.onValueChanged.AddListener(OnBgmToggled);
            if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSfxToggled);

            // Kích hoạt phát nhạc nền nhạc menu nếu có
            if (AudioManager.Instance != null && AudioManager.Instance.bgmClip != null)
            {
                AudioManager.Instance.PlayMusic(AudioManager.Instance.bgmClip);
            }
        }

        /// <summary>
        /// Bật Panel chỉ định và ẩn các Panel khác.
        /// </summary>
        private void ShowPanel(GameObject activePanel)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenuPanel == activePanel);
            if (levelSelectPanel != null) levelSelectPanel.SetActive(levelSelectPanel == activePanel);
            if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == activePanel);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(leaderboardPanel == activePanel);

            PlaySound(AudioManager.Instance != null ? AudioManager.Instance.bounceClip : null);
        }

        #region Main Menu Buttons

        /// <summary>
        /// Nhấn nút PLAY: Tiếp tục chơi level cao nhất đã mở khóa.
        /// </summary>
        public void OnPlayClicked()
        {
            int unlocked = 1;

            if (SaveManager.Instance != null)
            {
                unlocked = SaveManager.Instance.UnlockedLevel;
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SetSelectedLevelIndex(unlocked);
            }

            SceneManager.LoadScene("GameScene");
        }

        public void OnLevelSelectClicked()
        {
            ShowPanel(levelSelectPanel);
            
            // Tìm và yêu cầu LevelSelectPanel cập nhật danh sách nút
            LevelSelectManager selectManager = FindFirstObjectByType<LevelSelectManager>();
            if (selectManager != null)
            {
                selectManager.RefreshButtons();
            }
        }

        public void OnSettingsClicked()
        {
            ShowPanel(settingsPanel);
        }

        public void OnLeaderboardClicked()
        {
            ShowPanel(leaderboardPanel);
            DisplayLeaderboard();
        }

        public void OnQuitClicked()
        {
            Debug.Log("Quit Game!");
            Application.Quit();
        }

        public void OnBackClicked()
        {
            ShowPanel(mainMenuPanel);
        }

        #endregion

        #region Settings Functions

        private void OnBgmToggled(bool value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBgmOn(value);
            }
        }

        private void OnSfxToggled(bool value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxOn(value);
            }
        }

        /// <summary>
        /// Reset tiến trình người chơi.
        /// </summary>
        public void OnResetProgressClicked()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ResetProgress();
                
                // Cập nhật lại UI Toggle
                if (bgmToggle != null) bgmToggle.isOn = SaveManager.Instance.IsBgmOn;
                if (sfxToggle != null) sfxToggle.isOn = SaveManager.Instance.IsSfxOn;
                
                Debug.Log("Đã xóa trắng tiến trình người chơi!");
            }
            
            ShowPanel(mainMenuPanel);
        }

        #endregion

        #region Leaderboard Format

        private void DisplayLeaderboard()
        {
            if (leaderboardText == null) return;

            if (SaveManager.Instance == null)
            {
                leaderboardText.text = "Không tìm thấy dữ liệu lưu trữ.";
                return;
            }

            var scores = SaveManager.Instance.GetLeaderboard();
            if (scores == null || scores.Count == 0)
            {
                leaderboardText.text = "BẢNG XẾP HẠNG TRỐNG\nHãy vượt qua các màn để ghi danh!";
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("TOP 5 ĐIỂM CAO");
            sb.AppendLine("--------------------");

            for (int i = 0; i < scores.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {scores[i].playerName} - {scores[i].score} Pts ({scores[i].date})");
            }

            leaderboardText.text = sb.ToString();
        }

        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (AudioManager.Instance != null && clip != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
        }
    }
}
