using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MemoryMatchPro
{
    /// <summary>
    /// UI Settings popup: Sound, Music, Difficulty, Reset Progress.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Audio Toggles")]
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle musicToggle;

        [Header("Difficulty")]
        [SerializeField] private TMP_Dropdown difficultyDropdown;

        [Header("Buttons")]
        [SerializeField] private Button resetProgressButton;
        [SerializeField] private Button closeButton;

        [Header("Confirmation Dialog (optional)")]
        [SerializeField] private GameObject confirmResetDialog;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private void Awake()
        {
            soundToggle?.onValueChanged.AddListener(OnSoundToggleChanged);
            musicToggle?.onValueChanged.AddListener(OnMusicToggleChanged);
            difficultyDropdown?.onValueChanged.AddListener(OnDifficultyChanged);
            resetProgressButton?.onClick.AddListener(OnResetProgressClicked);
            closeButton?.onClick.AddListener(OnCloseClicked);

            confirmYesButton?.onClick.AddListener(OnConfirmReset);
            confirmNoButton?.onClick.AddListener(OnCancelReset);

            if (confirmResetDialog != null)
                confirmResetDialog.SetActive(false);
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        /// <summary>
        /// Đồng bộ UI với SaveManager
        /// </summary>
        public void RefreshUI()
        {
            // Sound
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.RemoveAllListeners();
                soundToggle.isOn = SaveManager.IsSoundEnabled();
                soundToggle.onValueChanged.AddListener(OnSoundToggleChanged);
            }

            // Music
            if (musicToggle != null)
            {
                musicToggle.onValueChanged.RemoveAllListeners();
                musicToggle.isOn = SaveManager.IsMusicEnabled();
                musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            }

            // Difficulty
            if (difficultyDropdown != null)
            {
                difficultyDropdown.onValueChanged.RemoveAllListeners();
                // Tùy chọn: Easy=0, Normal=1, Hard=2
                if (difficultyDropdown.options.Count == 0)
                {
                    difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Easy"));
                    difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Normal"));
                    difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Hard"));
                    difficultyDropdown.RefreshShownValue();
                }
                difficultyDropdown.value = SaveManager.GetDifficulty();
                difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
            }
        }

        private void OnSoundToggleChanged(bool isOn)
        {
            AudioManager.Instance?.SetSoundEnabled(isOn);
            if (isOn) AudioManager.Instance?.PlayButtonClick();
        }

        private void OnMusicToggleChanged(bool isOn)
        {
            AudioManager.Instance?.SetMusicEnabled(isOn);
        }

        private void OnDifficultyChanged(int value)
        {
            AudioManager.Instance?.PlayButtonClick();
            SaveManager.SetDifficulty(value);
            Debug.Log($"[SettingsUI] Difficulty set to {value}");
        }

        private void OnResetProgressClicked()
        {
            AudioManager.Instance?.PlayButtonClick();

            if (confirmResetDialog != null)
            {
                // Hiện dialog xác nhận
                confirmResetDialog.SetActive(true);
            }
            else
            {
                // Không có dialog → reset ngay
                DoResetProgress();
            }
        }

        private void OnConfirmReset()
        {
            AudioManager.Instance?.PlayButtonClick();
            confirmResetDialog?.SetActive(false);
            DoResetProgress();
        }

        private void OnCancelReset()
        {
            AudioManager.Instance?.PlayButtonClick();
            confirmResetDialog?.SetActive(false);
        }

        private void DoResetProgress()
        {
            SaveManager.ResetProgress();
            Debug.Log("[SettingsUI] Progress reset.");
        }

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            gameObject.SetActive(false);
        }
    }
}
